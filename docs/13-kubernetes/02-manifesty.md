# 02. Manifesty

Poprzedni numer to [kind](01-kind.md). Następny to [Terraform maszyny](03-terraform-maszyna.md).

Ten numer opisuje usługi JJDevHub jako manifesty Kustomize: Deployment, StatefulSet, Service, ConfigMap, Secret poza gitem, probe, requesty. Nakładka `kind` i nakładka `vm` różnią się tagiem obrazu i liczbą replik, nie kształtem aplikacji. Na kind da się postawić bramę i Learning. Maszyna jeszcze zostaje na compose, dopóki nie ma Terraforma i przełączenia deployu.

## Po co

Jeden wielki YAML skopiowany na kind i na VM rozjedzie się przy pierwszej zmianie probe. Base i overlay zamykają różnicę. Secret w git zamknąłby się wyciekiem hasła Postgresa. Secret powstaje na klastrze spoza repozytorium.

## Dlaczego ta technologia

### Kustomize

`deploy/k8s/base` ma obiekty bez tagu obrazu przypiętego do środowiska. `overlays/kind` ustawia tag `dev` i `imagePullPolicy: IfNotPresent`. `overlays/vm` ustawia digest albo tag komponentu i `imagePullPolicy: IfNotPresent` z rejestru GHCR. Problem, który zamyka, to dwa pełne kopie Deploymentu. Odpada Helm. Koszt: trzeba rozumieć `kustomization.yaml`. Jest krótki.

### StatefulSet i Deployment

Postgres i Kafka są StatefulSet z wolumenem. Procesy aplikacji są Deployment. Problem, który zamyka, to baza jako Deployment bez stabilnego dysku. Odpada jeden rodzaj obiektu na wszystko. Koszt: dwa sposoby. Baza nie dostaje HPA. To jest w numerze skalowania, a tu w ogóle nie ma obiektu HPA.

### Probe

Liveness: `httpGet` `/health/live`. Readiness: `/health/ready`. `initialDelaySeconds` krótkie, `periodSeconds` 10. Problem, który zamyka, to probe na `/health` bez podziału, albo liveness na ready. Odpada exec w kontenerze, który nie ma powłoki, gdy kiedyś wejdzie obraz chiseled. HTTP zostaje.

### Zasoby

Request CPU i pamięci są ustawione na bramie i na Learning. Bez requestu HPA i planista nie mają z czego liczyć. Limit jest wyższy niż request. Problem, który zamyka, to Pod bez requestu na wspólnym węźle. Odpada brak liczb „bo lokalnie działa”. Koszt: złe liczby wymagają poprawki. Startowe liczby są w manifeście i wolno je zmienić po obserwacji, jawnym commitem.

### Secret

`kustomize` nie zawiera hasła. Secret `jjdevhub-env` powstaje z pliku na maszynie albo z `kind` przez `kubectl create secret` z lokalnego env, który jest w `.gitignore`. Przykład kluczy jest w `deploy/k8s/secret.example`. Problem, który zamyka, to `Secret` z `stringData` w git. Odpada zaszyfrowany SOPS, którego i tak nie chcemy w repozytorium.

### Sieć

