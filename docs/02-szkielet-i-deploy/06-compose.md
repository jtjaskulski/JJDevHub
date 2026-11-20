# 06. Compose z AppHosta

Poprzedni numer to [Cloudflare](05-cloudflare.md). Następny to [deploy](07-deploy.md).

Ten numer spina AppHost z Docker Compose. `aspire publish` generuje `docker-compose.yaml`. Plik wygenerowany nie jest źródłem prawdy i nie jest commitowany. Źródłem prawdy zostaje `AppHost.cs`. Na maszynie brama startuje z tego pliku, z obrazem zbudowanym Dockerfilem z repozytorium, nie z Dockerfile wygenerowanym obok.

## Po co

Ręczny compose i AppHost to dwa opisy. Trzeciego dnia mają inne porty. Generowanie zamyka rozjazd. Commit wygenerowanego pliku zamyka go z powrotem, bo ktoś poprawi YAML i nie poprawi AppHosta. Dlatego generat ląduje w `aspire-output/`, który jest w `.gitignore`, a na maszynę kopiuje go job deployu.

Dockerfile z [03-dockerfile.md](03-dockerfile.md) zostaje nasz. Aspire nie generuje drugiego.

## Dlaczego ta technologia

### `AddDockerComposeEnvironment`

`Aspire.Hosting.Docker` 13.6.5 dodaje środowisko o nazwie `compose`. Gdy środowisko jest w modelu, zasoby compute publikują się jako usługi compose. Problem, który zamyka, to ręczne przepisywanie `depends_on` i zmiennych. Odpada ręczny YAML jako źródło. Koszt: trzeba rozumieć, co generator wstawi, i nadpisać to, co wstawi źle. Nadpisanie jest w kodzie AppHosta (`PublishAsDockerComposeService`), nie w poprawce YAML po generowaniu.

### Nasz Dockerfile

Projekt bramy dostaje w publikacji obraz budowany z `deploy/docker/Dockerfile` i argumentów `PROJECT` oraz `ASSEMBLY`. Problem, który zamyka, to drugi, wygenerowany Dockerfile, którego nikt nie czyta. Odpada domyślne generowanie Dockerfile przez Aspire dla tej usługi. Koszt: AppHost musi wskazać plik wprost. To jest jedna konfiguracja w `AppHost.cs`.

### Env na maszynie, nie w wygenerowanym pliku

`aspire publish` wypisuje `.env` z kluczami bez wartości produkcyjnych. Job nie kopiuje tego `.env` na maszynę jako pliku z hasłami. Maszyna używa `/etc/jjdevhub/compose.env`. Problem, który zamyka, to wygenerowany `.env` z hasłem w artefakcie przebiegu. Odpada uzupełnianie wygenerowanego `.env` sekretem w CI. Koszt: nazwy zmiennych w AppHost muszą zgadzać się z przykładam `compose.env.example`. Nowa zmienna jest zmianą przykładu w tym samym pull requeście. Wartość dopisuje się na maszynie, poza gitem.

### Jedna usługa na tym numerze

W modelu publikacji jest brama i nic więcej z procesów produktu. Postgres i Kafka dojdą w swoich numerach jako zasoby AppHosta i wtedy pojawią się w wygenerowanym compose bez osobnego YAML. Telemetria `otel-lgtm` dochodzi w [08-telemetria.md](08-telemetria.md). Nie upychamy jej tu, żeby ten numer kończył się jedną usługą, którą da się wytłumaczyć.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| publish | wygenerowanie artefaktów wdrożenia, bez uruchamiania AppHosta na maszynie |
| środowisko compose | zasób Aspire o nazwie `compose` |
| źródło prawdy | `AppHost.cs` i Dockerfile; nie plik w `aspire-output/` |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/apphost/JJDevHub.AppHost/AppHost.cs` | `AddDockerComposeEnvironment("compose")` i projekt bramy |
| `deploy/compose/compose.env.example` | nazwy zmiennych bramy, w tym `SERVICE_VERSION` |
| `aspire-output/` | wynik `aspire publish`, ignorowany |

## Plan

### 1. Środowisko

W AppHost:

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("compose");
builder.AddProject<Projects.JJDevHub_Gateway>("gateway")
    .WithEnvironment("SERVICE_VERSION", builder.Configuration["SERVICE_VERSION"]);
builder.Build().Run();
```

Fragment jest początkiem. W tym samym pliku publikacja obrazu wskazuje `deploy/docker/Dockerfile`, `PROJECT=JJDevHub.Gateway` i `ASSEMBLY=JJDevHub.Gateway.dll`. Metoda, którą Aspire 13.6 przyjmuje na projekcie, to ta, która w dokumentacji publikacji ustawia dockerfile kontekstu. Nie dopisujemy obok ręcznej usługi `gateway` w YAML.

Port hosta na maszynie: `127.0.0.1:8080:8080`. Publikacja na `0.0.0.0` jest zabroniona. Tunel i tak chodzi na localhost.

### 2. Publish

Z katalogu AppHost albo z korzenia, zgodnie z `aspire.config.json`:

```bash
aspire publish -o aspire-output
```

