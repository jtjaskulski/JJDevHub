# 04. Deploy na k3s

Poprzedni numer to [Terraform](03-terraform-maszyna.md). Następny to [OpenBao](05-openbao.md).

Ten numer przełącza job `deploy` z `docker compose` na `kubectl` na k3s. Tag i atestacja zostają. Obraz wchodzi jako digest w overlay. Rollback jest poprzednim tagiem, tym samym jobem. Compose schodzi z ruchu, gdy brama na klastrze odpowie.

## Po co

Dwa sposoby wydania na raz kończą się tym, że tag wjechał do GHCR, a na maszynie stoi stary kontener compose. Jeden job zamyka drogę. k3s bez zmiany joba jest tylko węzłem, na który nikt nie wgrywa obrazu.

## Dlaczego ta technologia

### Ten sam workflow

`deploy.yml` dostaje krok `kubectl apply -k deploy/k8s/overlays/vm` zamiast `docker compose up`. Weryfikacja atestacji zostaje przed apply. Problem, który zamyka, to drugi workflow „na klaster”, który ktoś odpali z `:latest`. Odpada równoległy compose i klaster jako dwa wydania. Koszt: okno przełączenia. Najpierw apply klastra, sprawdzenie `/versions` przez port wewnętrzny, potem przełączenie tunelu albo `hostPort` na Service, potem zatrzymanie compose. Jeśli health klastra padnie, compose zostaje. Nie wyłączamy compose przed sukcesem.

### Digest

Overlay `vm` dostaje obraz `ghcr.io/<właściciel>/jjdevhub-gateway@sha256:...` z joba publish. Tag gita jest w etykiecie i w `SERVICE_VERSION`. Problem, który zamyka, to pływający tag w manifeście. Odpada `:latest` i odpada sam tag bez digestu jako jedyna referencja. Koszt: manifest na klastrze zmienia się przy każdym wydaniu. To jest właśnie deploy.

### Runner

Job nadal leci na `jjdevhub`. `kubectl` używa kubeconfig z maszyny, spoza gita, uprawnienie `600`. Problem, który zamyka, to kubeconfig w sekretach GitHuba, skoro runner i tak stoi na węźle. Odpada kopiowanie kubeconfig do GitHuba. Koszt: runner ma dostęp do klastra. Ma go już dziś przez grupę docker. Nie poszerzamy tego na pull requesty. Job nadal nie jest na `pull_request`.

### Migracja

Kontener jednorazowy albo `Job` Kubernetesa wykonuje migrację przed rolloutem Deploymentu. Nie ma migracji w `startupProbe` pięciu replik naraz. Na tym numerze replika aplikacji to 1. Problem, który zamyka, to wyścig migracji. Odpada `Migrate` przy każdym starcie Poda.

### Powrót

