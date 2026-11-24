# 01. Solucja i centralne wersje pakietów

Poprzedni plik to [zasady pracy](../00-fundamenty/zasady-pracy.md). Następny numer to [gałęzie](02-galezie.md).

Ten numer stawia pustą solucję .NET 10. Jeszcze nie ma usługi. Jest przypięty SDK, wspólne właściwości projektów i jedno miejsce na wersje pakietów. Bez tego pierwszy projekt, który dojdzie w późniejszym numerze, wybierze sobie wersję w `.csproj` i reszta solucji się rozjedzie.

W drzewie, które czytasz, projekty i wiersze paczek już są. Ten numer odpowiada za mechanizm. Wpisy, które doszły później, są w krokach podpisane numerem, który je wziął.

## Po co

Dwa projekty z tą samą biblioteką w dwóch wersjach kompilują się do dwóch kopii zestawu. Błąd wychodzi dopiero w czasie działania, gdy typ z jednej kopii nie pasuje do typu z drugiej. Centralna wersja zamyka ten problem, zanim powstanie drugi projekt.

Pusty `dotnet build` na maszynie z innym SDK niż w `global.json` kończy się `NETSDK1045` albo cichym użyciem nowszego kompilatora. Przypięcie SDK sprawia, że laptop i `ubuntu-latest` widzą tę samą linię.

## Dlaczego ta technologia

### `global.json`

`global.json` mówi `dotnet`, którego SDK użyć w tym katalogu. Problem, który zamyka, to przypadek, w którym lokalnie jest SDK 10, a runner ma tylko 8 albo odwrotnie. Odpada brak pliku, bo wtedy wygrywa najnowszy SDK na maszynie. Koszt: podbicie SDK jest osobną zmianą i przechodzi checki, a nie dzieje się przy okazji.

W pliku jest `rollForward: latestFeature` w ramach `10.0`. Poprawka SDK wchodzi. Major nie wchodzi.

### Central Package Management

`Directory.Packages.props` trzyma numer wersji. `.csproj` ma `PackageReference` bez atrybutu `Version`. Problem, który zamyka, to drugi numer tej samej paczki. Odpada wersja wpisana przy referencji: przy włączonym CPM build pada kodem `NU1008`, i o to chodzi. Koszt: nowa paczka wymaga dwóch edycji, pliku wersji i referencji, oraz wiersza w [technologie.md](../00-fundamenty/technologie.md).

`CentralPackageTransitivePinningEnabled` zostaje `false`. Nie przypinamy przechodnich paczek, dopóki konkretny konflikt tego nie wymusi. Przypięcie wszystkiego na zapas ukrywa, skąd wersja się wzięła.

### `Directory.Build.props`

Wspólne właściwości: `Nullable` włączone, `ImplicitUsings` włączone, `TreatWarningsAsErrors` włączone, `AnalysisLevel` na `latest`, `TargetFramework` na `net10.0`. Problem, który zamyka, to projekt, który wyłącza nullable lokalnie i wpuszcza `null` do `Result`. Odpada kopiowanie tych samych `PropertyGroup` do każdego `.csproj`. Koszt: ostrzeżenie w nowym projekcie jest błędem od pierwszej kompilacji, nie od „późniejszego sprzątania”.

### Solucja `slnx`

