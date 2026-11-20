# 03. Jeden Dockerfile

Poprzedni numer to [brama](02-gateway.md). Następny to [maszyna](04-maszyna.md).

Ten numer dodaje `deploy/docker/Dockerfile`. Jeden plik buduje każdy host .NET: bramę, api, relay i projektor. Argument `PROJECT` wybiera, który projekt wchodzi do obrazu. Od tego commita checki `image-scan`, `sbom` i `attestations` przestają kończyć się komunikatem `no material` dla bramy.

## Po co

Dockerfile na usługę rozjeżdża się przy pierwszej poprawce warstwy: jedna usługa dostaje obraz chiseled, druga zostaje na pełnym Debianie, trzecia kopiuje sekrety do warstwy builda. Jeden plik zamyka ten rozjazd.

Bez cache warstw każdy commit przebudowuje restore. Kolejność instrukcji ma zostać taka, żeby zmiana `.cs` nie unieważniała restore paczek.

## Dlaczego ta technologia

### Multi-stage i framework-dependent

Pierwszy etap to SDK, restore i `dotnet publish`. Ostatni etap to `mcr.microsoft.com/dotnet/aspnet:10.0` dla projektów web albo `mcr.microsoft.com/dotnet/runtime:10.0` dla workera, wybierane argumentem `BASE_IMAGE`. Publikacja jest framework-dependent, z RID `linux-x64` albo `linux-arm64` według architektury BuildKit. Problem, który zamyka, to obraz, który niesie SDK i źródła. Odpada self-contained jako domyślne, bo pięć kopii runtime w pięciu obrazach nic tu nie upraszcza, a łatka runtime jest podbiciem obrazu bazowego. Koszt: na maszynie musi być zgodny runtime w obrazie bazowym, nie na hoście. Host ma tylko Dockera.

Self-contained zostaje opisane w `## Pojęcia`, żeby było wiadomo, czego świadomie nie bierzemy. Nie ma drugiego Dockerfile „self-contained na wszelki wypadek”.

### Obraz chiseled jako wariant, nie jako jedyny

Argument `BASE_IMAGE` pozwala zbudować wariant `aspnet:10.0-chiseled`. Domyślny build bramy używa obrazu z powłoką, dopóki healthcheck i diagnoza na maszynie tego potrzebują. Problem, który zamyka, to brak narzędzi w obrazie w dniu, w którym pierwszy raz coś nie wstaje. Odpada chiseled jako jedyny obraz od pierwszego deployu. Koszt: obraz jest większy. Przełączenie na chiseled jest osobną zmianą argumentu, po tym jak probe z [health](../04-zdarzenia/09-health-i-odpornosc.md) działają.

### Użytkownik nie-root

Ostatni etap tworzy użytkownika o stałym uid i przełącza się na niego przed `ENTRYPOINT`. Problem, który zamyka, to proces bramy jako root. Odpada obraz, który „na razie” zostaje rootem. Koszt: katalog na log plikowy nie jest potrzebny, bo log idzie na stdout. Nie montujemy katalogu, do którego ten użytkownik nie może pisać.

### `.dockerignore`

Kontekst builda nie zawiera `.git`, `tests`, `**/bin`, `**/obj`, `src/clients`. Problem, który zamyka, to sekret z katalogu roboczego wpadający do kontekstu. Odpada brak ignore. Koszt: plik trzeba zaktualizować, gdy dojdzie nowy katalog, który nie ma być w obrazie.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| stage | etap Dockerfile; do obrazu końcowego wchodzi tylko ostatni |
| framework-dependent | obraz niesie aplikację, runtime jest w obrazie bazowym |
| self-contained | publikacja z własną kopią runtime; u nas nie jest domyślna |
| RID | identyfikator środowiska, `linux-x64` albo `linux-arm64` |
| chiseled | obraz bazowy bez powłoki i bez menedżera pakietów |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `deploy/docker/Dockerfile` | jedyny Dockerfile procesów .NET |
| `.dockerignore` | co nie wchodzi do kontekstu |

Obrazy frontu, gdy dojdą, dostaną własny plik w numerze klienta. Nie rozszerzają tego pliku o Node.

## Plan

### 1. Kolejność warstw

```dockerfile
# syntax=docker/dockerfile:1
ARG PROJECT
ARG BASE_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet publish src/${PROJECT}/${PROJECT}.csproj -c Release -o /out
FROM ${BASE_IMAGE} AS final
WORKDIR /app
COPY --from=build /out ./
USER 10001
ENTRYPOINT ["dotnet", "/app/app.dll"]
```

Fragment jest szkieletem. W planie wdrożenia nazwa zestawu wynikowego jest znana: publish z `-p:AssemblyName` albo `ENTRYPOINT` wskazuje dll o nazwie projektu, przekazaną drugim argumentem `ASSEMBLY`. Nie zgadujemy nazwy `app.dll`, jeśli projekt nazywa się `JJDevHub.Gateway`. Argument `ASSEMBLY` jest obowiązkowy. Brama woła się z `ASSEMBLY=JJDevHub.Gateway.dll`.

Restore przed kopiowaniem wszystkich źródeł jest celem. Jeśli jeden `COPY src/` unieważnia cache przy każdej zmianie, kolejna poprawka tego pliku rozdziela kopiowanie `*.csproj` od kopiowania reszty. Ten numer zostawia to rozdzielenie, gdy pierwszy build pokaże, że cache nie działa. Checkpoint porównuje dwa buildy pod rząd: drugi ma hit na restore.

### 2. Czego plik nie robi