`workflow_dispatch` z poprzednim tagiem stosuje poprzedni digest. Nie ma `helm rollback`. Jest ten sam apply. Problem, który zamyka, to osobna procedura cofania. Odpada edycja replik ręcznie na klastrze jako wydanie. Ręczna zmiana i tak zniknie przy następnym apply. Źródłem jest git i tag.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| digest | `sha256` obrazu w manifeście |
| Job migracji | jednorazowy Pod schematu, przed ruchem |
| przełączenie | ruch na Service dopiero po health |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/deploy.yml` | `kubectl` zamiast compose na maszynie |
| `deploy/k8s/overlays/vm/` | łatka digestu, generowana w przebiegu albo kustomize `images` |
| `deploy/k8s/base/migrate-job.yaml` | migracja |

Przebieg nie commituje digestu z powrotem do gałęzi. Zostawia go w artefakcie i w klastrze. Inaczej tag musiałby robić commit. Źródłem obrazu na klastrze jest apply z tego przebiegu, nie plik na `main` z sha. Plik na `main` ma nazwę obrazu. Digest dokleja job. To jest uczciwe: `main` nie nadąża za sha w commicie, a klaster ma dokładnie to, co job zweryfikował.

## Plan

### 1. Publish bez zmian sensu

Nadal buduje i atestuje. Dokłada artefakt z digestem.

### 2. Apply

`kustomize edit set image` w katalogu tymczasowym na runnerze, `kubectl apply`, `kubectl rollout status`. Potem `kubectl exec` albo `curl` na Service `gateway` z węzła: `/health/live` i `/versions` z tagiem.

### 3. Ruch

Dopiero wtedy jednostka, która trzyma port tunelu, wskazuje na ten Service. Compose `stop` bramy. Postgres compose nie jest kasowany w ciemno, jeśli dane mają zostać. Ten numer mówi wprost: albo wolumen Postgresa jest ten sam dysk podpięty do StatefulSet, albo jest jednorazowy zrzut i odtworzenie. Bez tej decyzji nie wyłączamy starej bazy. Decyzja: StatefulSet dostaje ten sam katalog danych, jeśli compose trzymało go w `/var/lib/jjdevhub/pg`. Jeśli nie, zostajemy na compose dla bazy i tylko aplikacja idzie na k3s. Prostsze i bezpieczniejsze na ten numer: na k3s idzie aplikacja, a baza zostaje tam, gdzie już ma dane, dopóki osobny plan nie przeniesie wolumenu. Connection string w Secret wskazuje bazę, która naprawdę ma dane. Nie stawiamy drugiej pustej bazy i nie udajemy, że to migracja danych.

Jeśli manifest z numeru manifestów miał StatefulSet Postgresa, na maszynie z istniejącymi danymi ten StatefulSet nie dostaje pustego dysku obok starego. Albo mount istniejącego katalogu, albo baza zostaje poza klastrem. Ten numer wybiera: baza i Kafka zostają na Dockerze, dopóki wolumen nie jest świadomie przeniesiony. Aplikacje na k3s łączą się z nimi po adresie hosta. To jest kompromis opisany, nie „pełny klaster od dziś”. Pełne przeniesienie wolumenu jest osobnym zdaniem w tym samym numerze tylko wtedy, gdy checkpoint obejmuje zrzut i odtworzenie. Domyślna ścieżka „gotowe”: aplikacja na k3s, dane tam gdzie były, jeden `/versions` z tagiem.

### Co jest do czego

| Krok | Kto |
| --- | --- |
| atestacja | job przed apply |
| apply | kubectl na runnerze |
| compose stop bramy | po health, nie przed |

## Testy

Suchy `kustomize build` overlay vm. Na żywo checkpoint na maszynie.

| Arrange | Act | Assert |
| --- | --- | --- |
| tag bramy | job deploy | `/versions` pokazuje ten tag, proces bramy jest Podem, nie kontenerem compose |

## Checkpoint

```bash
kubectl -n jjdevhub rollout status deployment/gateway
curl -fsS http://127.0.0.1:8080/versions
docker ps --filter name=gateway
```

`curl` pokazuje tag. `docker ps` nie pokazuje bramy compose albo pokazuje ją jako exited. Baza, jeśli została na Dockerze, nadal jest `healthy`. Atestacja była sprawdzona w logu joba zanim apply się zaczął.

## Umiesz wyjaśnić

Dlaczego nie wyłączamy compose przed health?

Bo apply może się nie udać. Stara brama trzyma ruch. Najpierw nowa odpowiada, potem stara schodzi.

Czemu digest nie jest commitem na `main`?

Bo commit z joba deployu miesza wydanie z recenzją i wymaga uprawnień do push na `main` z runnera. Digest żyje w klastrze i w logu przebiegu, który atestacja wiąże z obrazem. `main` ma źródło. Klaster ma sha.

Dlaczego baza może zostać na Dockerze?

Bo przeniesienie dysku z danymi jest osobnym ryzykiem od zmiany sposobu uruchamiania bramy. Mieszanie obu w jednym oknie zostawia pustą bazę i działającą bramę. Dane zostają. Aplikacja przechodzi na Pody.

Co jest rollbackiem?

Ten sam job, poprzedni tag, poprzedni digest. Nie ma edycji replik w panelu jako procedury.

Dlaczego migracja nie jest w każdym Podzie?

Bo dwie repliki na starcie odpaliłyby ją dwa razy. Osobny Job, raz, przed rolloutem.

## Zasady

- Atestacja przed `kubectl apply`.
- Brak `:latest`.
- Compose bramy schodzi dopiero po health Poda.
- Baza z danymi nie jest zastępowana pustym wolumenem w tym samym kroku.
- Deploy nie jest na pull requeście.
- Kubeconfig nie wchodzi do git ani do sekretów GitHuba, skoro runner jest na węźle.

## Poza zakresem

- HPA jest w [07-skalowanie.md](07-skalowanie.md).
- OpenBao jest w [05-openbao.md](05-openbao.md).
- Redis jest w [06-redis.md](06-redis.md).
- Azure jest w [08-azure.md](08-azure.md) i nie jest tym klastrem.

## Źródła

- [kubectl apply](https://kubernetes.io/docs/reference/kubectl/generated/kubectl_apply/). Wiążące jest deklaratywne apply katalogu. Dotyczy overlay vm. Ze źródła nie bierzemy `kubectl edit` jako wydania.
- Numer deployu compose. Wiążące są tag, atestacja i rollback tym samym jobem. Dotyczy przełączenia narzędzia, nie przełączenia zasad. Ze źródła nie bierzemy równoległego drugiego workflow.
- [Jobs](https://kubernetes.io/docs/concepts/workloads/controllers/job/). Wiążące jest jednorazowe dojście do końca. Dotyczy migracji. Ze źródła nie bierzemy migracji w pętli Deploymentu.

## Gotowe gdy

- Tag bramy kończy się Podem Ready i polem `gateway` w `/versions`.
- Stary kontener bramy compose nie obsługuje ruchu.
- Baza z danymi nadal odpowiada.
- W logu joba atestacja jest przed apply.
- Nie ma `:latest` w zastosowanym manifeście.