`JJDevHub.slnx` jest formatem solucji SDK. Problem, który zamyka, to ręcznie edytowany `.sln` z GUID-ami, które konfliktują przy dwóch zmianach naraz. Odpada klasyczny `.sln` jako źródło. Koszt: narzędzie, które nie czyta `slnx`, nie otwiera solucji; `dotnet` i Rider w aktualnej linii czytają.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| SDK | kompilator i cele builda, nie pakiet NuGet aplikacji |
| CPM | Central Package Management, wersja paczki poza `.csproj` |
| `PackageReference` | deklaracja paczki w projekcie, u nas bez numeru wersji |
| `rollForward` | jak daleko `dotnet` może odejść od wersji z `global.json` |
| `PackageVersion` | wiersz wersji w `Directory.Packages.props`, jeden na paczkę |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `global.json` | linia SDK `10.0`, `rollForward: latestFeature`; klucz `test.runner` jest w tym pliku i jest opisany w [buildzie](03-build-testy-format.md) |
| `Directory.Build.props` | właściwości wspólne każdego projektu, w tym `ManagePackageVersionsCentrally` |
| `Directory.Packages.props` | flaga przypinania przechodnich i wiersze `PackageVersion`; wiersze doszły w numerach, które wzięły paczkę |
| `JJDevHub.slnx` | solucja; projekty w środku należą do [Aspire](../02-szkielet-i-deploy/01-aspire.md) i [bramy](../02-szkielet-i-deploy/02-gateway.md) |
| `.editorconfig` | styl, który później czyta job `format` |
| `.gitignore` | `bin/`, `obj/`, `.vs/`, `.idea/`, user secrets, `aspire-output/`, wygenerowany compose |
| `.gitattributes` | `* text=auto eol=lf` |

## Plan

### 1. Przypiąć SDK

W korzeniu powstaje `global.json`. Blok, który ten numer zostawia, to:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

Wersja `10.0.100` jest dolną granicą. Runner instaluje `10.0.x` zgodny z tym plikiem. Jeśli w dniu wdrożenia aktualna poprawka ma wyższy numer builda, w `global.json` zostaje major i minor `10.0`, a `rollForward` wpuszcza poprawkę. Nie wpisuje się `8.0` „na chwilę”.

W tym samym pliku jest drugi klucz, `test.runner` równe `Microsoft.Testing.Platform`. Ten numer go nie uzasadnia. Czyta go `dotnet test` z [numeru builda](03-build-testy-format.md).

Sprawdzenie na laptopie:

```bash
dotnet --version
python3 -c 'import json; sdk=json.load(open("global.json"))["sdk"]; assert sdk["version"].startswith("10.0"); assert sdk["rollForward"]=="latestFeature"; assert sdk["allowPrerelease"] is False'
```

`dotnet --version` zaczyna się od `10.0`. Drugie polecenie kończy się kodem zero.

### 2. Włączyć wspólne właściwości

`Directory.Build.props` wchodzi do każdego projektu, zanim projekt przeczyta własny `.csproj`. Żywy plik ma jeden `PropertyGroup`:

```xml
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
<TargetFramework>net10.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<AnalysisLevel>latest</AnalysisLevel>
```

Testowy projekt, gdy powstanie, nie powtarza tych pól. Pojedynczy `.csproj` nie wyłącza `TreatWarningsAsErrors`, żeby „przeszło”.

Sprawdzenie:

```bash
grep -q '<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>' Directory.Build.props
grep -q '<TreatWarningsAsErrors>true</TreatWarningsAsErrors>' Directory.Build.props
grep -q '<AnalysisLevel>latest</AnalysisLevel>' Directory.Build.props
```

Każde polecenie kończy się kodem zero.

### 3. Włączyć jedno miejsce wersji

`Directory.Packages.props` włącza mechanizm i zostawia przypinanie przechodnich wyłączone:

```xml
<CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>
```

Na zamknięciu tego numeru `ItemGroup` z `PackageVersion` jest pusty. W drzewie, które czytasz, wiersze już są. Każdy wiersz dopisał numer, który wziął paczkę, i ten sam numer ma wiersz w tabeli [technologie.md](../00-fundamenty/technologie.md). Ten plik ich nie powtarza.

Wiersze, które widać dziś, należą do szkieletu i do testów: pięć paczek OpenTelemetry, `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.ServiceDiscovery`, `xunit.v3` i `Shouldly`. Numer wersji stoi tylko tutaj. `.csproj` ma `PackageReference` bez atrybutu `Version`. Paczka, której w tym pliku jeszcze nie ma, dochodzi w numerze, który pierwszy raz jej użyje.