Nie kopiuje `appsettings.Development.json` osobno od publikacji. Publikacja bierze pliki projektu. W tych plikach nie ma hasła, co sprawdził już gitleaks i co powtarza zasada: puste wartości tam, gdzie klucz jest.

Nie ustawia `ENV ConnectionStrings__...`. Connection string przychodzi z compose albo z AppHosta w czasie uruchomienia.

### 3. Checki obrazu

Od tego numeru [artifacts.yml](../01-bramki/08-obrazy.md) buduje co najmniej `PROJECT=JJDevHub.Gateway`. Skan Trivy obowiązuje. Jeśli obraz bazowy ma CVE wysokie, numer nie obchodzi progu wpisem do allowlisty bez daty i powodu. Najpierw podbicie tagu obrazu bazowego.

### Co jest do czego

| Argument | Kto go podaje |
| --- | --- |
| `PROJECT` | workflow obrazu i compose |
| `BASE_IMAGE` | workflow, domyślnie aspnet |
| `ASSEMBLY` | ten sam workflow, dll projektu |

## Testy

| Arrange | Act | Assert |
| --- | --- | --- |
| zbudowany obraz bramy | `docker run --rm` z `SERVICE_VERSION=gateway-v0.0.0` i mapowaniem portu | `GET /health/live` zwraca `200` |
| ten sam obraz | proces w kontenerze | uid nie jest `0` |

Nazwa nie jest testem xUnit. Jest poleceniem checkpointu. xUnit nie podnosi Dockera w `unit-tests`.

## Checkpoint

```bash
docker build -f deploy/docker/Dockerfile \
  --build-arg PROJECT=JJDevHub.Gateway \
  --build-arg ASSEMBLY=JJDevHub.Gateway.dll \
  -t jjdevhub-gateway:dev .
docker run --rm -d --name jjdevhub-gw-test -p 18080:8080 \
  -e SERVICE_VERSION=gateway-v0.0.0 jjdevhub-gateway:dev
curl -fsS http://127.0.0.1:18080/health/live
docker inspect --format '{{.Config.User}}' jjdevhub-gateway:dev
docker rm -f jjdevhub-gw-test
```

`curl` zwraca sukces. `inspect` pokazuje użytkownika innego niż root. Kestrel w obrazie słucha na `8080`. `ASPNETCORE_HTTP_PORTS=8080` jest w Dockerfile jako `ENV`, nie w sekrecie.

## Umiesz wyjaśnić

Dlaczego jeden Dockerfile, a nie plik na usługę?

Bo różnica między usługami jest nazwą projektu, nie kształtem obrazu. Pięć plików rozjedzie się przy pierwszej poprawce bezpieczeństwa obrazu bazowego.

Czym framework-dependent różni się od self-contained?

W pierwszym runtime jest w obrazie bazowym i łatka runtime to nowy obraz bazowy. W drugim runtime jest w publikacji i łatka wymaga przebudowy każdej usługi nawet bez zmiany kodu. Bierzemy pierwsze.

Dlaczego nie chiseled od razu?

Bo pierwszy deploy ma dać się zdiagnozować. Chiseled nie ma powłoki. Wejdzie jako zmiana `BASE_IMAGE`, gdy probe wystarczą do diagnozy.

Co nie może trafić do kontekstu builda?

`.git`, testy, katalogi `bin` i `obj`, katalogi klientów, plik env. Ignorowanie jest po to, żeby sekret z katalogu roboczego nie wszedł do warstwy przypadkiem.

Dlaczego uid jest stałe?

Żeby mount i polityka klastra mogły wskazać tego samego użytkownika. Root w kontenerze jest rootem na hoście w zakresie uprawnień procesu.

## Zasady

- Nie ma drugiego Dockerfile dla procesów .NET.
- Obraz nie zawiera SDK.
- Proces nie działa jako root.
- Hasło nie jest `ENV` w Dockerfile.
- Domyślny tag lokalny `dev` nie jest tagiem, który deploy wgrywa na maszynę.

## Poza zakresem

- Maszyna, na której obraz będzie działał, jest w [04-maszyna.md](04-maszyna.md).
- Compose, które ten obraz uruchamia, jest w [06-compose.md](06-compose.md).
- Wypchnięcie do GHCR jest w [07-deploy.md](07-deploy.md).
- Dockerfile webu jest w [02-szkielet.md](../11-web/02-szkielet.md).

## Źródła

- [Dockerfile reference](https://docs.docker.com/reference/dockerfile/). Wiążące są etapy, `USER` i kolejność `COPY` przed tym, co zmienia się często. Dotyczy jednego pliku. Ze źródła nie bierzemy sekretu w `ENV`.
- [dotnet publish](https://learn.microsoft.com/dotnet/core/tools/dotnet-publish). Wiążące jest rozróżnienie framework-dependent i self-contained oraz RID. Dotyczy etapu build. Ze źródła nie bierzemy self-contained jako domyślnego trybu.
- [.NET chiseled containers](https://devblogs.microsoft.com/dotnet/announcing-dotnet-chiseled-containers/). Wiążące jest to, że obraz nie ma powłoki. Dotyczy wariantu `BASE_IMAGE`, nie domyślnego tagu. Ze źródła nie bierzemy chiseled jako jedynego obrazu w dniu pierwszego deployu.

## Gotowe gdy

- `docker build` bramy kończy się obrazem, którego `GET /health/live` zwraca `200`.
- Użytkownik procesu nie jest rootem.
- Checki obrazu z numeru artefaktów budują ten Dockerfile i nie logują już `no material` dla bramy.
- W drzewie nie ma second Dockerfile dla .NET.
- `.dockerignore` wyklucza `.git` i `src/clients`.
