# 01. Solucja i centralne wersje pakietów

Poprzedni plik to [zasady pracy](../00-fundamenty/zasady-pracy.md). Następny numer to [gałęzie](02-galezie.md).

Ten numer stawia pustą solucję .NET 10. Jeszcze nie ma usługi. Jest przypięty SDK, wspólne właściwości projektów i jedno miejsce na wersje pakietów. Bez tego pierwszy projekt, który dojdzie w późniejszym numerze, wybierze sobie wersję w `.csproj` i reszta solucji się rozjedzie.

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

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `global.json` | linia SDK `10.0`, `rollForward: latestFeature` |
| `Directory.Build.props` | właściwości wspólne każdego projektu |
| `Directory.Packages.props` | wersje paczek; na tym numerze plik istnieje i ma włączone CPM, lista paczek jest pusta |
| `JJDevHub.slnx` | solucja bez projektów |
| `.editorconfig` | styl, który później czyta job `format` |
| `.gitignore` | `bin/`, `obj/`, `.vs/`, `.idea/`, user secrets, `aspire-output/`, wygenerowany compose |
| `.gitattributes` | `* text=auto eol=lf` |

## Plan

### 1. Przypiąć SDK

W korzeniu:

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

### 2. Włączyć CPM i właściwości

`Directory.Build.props` ustawia `ManagePackageVersionsCentrally` na `true`, `TargetFramework` na `net10.0`, `Nullable` na `enable`, `TreatWarningsAsErrors` na `true`, `ImplicitUsings` na `enable`. Testowy projekt, gdy powstanie, nie powtarza tych pól.

`Directory.Packages.props` ma pusty zestaw `ItemGroup`. Pierwsza paczka dojdzie w numerze, który jej użyje, i jednocześnie w tabeli [technologie.md](../00-fundamenty/technologie.md).

### 3. Utworzyć solucję i pliki gita

`dotnet new sln --name JJDevHub --format slnx`. Do `.gitignore` wchodzą katalogi builda, katalog wyjścia Aspire i plik env. Do `.gitattributes` wchodzi koniec linii LF, żeby diff na runnerze nie składał się z samych powrotów karetki.

### 4. Sprawdzić, że pusta solucja się buduje

`dotnet build JJDevHub.slnx` kończy się kodem zero i komunikatem, że nie ma projektów do zbudowania. To jest oczekiwane. Job `build` z następnych numerów potraktuje ten stan jako brak materiału tylko wtedy, gdy w drzewie nie ma żadnego `.csproj`. Samo istnienie `slnx` nie jest materiałem kompilacji.

### Co jest do czego

| Plik | Kto go czyta |
| --- | --- |
| `global.json` | `dotnet` na laptopie i `actions/setup-dotnet` w CI |
| `Directory.Packages.props` | restore każdego projektu |
| `Directory.Build.props` | build każdego projektu, zanim przeczyta własny `.csproj` |
| `JJDevHub.slnx` | `dotnet build`, `dotnet test`, IDE |

## Testy

Ten numer nie dodaje projektu testowego. Materiałem sprawdzającym jest sam build.

| Arrange | Act | Assert |
| --- | --- | --- |
| czysty katalog z plikami tego numeru | `dotnet build JJDevHub.slnx --nologo` | kod wyjścia `0` |

Gdyby ktoś wpisał `Version` w nieistniejącym jeszcze projekcie, ten numer jeszcze tego nie złapie. Złapie to pierwszy projekt, bo CPM jest już włączone. Dlatego pierwszy `.csproj` nie dostaje atrybutu `Version`.

## Checkpoint

```bash
dotnet --version
dotnet build JJDevHub.slnx --nologo
test -f global.json
test -f Directory.Packages.props
test -f Directory.Build.props
```

`dotnet --version` zaczyna się od `10.0`. Build kończy się kodem zero. Trzy pliki istnieją.

## Umiesz wyjaśnić

Dlaczego wersja paczki nie stoi w `.csproj`?

Bo dwie wersje tej samej paczki w jednej solucji dają dwa zestawy w czasie działania. Jedno miejsce wersji, `Directory.Packages.props`, sprawia, że rozjazd nie przechodzi restore.

Co robi `rollForward: latestFeature`?

Pozwala na nowszą poprawkę w ramach `10.0` i nie pozwala wskoczyć na kolejny major. Laptop z `10.0.101` buduje solucję przypiętą do `10.0.100`.

Czym kończy się `PackageReference` z własną wersją przy włączonym CPM?

Restore pada kodem `NU1008`. To jest cel, nie usterka narzędzia.

Dlaczego solucja na tym numerze nie ma projektu?

Bo bramki mają istnieć, zanim powstanie pierwsza usługa. Pusty build jest jawnym stanem, nie brakiem pliku solucji.

Co wchodzi do `.gitignore` i dlaczego nie user secrets jako wartość?

Katalog sekretów użytkownika i pliki env. Wartość sekretu nie jest w repozytorium nawet wtedy, gdy plik jest na liście ignorowanych lokalnie u jednej osoby: ignorowanie nie wymazuje pliku, który już został dodany.

## Zasady

- Jeden `TargetFramework`: `net10.0`.
- Żaden `.csproj` nie niesie `Version` paczki.
- `TreatWarningsAsErrors` nie jest wyłączane w pojedynczym projekcie, żeby „przeszło”.
- `global.json` nie używa `allowPrerelease: true`.
- Plik solucji to `JJDevHub.slnx`, bez drugiego `.sln`.

## Poza zakresem

- Checki gałęzi i tytułu są w [02-galezie.md](02-galezie.md).
- Job builda jest w [03-build-testy-format.md](03-build-testy-format.md). Ten numer tylko sprawia, że jest co budować, gdy projekty dojdą.
- Pierwsza paczka i pierwszy projekt są w [Aspire](../02-szkielet-i-deploy/01-aspire.md) i w [jądrze](../03-learning-zapis/01-kernel-i-domena.md).

## Źródła

- [global.json](https://learn.microsoft.com/dotnet/core/tools/global-json). Wiążące są `version`, `rollForward` i `allowPrerelease`. Dotyczy przypięcia linii `10.0`. Ze źródła nie bierzemy `rollForward: latestMajor`, bo to wpuściłoby SDK 11 bez zmiany pliku.
- [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management). Wiążące jest `Directory.Packages.props` i zakaz wersji w `PackageReference`. Dotyczy jednego numeru paczki w całej solucji. Ze źródła nie bierzemy włączonego przypinania przechodnich zależności na zapas.
- [Directory.Build.props](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory). Wiążące jest to, że plik w katalogu nadrzędnym wchodzi do każdego projektu. Dotyczy nullable i ostrzeżeń jako błędów. Ze źródła nie bierzemy osobnego `Directory.Build.props` w każdej usłudze, który wyłącza regułę z korzenia.

## Gotowe gdy

- `dotnet build JJDevHub.slnx` zwraca kod zero na SDK `10.0`.
- `global.json` ma `rollForward` równe `latestFeature` i nie ma `allowPrerelease: true`.
- `Directory.Packages.props` włącza CPM i nie zawiera paczki bez wiersza w tabeli technologii. Na tym numerze lista jest pusta.
- W drzewie nie ma drugiego pliku solucji.
- `.gitignore` obejmuje `bin/`, `obj/` i katalog wyjścia publikacji Aspire.