Sprawdzenie:

```bash
grep -q '<CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>' Directory.Packages.props
python3 -c 'import pathlib,re; text=pathlib.Path("Directory.Packages.props").read_text(); names=re.findall(r"Include=\"([^\"]+)\"", text); assert len(names)==len(set(names))'
```

Flaga jest wyłączona. Żadna paczka nie ma dwóch wierszy `PackageVersion`.

### 4. Utworzyć solucję

Solucja powstaje w korzeniu:

```bash
dotnet new sln --name JJDevHub --format slnx
```

Plik nazywa się `JJDevHub.slnx`. Drugiego `.sln` w drzewie nie ma. Format nie niesie GUID-ów. Projekt dokleja się ścieżką:

```xml
<Solution>
  <Folder Name="/src/gateway/">
    <Project Path="src/gateway/JJDevHub.Gateway/JJDevHub.Gateway.csproj" />
  </Folder>
</Solution>
```

To jest kształt jednego wpisu, nie pełna lista. Na zamknięciu tego numeru w solucji nie ma projektu. W drzewie, które czytasz, są:

| Projekt w `JJDevHub.slnx` | Numer, który go dodał |
| --- | --- |
| `src/apphost/JJDevHub.AppHost/JJDevHub.AppHost.csproj` | [Aspire](../02-szkielet-i-deploy/01-aspire.md) |
| `src/service-defaults/JJDevHub.ServiceDefaults/JJDevHub.ServiceDefaults.csproj` | [Aspire](../02-szkielet-i-deploy/01-aspire.md) |
| `tests/JJDevHub.ServiceDefaults.Tests/JJDevHub.ServiceDefaults.Tests.csproj` | [Aspire](../02-szkielet-i-deploy/01-aspire.md) |
| `src/gateway/JJDevHub.Gateway/JJDevHub.Gateway.csproj` | [brama](../02-szkielet-i-deploy/02-gateway.md) |
| `tests/JJDevHub.Gateway.Tests/JJDevHub.Gateway.Tests.csproj` | [brama](../02-szkielet-i-deploy/02-gateway.md) |

Build na dzisiejszym drzewie kompiluje te projekty. Samo istnienie `slnx` nadal nie jest materiałem kompilacji: job `build` z [następnego folderu checków](03-build-testy-format.md) traktuje brak `*.csproj` jako `no material`.

Sprawdzenie:

```bash
dotnet build JJDevHub.slnx --nologo
find . -name '*.sln' -print
```

Build kończy się kodem zero. `find` nic nie wypisuje.

### 5. Złożyć styl i pliki gita

`.gitattributes` ma jedną linię. Koniec linii w repozytorium jest LF, także na laptopie, który domyślnie zapisuje CR LF:

```text
* text=auto eol=lf
```

`.gitignore` zaczyna się od szablonu `dotnet new gitignore`. Z tego szablonu ten numer bierze katalogi builda: `[Bb]in/`, `[Oo]bj/`, `.vs/`, `.idea/`. Na dole pliku jest blok produktu:

```text
# JJDevHub
aspire-output/
*.env
.env.*
!.env.example
*.secret
```

`aspire-output/` to katalog wyjścia publikacji Aspire. `*.env` i `*.secret` trzymają wartość sekretu poza gitem. `!.env.example` zostawia wzór bez wartości. Dalsze linie tego samego pliku (`.sonarqube/`, `.pnpm-store/`, `.expo/`, `kubeconfig`) doszły z numerów klienta, skanu i klastra. Ignorowanie nie wymazuje pliku, który już został dodany.

`.editorconfig` jest kontraktem joba `format`. Ten numer zostawia wcięcia i koniec pliku C#:

```text
root = true

[*]
indent_style = space

[*.cs]
indent_size = 4
tab_width = 4
insert_final_newline = false
```

Reszta pliku to konwencje nazewnictwa i odstępów, które `dotnet format` czyta w [numerze formatu](03-build-testy-format.md). Osobnego pliku stylu na usługę nie ma.

