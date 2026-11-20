# 01. kind

Poprzedni numer to [powiadomienia](../12-mobile/04-powiadomienia.md). Następny to [manifesty](02-manifesty.md).

Ten numer stawia lokalny klaster `kind` i jeden Pod, który odpowiada na HTTP. Nie ma jeszcze usług JJDevHub na klastrze. Jest narzędzie, probe i różnica między Podem a Deploymentem sprawdzona na pustym obrazie.

## Po co

Compose na maszynie nie uczy probe, requestów i rolloutu. kind zamyka klaster na laptopie, bez chmury i bez k3s na VM. Najpierw jeden Pod, potem manifesty produktu. Odwrócenie kolejności kończy się YAML-em, którego nikt nie umie zdiagnozować.

## Dlaczego ta technologia

### kind

`kind` podnosi węzły jako kontenery. Problem, który zamyka, to nauka Kubernetes tylko z rysunku. Odpada minikube, jeśli kind jest lżejszy na tym samym Dockerze, który już jest. Koszt: to nie jest klaster maszyny. Maszyna dostanie k3s później. kind nie jest celem wydania.

### Obiekty

Najpierw Pod z obrazem, który ma `/health/live`. Potem Deployment i Service, skasowanie Poda i obserwacja, że Deployment stawia nowy. Problem, który zamyka, to mylenie Poda z Deploymentem. Odpada sam Deployment bez zobaczenia, że Pod jest wymienny. Koszt: godzina na obiekty, które produkt i tak użyje.

### Probe

Na tym ćwiczeniu liveness woła `/health/live`, readiness `/health/ready`. Zły liveness, który woła ready z bazą, jest opisany jako błąd, którego nie powielimy w manifeście produktu. Problem, który zamyka, to restart w pętli. Odpada jeden probe na wszystko.

### kubectl

Polecenia `kubectl get`, `describe`, `logs` są w checkpointcie. Nie ma Helm na tym numerze. Problem, który zamyka, to szablon, który chowa obiekt. Odpada Helm jako pierwsze narzędzie. Kustomize dojdzie w manifeście produktu.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| kind | klaster Kubernetes w Dockerze |
| Pod | najmniejsza jednostka uruchomienia |
| Deployment | pilnuje, żeby Pod o danym szablonie istniał |
| probe | pytanie kubeleta, czy żyjesz i czy wolno dać ruch |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/k8s/kind-config.yaml` | klaster `jjdevhub`, mapowanie portu na przyszłą bramę |
| `deploy/k8s/scratch/pod.yaml` | ćwiczenie, nie manifest produktu |
| `deploy/k8s/up-kind.sh` | utworzenie klastra |

`scratch/` nie jest aplikowane na maszynę. Jest w git, bo jest ćwiczeniem do powtórzenia, i ma w pierwszej linii komentarz, że nie jest wydaniem.

## Plan

### 1. Klaster

`kind create cluster --name jjdevhub --config deploy/k8s/kind-config.yaml`. Kontekst kubectl wskazuje ten klaster.

### 2. Pod

Obraz, który już mamy, albo `nginx` wycofany po ćwiczeniu. Lepiej obraz bramy `jjdevhub-gateway:dev` załadowany `kind load`. Probe na `/health/live`. `kubectl delete pod` przy samym Podzie nie stawia go z powrotem. To jest obserwacja checkpointu.

### 3. Deployment

Ten sam kontener w Deployment. `kubectl delete pod` kończy się nowym Podem w stanie Ready. Service ClusterIP. `kubectl port-forward` na `/health/live` daje `200`.

### Co jest do czego

| Obiekt | Po ćwiczeniu |
| --- | --- |
| Pod w `scratch/` | nie wchodzi do overlay maszyny |
| klaster kind | zostaje do numeru manifestów |
| Deployment ćwiczeniowy | kasowany na końcu numeru albo zostaje w `scratch` z replikami 0 |

## Testy

Nie ma testu xUnit. Checkpoint jest sekwencją kubectl.

| Arrange | Act | Assert |
| --- | --- | --- |
| Deployment Ready | `kubectl delete pod -l app=scratch` | nowy Pod Ready, `GET` po port-forward `200` |

## Checkpoint

```bash
kind get clusters
kubectl config current-context
kubectl apply -f deploy/k8s/scratch/deployment.yaml
kubectl rollout status deployment/scratch
kubectl port-forward svc/scratch 18080:8080
curl -fsS http://127.0.0.1:18080/health/live
```

Nazwy z pliku scratch. Po ćwiczeniu `kubectl delete -f deploy/k8s/scratch/` sprząta obiekty. Klaster zostaje.

## Umiesz wyjaśnić

Czym Pod różni się od Deploymentu?

Pod jest procesem, który się kończy. Deployment trzyma liczbę replik i stawia nowego Poda, gdy stary zniknie. Sam manifest Poda tego nie zrobi.

Dlaczego liveness nie woła bazy?

Bo kubelet zrestartuje kontener, a baza przez to nie wstanie. Ready zdejmuje ruch. Live ma mówić, że proces oddycha.

Czym kind różni się od maszyny?

kind jest na laptopie i znika z Dockerem. Wydanie jest na k3s na VM. Uczymy obiektów tu, żeby YAML maszyny nie był pierwszym kontaktem.

Dlaczego nie Helm?

Bo szablon chowa Deployment, który właśnie mamy zobaczyć. Kustomize w następnym numerze nakłada łatki bez języka szablonu.

Co zostaje po numerze?

Konfiguracja kind i umiejętność. Obiekty scratch nie są produktem i nie jadą na VM.

## Zasady

- `scratch/` nie jest w overlay maszyny.
- Liveness ćwiczenia nie zależy od bazy.
- kind nie jest celem deployu tagu.
- Nie ma Helm.
- Obraz do ćwiczenia jest ładowany do kind, nie ciągnięty z publicznego `:latest` bez tagu, jeśli używamy bramy: tag `dev`.

## Poza zakresem

- Manifesty usług są w [02-manifesty.md](02-manifesty.md).
- k3s na VM jest w [04-deploy-k3s.md](04-deploy-k3s.md).
- HPA jest w [07-skalowanie.md](07-skalowanie.md).

## Źródła

- [kind](https://kind.sigs.k8s.io/). Wiążące jest tworzenie klastra konfiguracją. Dotyczy `kind-config.yaml`. Ze źródła nie bierzemy kind jako produkcji.
- [Pod lifecycle](https://kubernetes.io/docs/concepts/workloads/pods/pod-lifecycle/). Wiążące są probe. Dotyczy różnicy live i ready. Ze źródła nie bierzemy liveness wołającego zależność.
- [Deployment](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/). Wiążące jest odtwarzanie Poda. Dotyczy ćwiczenia ze skasowaniem. Ze źródła nie bierzemy strategii innej niż RollingUpdate w produkcie, gdy do niego dojdziemy.

## Gotowe gdy

- Klaster `jjdevhub` istnieje lokalnie.
- Skasowanie Poda Deploymentu kończy się nowym Podem Ready.
- `GET /health/live` przez port-forward zwraca `200`.
- Obiekty scratch nie są aplikowane na maszynę.
- W numerze nie ma Helm.