Wynik zawiera `docker-compose.yaml` z usługą bramy. Plik nie jest dodawany do gita. `.gitignore` już obejmuje `aspire-output/` z numeru solucji; jeśli nie obejmuje, ten numer poprawia ignore.

### 3. Nazwy zmiennych

`compose.env.example` zawiera `SERVICE_VERSION=` z pustą wartością. Job deployu podstawi tag. Na laptopie `aspire run` nie potrzebuje tagu produkcyjnego.

### 4. Spójność z tunelem

Compose nie publikuje 443. Nie publikuje 8080 na adres inny niż `127.0.0.1`. `cloudflared` z poprzedniego numeru trafia w ten sam port.

### Co jest do czego

| Plik | Kto go czyta |
| --- | --- |
| `AppHost.cs` | `aspire run` i `aspire publish` |
| wygenerowany YAML | `docker compose` na maszynie, w jobie deployu |
| `compose.env` | `docker compose --env-file` na maszynie |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| AppHost tego numeru | `aspire publish -o aspire-output` | w YAML jest usługa bramy i nie ma usługi `postgres` |
| wygenerowany YAML | przegląd portów | `8080` jest związany z `127.0.0.1` |

To nie jest test xUnit. Jest krokiem checkpointu. Test xUnit AppHosta, który podnosi cały skład, dojdzie w fazie testów integracyjnych. Tu wystarczy publish.

## Checkpoint

```bash
aspire publish -o aspire-output
test -f aspire-output/docker-compose.yaml
grep -n gateway aspire-output/docker-compose.yaml
git check-ignore -q aspire-output/docker-compose.yaml
```

`git status` nie pokazuje `aspire-output` jako zmiany do dodania. `grep postgres` w wygenerowanym pliku nic nie znajduje.

Lokalnie:

```bash
docker compose -f aspire-output/docker-compose.yaml --env-file deploy/compose/compose.env.example up -d
curl -fsS http://127.0.0.1:8080/health/live
```

Przykładowy env nie ma haseł, więc brama wstaje. `SERVICE_VERSION` może być puste i `/versions` i tak zwróci sześć pól.

## Umiesz wyjaśnić

Dlaczego wygenerowany YAML nie jest w git?

Bo dwie osoby poprawią dwa źródła. Git trzyma AppHost. YAML jest artefaktem, jak `bin/`.

Co jest źródłem prawdy dla obrazu?

`deploy/docker/Dockerfile`. AppHost tylko wskazuje ten plik i argumenty. Generator nie pisze własnego Dockerfile bramy.

Dlaczego port jest na localhost?

Bo z internetu wchodzi tunel, który stoi na tej samej maszynie. Publikacja na wszystkie interfejsy otwierałaby bramę z pominięciem tunelu w sieci LAN i na każdym interfejsie gościa.

Kiedy w compose pojawi się Postgres?

Gdy numer bazy doda zasób do AppHosta. Nie dopisujemy go ręcznie do YAML.

Po co `SERVICE_VERSION` w środowisku, skoro na laptopie jest pusta?

Bo ten sam compose jedzie na maszynę, gdzie zmienna jest tagiem. Pusta wartość na laptopie jest poprawna. Pusta wartość na maszynie po deployu jest błędem joba deployu, nie tego pliku.

## Zasady

- `aspire-output/` nie jest commitowany.
- Nie ma ręcznego `docker-compose.yaml` w git obok generatora.
- Brama na maszynie słucha tylko na `127.0.0.1:8080`.
- Hasło nie trafia do wygenerowanego `.env` w artefakcie CI.
- AppHost nie dostaje w tym numerze Postgresa ani Kafki.

## Poza zakresem

- Job, który kopiuje YAML i uruchamia go na maszynie, jest w [07-deploy.md](07-deploy.md).
- `otel-lgtm` w tym compose jest w [08-telemetria.md](08-telemetria.md).
- Postgres w AppHost jest w [03-postgres.md](../03-learning-zapis/03-postgres.md).
- Zamiana compose na manifesty jest w [04-deploy-k3s.md](../13-kubernetes/04-deploy-k3s.md).

## Źródła

- [Deploy to Docker Compose](https://aspire.dev/deployment/docker-compose/). Wiążące jest `AddDockerComposeEnvironment` i `aspire publish`. Dotyczy generowania YAML z modelu. Ze źródła nie bierzemy commita wygenerowanego pliku jako źródła prawdy.
- [Docker hosting integration](https://aspire.dev/integrations/compute/docker/). Wiążące jest nadpisanie usługi w kodzie AppHosta. Dotyczy wskazania naszego Dockerfile. Ze źródła nie bierzemy drugiego, wygenerowanego Dockerfile obok `deploy/docker/Dockerfile`.
- [Compose env file](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/). Wiążące jest `--env-file` poza katalogiem gita. Dotyczy `/etc/jjdevhub/compose.env`. Ze źródła nie bierzemy `.env` z hasłem w repozytorium.

## Gotowe gdy

- `aspire publish` produkuje compose z bramą i bez Postgresa.
- Wynik jest ignorowany przez git.
- `GET /health/live` na `127.0.0.1:8080` przechodzi po lokalnym `up`.
- Port nie jest opublikowany na `0.0.0.0`.
- `compose.env.example` ma `SERVICE_VERSION` i pustą wartość.