Sprawdzenie:

```bash
test -f .editorconfig
test -f .gitignore
grep -qx '\* text=auto eol=lf' .gitattributes
grep -q 'aspire-output/' .gitignore
grep -q '\[Bb\]in/' .gitignore
grep -q 'insert_final_newline = false' .editorconfig
```

Każde polecenie kończy się kodem zero.

### Co jest do czego

| Plik | Kto go czyta |
| --- | --- |
| `global.json` | `dotnet` na laptopie i `actions/setup-dotnet` w CI; klucz `test.runner` czyta `dotnet test` |
| `Directory.Packages.props` | restore każdego projektu |
| `Directory.Build.props` | build każdego projektu, zanim przeczyta własny `.csproj` |
| `JJDevHub.slnx` | `dotnet build`, `dotnet test`, IDE |
| `.editorconfig` | `dotnet format` w jobie `format` |
| `.gitattributes` | git przy zapisie i przy diffie na runnerze |

## Testy

Ten numer nie dodaje projektu testowego. Materiałem sprawdzającym jest sam build i kształt plików.

| Arrange | Act | Assert |
| --- | --- | --- |
| czysty katalog z plikami tego numeru | `dotnet build JJDevHub.slnx --nologo` | kod wyjścia `0` |
| `Directory.Build.props` z drzewa | odczyt `ManagePackageVersionsCentrally` | wartość `true` |
| `Directory.Packages.props` z drzewa | odczyt `CentralPackageTransitivePinningEnabled` | wartość `false` |
| `global.json` z drzewa | odczyt `sdk.allowPrerelease` | wartość `false` |

Gdyby ktoś wpisał `Version` w projekcie, ten numer jeszcze tego nie złapie własnym testem. Złapie to restore pierwszego projektu, bo CPM jest już włączone. Dlatego pierwszy `.csproj` nie dostaje atrybutu `Version`.

Na dzisiejszym drzewie pierwsza asercja buduje projekty z Aspire i z bramy. Kod zero znaczy, że solucja i wspólne właściwości trzymają się razem. Pusty komunikat „nie ma projektów” był wynikiem w dniu zamknięcia tego numeru, zanim te projekty doszły.

## Checkpoint

```bash
dotnet --version
dotnet build JJDevHub.slnx --nologo
test -f global.json
test -f Directory.Packages.props
test -f Directory.Build.props
python3 -c 'import json; sdk=json.load(open("global.json"))["sdk"]; assert sdk["rollForward"]=="latestFeature" and sdk["allowPrerelease"] is False'
```

`dotnet --version` zaczyna się od `10.0`. Build kończy się kodem zero. Trzy pliki istnieją. Ostatnie polecenie potwierdza `rollForward` i zakaz prerelease.

## Umiesz wyjaśnić

Dlaczego wersja paczki nie stoi w `.csproj`?

Bo dwie wersje tej samej paczki w jednej solucji dają dwa zestawy w czasie działania. Jedno miejsce wersji, `Directory.Packages.props`, sprawia, że rozjazd nie przechodzi restore.

Co robi `rollForward: latestFeature`?

Pozwala na nowszą poprawkę w ramach `10.0` i nie pozwala wskoczyć na kolejny major. Laptop z `10.0.101` buduje solucję przypiętą do `10.0.100`.

Czym kończy się `PackageReference` z własną wersją przy włączonym CPM?

Restore pada kodem `NU1008`. To jest cel, nie usterka narzędzia.

Dlaczego solucja na tym numerze nie ma projektu?

Bo bramki mają istnieć, zanim powstanie pierwsza usługa. Pusty build jest jawnym stanem, nie brakiem pliku solucji. Projekty, które widać w `JJDevHub.slnx` dziś, doszły w Aspire i w bramie.

Co wchodzi do `.gitignore` i dlaczego nie user secrets jako wartość?

