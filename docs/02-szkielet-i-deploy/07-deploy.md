# 07. Deploy i rollback

Poprzedni numer to [compose](06-compose.md). Następny to [telemetria](08-telemetria.md).

Ten numer dodaje `.github/workflows/deploy.yml`. Job `publish` buduje jeden obraz na `ubuntu-latest` i wypycha go do GHCR. Job `deploy` na runnerze `jjdevhub` sprawdza atestację i podmienia jeden komponent. Wejściem jest tag z [09-wersje.md](../01-bramki/09-wersje.md). Pierwszy rollout to brama. Rollback jest tym samym jobem z poprzednim tagiem tego samego komponentu.

## Po co

Obraz, który da się zbudować lokalnie, nie jest jeszcze wydaniem. Wydanie ma digest, atestację i numer, który widać w `GET /versions`. Bez joba tag zostaje etykietą gita. Bez weryfikacji atestacji runner uruchomi obraz, który ktoś wypchnął pod tę samą nazwę z laptopa.

Rollback osobną gałęzią `hotfix` omija checki. Poprzedni tag ich nie omija: został zbudowany wtedy, gdy checki były zielone.

## Dlaczego ta technologia

### GHCR

Rejestr to `ghcr.io/<właściciel>/jjdevhub-gateway`. Problem, który zamyka, to obraz tylko na dysku runnera: runner po reinstalacji nie ma czym wstać. Odpada rejestr na laptopie. Koszt: package w GitHubie ma uprawnienia. Obraz bramy jest publiczny, tak jak repozytorium. Sekretu w obrazie nie ma, co pilnują gitleaks i pusty `appsettings`.

### Dwa joby

`publish` nie ma dostępu do maszyny. `deploy` nie buduje kompilatorem. Problem, który zamyka, to jeden job, który i kompiluje nieprzejrzany kod, i ma demona Dockera na maszynie. Deploy uruchamia się tylko na tagu `gateway-v*` (i później na tagach pozostałych komponentów tą samą macierzą). Nie uruchamia się na `pull_request`.

### Atestacja przed `docker compose up`

Job `deploy` woła weryfikację atestacji digestu. Problem, który zamyka, to `docker pull` zaufany tylko dlatego, że tag wygląda znajomo. Odpada deploy bez tej weryfikacji. Koszt: obraz bez atestacji, zbudowany ręcznie, nie wejdzie. To jest cel.

### Jedna wersja naraz

Plik env na maszynie dostaje `SERVICE_VERSION` równe tagowi, na przykład `gateway-v0.1.0`. Compose podnosi usługę z digestu, nie z `:latest`. Problem, który zamyka, to dwa deploye, które nadpisują zmienną w połowie. Job jest jeden na runnerze i runner jest jeden. Równoległy deploy tego samego komponentu nie wchodzi: concurrency group `deploy-gateway`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| digest | identyfikator obrazu, którego deploy używa w compose |
| concurrency group | kolejka, która nie wpuszcza dwóch deployów bramy naraz |
| rollback | ponowne uruchomienie joba z poprzednim tagiem |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `.github/workflows/deploy.yml` | `publish` i `deploy` |
| `/etc/jjdevhub/compose.env` | na maszynie, `SERVICE_VERSION` i ewentualne późniejsze sekrety |

Workflow nasłuchuje `push` tagów dopasowanych do `*-v[0-9]+.[0-9]+.[0-9]+`. Prefiks wybiera komponent. Na tym numerze jedyny komponent z Dockerfile to `gateway`. Pozostałe prefiksy kończą się jawnym logiem `component has no image yet` i kodem `0`, żeby tag `web-v0.1.0` z przyszłości nie był czerwony, zanim web istnieje. Gdy web dostanie obraz, ten sam workflow przestaje tak logować i buduje web. Lista „ma obraz” jest w pliku `.github/deploy-components.txt`, nie w komentarzu.

## Plan

### 1. Publish

`runs-on: ubuntu-latest`. Uprawnienia: `contents: read`, `packages: write`, `id-token: write`, `attestations: write`.

Kroki: checkout tagu, build `deploy/docker/Dockerfile` z `PROJECT` i `ASSEMBLY` tego komponentu, tag obrazu digestem i nazwą `gateway-vX.Y.Z`, `docker push` do GHCR, atestacja digestu, wgranie SBOM jako artefaktu przebiegu. Nie używa `:latest`.

### 2. Deploy

`needs: publish`. `runs-on: [self-hosted, jjdevhub]`. Uprawnienia: `contents: read`, `packages: read`, `id-token: write`, `attestations: read` w zakresie, którego wymaga weryfikacja.

Kroki: weryfikacja atestacji, `aspire publish` na tym tagu (albo użycie artefaktu YAML z publish, jeśli publish go wgrał), zapis digestu do pliku compose override `image:`, ustawienie `SERVICE_VERSION` w env poza gitem, `docker compose up -d --remove-orphans` tylko usługi tego komponentu, `curl --fail http://127.0.0.1:8080/health/live`, `curl` `/versions` i sprawdzenie, że pole `gateway` równa się tagowi.

Override obrazu jest plikiem na maszynie ` /var/lib/jjdevhub/gateway.image.yml`, nie commitem.

### 3. Rollback

Nie ma osobnego workflow. Ponowne uruchomienie `deploy.yml` z `workflow_dispatch` i wejściem `tag=gateway-v<poprzedni>` wykonuje te same kroki. Tag musi istnieć i mieć atestację. Nie da się wpisać tagu, którego nie było.