Service `gateway` ClusterIP port 8080. Usługi nie mają Service typu NodePort na maszynę. Na kind jest port mapping z konfiguracji kind na ten Service albo port-forward. Ingress kontrolera nie ma. YARP jest bramą. Traefik nie jest instalowany.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| base | manifest wspólny |
| overlay | łatka środowiska |
| request | ile planista rezerwuje |
| Secret | obiekt klastra, nie plik w git |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/k8s/base/` | Deployment bramy, Learning, Postgres, Kafka, Service |
| `deploy/k8s/base/kustomization.yaml` | lista zasobów |
| `deploy/k8s/overlays/kind/` | tag `dev` |
| `deploy/k8s/overlays/vm/` | tag z GHCR |
| `deploy/k8s/secret.example` | nazwy kluczy, puste wartości |

Na tym numerze w base są brama, Learning (api, relay, projector), Postgres i Kafka. Reszta usług dochodzi tym samym wzorcem, gdy manifest jest kopiowany z tego szablonu w swoim commicie. Szablon jest jeden, nie siedem nieuzasadnionych kopii bez różnicy. Każda usługa ma swój katalog w `base/apps/<nazwa>`.

## Plan

### 1. Base

Deployment z `env` ze Secretu i z ConfigMapy, która nie ma haseł. Port kontenera 8080 dla HTTP. Relay nie wystawia Service publicznego, tylko probe na swoim porcie.

### 2. Overlay kind

`kustomize build deploy/k8s/overlays/kind` przechodzi. `kubectl apply` na kind. `kubectl rollout status` bramy. `curl` przez port z kind-config na `/versions`.

### 3. Obrazy

`kind load docker-image` dla tagów `dev`. Overlay nie ciągnie `:latest`.

### Co jest do czego

| Obiekt | Rodzaj |
| --- | --- |
| `gateway`, `learning-api`, `learning-relay`, `learning-projector` | Deployment |
| `postgres`, `kafka` | StatefulSet |
| `gateway` | Service ClusterIP |
| hasła | Secret spoza git |

## Testy

Nie ma xUnit. Jest `kustomize build` i rollout na kind.

| Arrange | Act | Assert |
| --- | --- | --- |
| overlay kind | `kubectl apply` i rollout | brama Ready, `/versions` zwraca JSON |

```bash
kustomize build deploy/k8s/overlays/kind >/dev/null
```

## Checkpoint

```bash
kustomize build deploy/k8s/overlays/kind >/dev/null
kind load docker-image jjdevhub-gateway:dev
kubectl apply -k deploy/k8s/overlays/kind
kubectl -n jjdevhub rollout status deployment/gateway
```

Namespace jest `jjdevhub`. `GET /versions` działa przez port kind. W `git grep` po `deploy/k8s` nie ma hasła ani `stringData` z wartością inną niż pusta w example.

## Umiesz wyjaśnić

Dlaczego Kustomize, a nie dwa pliki?

Bo probe i port mają być te same. Overlay zmienia obraz i ewentualnie repliki. Dwa pełne pliki rozjadą probe.

Dlaczego StatefulSet dla Kafki i Postgresa?

Bo dysk ma przeżyć nowego Poda o tej samej tożsamości. Deployment bazy gubi wolumen albo wiąże go słabiej z tożsamością. Aplikacja nie ma stanu na dysku kontenera. Ma stan w bazie.

Czemu Secret nie jest w git?

Bo hasło w historii zostaje. Example ma nazwy. Wartość jest w klastrze.

Dlaczego nie ma Ingress?

Bo jedyna brama HTTP to YARP. Drugi Ingress to druga brama. Na VM ruch i tak wchodzi tunelem na Service `gateway`.

Co daje request?

Planista wie, ile węzeł uniesie, a później HPA ma mianownik. Bez requestu procent CPU nie ma sensu.

## Zasady

- Brak hasła w `deploy/k8s`.
- Brak `:latest`.
- Liveness to `/health/live`, readiness to `/health/ready`.
- Baza i Kafka są StatefulSet.
- Nie ma Traefika i nie ma Helm.
- Overlay nie kopiuje całego Deploymentu, tylko łatkę.

## Poza zakresem

- Terraform VM jest w [03-terraform-maszyna.md](03-terraform-maszyna.md).
- Podmiana joba deployu na `kubectl` jest w [04-deploy-k3s.md](04-deploy-k3s.md).
- HPA i PDB są w [07-skalowanie.md](07-skalowanie.md).
- OpenBao i Redis nie są w tym base.

## Źródła

- [Kustomize](https://kustomize.io/). Wiążące są base i overlay. Dotyczy `deploy/k8s`. Ze źródła nie bierzemy Helm jako zamiennika.
- [StatefulSet](https://kubernetes.io/docs/concepts/workloads/controllers/statefulset/). Wiążący jest stabilny dysk. Dotyczy Postgresa i Kafki. Ze źródła nie bierzemy StatefulSet dla bramy.
- [Configure probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/). Wiążący jest podział live i ready. Dotyczy ścieżek z numeru health. Ze źródła nie bierzemy liveness na `/health/ready`.

## Gotowe gdy

- `kustomize build` obu overlayów zwraca kod zero.
- Na kind brama dochodzi do Ready i `/versions` odpowiada.
- W git nie ma wartości Secretu.
- Manifest nie używa `:latest`.
- Postgres i Kafka mają wolumen w StatefulSet.