Katalog sekretów użytkownika i pliki env. Wartość sekretu nie jest w repozytorium nawet wtedy, gdy plik jest na liście ignorowanych lokalnie u jednej osoby: ignorowanie nie wymazuje pliku, który już został dodany.

Po co w `global.json` jest `test.runner`?

Żeby `dotnet test` szedł przez Microsoft.Testing.Platform, którego używa xunit.v3. Klucz leży w pliku tego numeru. Jego rola jest w [numerze builda i testów](03-build-testy-format.md).

Skąd w `Directory.Packages.props` biorą się numery paczek, skoro ten numer zostawia pustą listę?

Z numeru, który pierwszy raz referencjonuje paczkę. Ten numer włącza CPM i wyłącza przypinanie przechodnich. Wiersz `PackageVersion` i wiersz w tabeli technologii dochodzą razem, później.

## Zasady

- Jeden `TargetFramework`: `net10.0`.
- Żaden `.csproj` nie niesie `Version` paczki.
- `TreatWarningsAsErrors` nie jest wyłączane w pojedynczym projekcie, żeby „przeszło”.
- `global.json` nie używa `allowPrerelease: true`.
- Plik solucji to `JJDevHub.slnx`, bez drugiego `.sln`.
- `CentralPackageTransitivePinningEnabled` zostaje `false`, dopóki konkretny konflikt nie wymusi przypięcia.

## Poza zakresem

- Checki gałęzi i tytułu są w [02-galezie.md](02-galezie.md).
- Job builda jest w [03-build-testy-format.md](03-build-testy-format.md). Ten numer tylko sprawia, że jest co budować, gdy projekty dojdą.
- Klucz `test.runner` czyta ten job. Opis runnera jest tam.
- Pierwsza paczka i pierwszy projekt są w [Aspire](../02-szkielet-i-deploy/01-aspire.md) i w [jądrze](../03-learning-zapis/01-kernel-i-domena.md). Wiersze, które już leżą w `Directory.Packages.props`, są zapisane w [technologie.md](../00-fundamenty/technologie.md).

## Źródła

- [global.json](https://learn.microsoft.com/dotnet/core/tools/global-json). Wiążące są `version`, `rollForward` i `allowPrerelease`. Dotyczy przypięcia linii `10.0`. Ze źródła nie bierzemy `rollForward: latestMajor`, bo to wpuściłoby SDK 11 bez zmiany pliku.
- [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management). Wiążące jest `Directory.Packages.props` i zakaz wersji w `PackageReference`. Dotyczy jednego numeru paczki w całej solucji. Ze źródła nie bierzemy włączonego przypinania przechodnich zależności na zapas.
- [Directory.Build.props](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory). Wiążące jest to, że plik w katalogu nadrzędnym wchodzi do każdego projektu. Dotyczy nullable i ostrzeżeń jako błędów. Ze źródła nie bierzemy osobnego `Directory.Build.props` w każdej usłudze, który wyłącza regułę z korzenia.
- [dotnet new sln](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-sdk-templates#sln). Wiążące jest `--format slnx` i nazwa `JJDevHub`. Dotyczy jednego pliku solucji w korzeniu. Ze źródła nie bierzemy klasycznego `.sln` jako drugiego źródła.

## Gotowe gdy

- `dotnet build JJDevHub.slnx` zwraca kod zero na SDK `10.0`.
- `global.json` ma `rollForward` równe `latestFeature` i nie ma `allowPrerelease: true`.
- `Directory.Build.props` włącza CPM, `Nullable`, `ImplicitUsings` i `TreatWarningsAsErrors`.
- `Directory.Packages.props` ma `CentralPackageTransitivePinningEnabled` równe `false`. Wiersze `PackageVersion` w dzisiejszym pliku należą do numerów, które wzięły paczkę, i mają odpowiednik w tabeli technologii. Ten numer sam nie dopisuje paczki.
- W drzewie nie ma drugiego pliku solucji.
- `.gitignore` obejmuje `bin/`, `obj/` i katalog wyjścia publikacji Aspire.