Usunięcie tagu nie jest rollbackiem. Poprzedni tag zostaje.

### 4. Pierwszy raz

Pierwszy tag bramy to efekt scalenia na `main` zmian, które release-please uzna za `feat(gateway): ...`, albo ręczne ustawienie wersji w `versions/gateway.json` przez pull request release-please z `0.0.0` na `0.1.0`. Ten numer nie taguje z laptopa. Jeśli `0.1.0` ma powstać razem z tym numerem, tytuł squash to `feat(gateway): add deploy workflow` i release-please na `main` wystawia tag. Treść numeru to workflow. Tag jest skutkiem, nie plikiem YAML.

### Co jest do czego

| Job | Czego nie robi |
| --- | --- |
| `publish` | nie łączy się z maszyną i nie czyta `compose.env` |
| `deploy` | nie kompiluje i nie pomija weryfikacji atestacji |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| tag bez atestacji, podstawiony w suchym przebiegu weryfikacji | krok weryfikacji | kod `1` |
| po prawdziwym deployu | `GET /versions` na maszynie | pole `gateway` równe tagowi |

Suchy przebieg weryfikacji używa narzędzia atestacji na digest, którego przebieg nie podpisał.

## Checkpoint

Po pierwszym tagu, na maszynie i z zewnątrz:

```bash
curl -fsS http://127.0.0.1:8080/versions
curl -fsS "https://<hostname>/versions"
```

Oba JSON-y mają `gateway` równe tagowi, który właśnie wjechał, i puste pozostałe pola. `docker inspect` obrazu pokazuje digest zgodny z atestacją przebiegu.

Rollback: `workflow_dispatch` z poprzednim tagiem, jeśli istnieje. Przy pierwszym tagu nie ma poprzedniego i rollback nie jest jeszcze czymś do pokazania. Wtedy checkpointem jest sam deploy. Drugi tag, choćby `fix(gateway):`, ma pokazać powrót.

## Umiesz wyjaśnić

Dlaczego deploy nie leci z pull requestu?

Bo pull request nie jest wydaniem. Wydanie jest tagiem na `main`. Runner na maszynie nie wykonuje kodu z gałęzi roboczej.

Czym rollback różni się od deployu?

Niczym, poza tagiem. Ten sam job, starszy tag, ta sama weryfikacja. Nie ma gałęzi wyciętej z `main`.

Dlaczego nie ma `:latest`?

Bo `:latest` nie mówi, który digest działa. Dwa `pull` pod rząd mogą być dwoma obrazami. Env i override trzymają tag i digest z tego przebiegu.

Co widać w `/versions`, gdy Learning jeszcze nie istnieje?

Pusty napis w polu `learning`. Pole jest. Deploy bramy nie dotyka innych usług, bo ich kontenerów jeszcze nie ma.

Dlaczego publish i deploy są rozdzielone?

Publish ma prawo pisać do rejestru i nie ma demona na maszynie. Deploy ma demona i nie potrzebuje prawa do push. Jeden job z oboma prawami jest większy, niż trzeba, i trudniejszy do przeczytania, gdy coś pójdzie nie tak.

## Zasady

- Deploy tylko z tagu albo z `workflow_dispatch` wskazującego istniejący tag.
- Brak `:latest` w override.
- Atestacja jest weryfikowana przed `up`.
- Job deployu nie drukuje `compose.env`.
- Concurrency group jest per komponent.

## Poza zakresem

- Kolejne komponenty dopisują się do `.github/deploy-components.txt` w numerze, w którym dostają Dockerfile i proces. Nie powstaje drugi workflow.
- Telemetria na maszynie jest w [08-telemetria.md](08-telemetria.md).
- Zamiana tego joba na `kubectl` jest w [04-deploy-k3s.md](../13-kubernetes/04-deploy-k3s.md). Do tego czasu job robi `docker compose`.
- OIDC do Azure jest w [08-azure.md](../13-kubernetes/08-azure.md) i nie dotyczy tej maszyny.

## Źródła

- [Publishing Docker images](https://docs.github.com/en/actions/publishing-packages/publishing-docker-images). Wiążące jest `packages: write` i nazwa GHCR. Dotyczy joba `publish`. Ze źródła nie bierzemy tagu `:latest`.
- [Artifact attestations](https://docs.github.com/en/actions/security-guides/using-artifact-attestations-to-establish-provenance-for-builds). Wiążąca jest weryfikacja przed użyciem digestu. Dotyczy joba `deploy`. Ze źródła nie bierzemy obrazu bez atestacji jako wyjątku „na szybko”.
- [Concurrency](https://docs.github.com/en/actions/using-jobs/using-concurrency). Wiążąca jest jedna grupa na komponent. Dotyczy tego, że dwa deploye bramy nie idą naraz. Ze źródła nie bierzemy anulowania deployu w połowie `up` (`cancel-in-progress` zostaje `false` dla tego workflow).

## Gotowe gdy

- Tag `gateway-v0.1.0` wypycha obraz do GHCR i podnosi bramę na maszynie.
- `GET /versions` z internetu, przez tunel, pokazuje ten tag w polu `gateway`.
- Obraz bez atestacji nie przechodzi kroku weryfikacji.
- W workflow nie ma `:latest` i nie ma `pull_request`.
- `cancel-in-progress` deployu jest wyłączone.
