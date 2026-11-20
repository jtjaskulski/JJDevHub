# 07. Ewolucja schematu

Poprzedni numer to [odczyt](06-odczyt-dapper.md). Następny to [ślady](08-slady-i-metryki.md).

Ten numer dodaje `LessonPublishedV2` i upcaster. Stare bajty `V1` nadal da się przeczytać. Nowe pole nie zmienia znaczenia starego. `V1` jako typ zostaje w kodzie. Nie edytujemy pól `V1` po to, żeby „dorzucić jeszcze jedno”.

## Po co

Pierwszy konsument na maszynie ma w logu wiadomości bez nowego pola. Jeśli zmienimy `V1` w miejscu, stary payload albo nie wejdzie w nowy rekord, albo wejdzie i pole wymagane będzie puste w sposób, którego nikt nie nazwał. Osobny `V2` i funkcja, która z `V1` robi `V2`, zamyka to jawnie. Upcaster jest w jednym miejscu. Projektor nie ma `if` rozrzuconego po pętli.

## Dlaczego ta technologia

### Pole opcjonalne i nowy numer

`V2` dodaje `string? Level` o wartościach `intro`, `core`, `extra` albo `null`. `null` znaczy „autor nie powiedział”. Problem, który zamyka, to nowe pole wymagane, którego stare wiadomości nie mają: deserializacja pada i wiadomość idzie na DLQ, choć dało się ją zastosować. Odpada zmiana typu istniejącego pola, na przykład `order` z liczby na string. Koszt: dwa rekordy i mapa między nimi.

`Level` nie jest w `V1`. Producent nowego zapisu wysyła `V2`. Wiadomości `V1` już leżące na topicu czytamy przez upcaster.

### Upcaster

`LessonPublishedUpcaster.ToV2(json, schemaVersion)` zwraca `LessonPublishedV2`. Dla wersji `1` czyta `V1` i ustawia `Level` na `null`. Dla wersji `2` czyta `V2`. Dla innej wersji rzuca, a projektor traktuje to jak nieznany typ: DLQ od razu. Problem, który zamyka, to `switch` w trzech konsumentach. Odpada kopiowanie mapowania do Cv: Cv i tak nie czyta lekcji. Gdyby czytało, wzięłoby tę samą klasę z `Learning.Contracts`.

### Kompatybilność w drugą stronę

Nowy projektor czyta stare i nowe. Stary projektor, który zna tylko `V1`, nie czyta `V2` i wrzuca je na DLQ. Dlatego najpierw wjeżdża konsument, potem producent. Problem, który zamyka, to deploy api, które wysyła `V2`, zanim projektor umie je czytać. Odpada jeden tag, w którym oba zmieniają się „naraz” bez kolejności. Koszt: dwa kroki wydania. Składnik `learning` ma jeden numer wersji, więc w jednym tagu jest i api, i projektor. Kolejność procesów w tym tagu: najpierw start projektora, potem relay i api. Compose `depends_on` nie daje „projektor gotowy na V2”. Daje start kontenera. W jednym tagu oba procesy mają ten sam kod, więc umieją `V2` jednocześnie. Kolejność ma znaczenie przy dwóch różnych wersjach na maszynie w czasie rolling update. Na compose, przy jednym kontenerze projektora, podmiana jest zatrzymaniem i startem. Okno jest krótkie. Zasada zostaje zapisana pod fazę klastra: nowy konsument przed nowym producentem, gdy idą osobno. W tym numerze jeden tag niesie oba i test sprawdza, że projektor z tego tagu czyta bajty `V1` i `V2`.

### Rejestr schematów

Nie ma Confluent Schema Registry. Rejestr jest typem, atrybutem i testem kontraktu. Problem, który zamyka, to kolejny serwer poza licencją obrazu. Odpada registry jako usługa. Koszt: nie ma centralnego „kompatybilne / niekompatybilne” poza testem. Test jest bramką.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| upcaster | funkcja ze starego JSON do aktualnego rekordu |
| `V1` | pierwszy kształt, zostaje do odczytu |
| `V2` | kształt z nowym polem, producent go wysyła |
| kompatybilność wstecz | nowy czytnik rozumie stary zapis |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Contracts/Catalog/LessonPublishedV2.cs` | rekord z `Level` |
| `Learning.Contracts/Catalog/LessonPublishedUpcaster.cs` | `ToV2` |
| `Learning.Contracts/ContractJson.cs` | dopisany `[JsonSerializable]` |
| `Learning.Projector/CatalogConsumer.cs` | woła upcaster zamiast deserializować `V1` wprost |
| handler publikacji lekcji | wysyła `V2` |
| `tests/Learning.Contracts.Tests/LessonUpcasterTests.cs` | stare bajty |

## Plan

### 1. Rekord

`LessonPublishedV2` ma wszystkie pola `V1` i `string? Level`. Atrybut `[EventName("learning.lesson-published.v2")]`. `schema-version` w outboxie to `2`. Stary atrybut `V1` zostaje.

### 2. Upcaster

Dla `schemaVersion == 1` deserializacja `LessonPublishedV1` i konstrukcja `V2` z `Level: null`. Dla `2` deserializacja `V2`. Inna liczba: wyjątek `UnknownSchemaException` w projekcie kontraktów, bez Kafki.

### 3. Projektor

Gałąź `lesson-published` woła upcaster. Reszta typów zostaje przy swoim `V1`, dopóki nie mają drugiej wersji. Nieznana wersja lekcji idzie na DLQ.

### 4. Producent

`PublishLessonHandler` buduje `V2`. `Level` pochodzi z komendy i wolno mu być puste. Puste nie jest `intro`. Jest `null`.

### Co jest do czego

| Wersja na topicu | Co robi aktualny projektor |
| --- | --- |
| 1 | upcaster do `V2` z `Level` null, zapis dokumentu |
| 2 | zapis dokumentu z poziomem |
| inna | DLQ |

## Testy

Nazwa: `V1_bytes_become_v2_with_null_level`.

| Arrange | Act | Assert |
| --- | --- | --- |
| JSON wyprodukowany z `LessonPublishedV1` bez pola `level` | `ToV2(json, 1)` | slug się zgadza, `Level` jest `null` |

Drugi fakt: `V2_bytes_keep_level`. Trzeci: `Schema_version_99_throws`.

```bash
dotnet test tests/Learning.Contracts.Tests --nologo --filter "FullyQualifiedName~Upcaster|FullyQualifiedName~V1_bytes"
```

## Checkpoint

```bash
dotnet test tests/Learning.Contracts.Tests tests/Learning.Projector.Tests --nologo --filter "Category!=Performance"
```

Na działającym `aspire run` da się opublikować lekcję z `level=core` i `GET` pokazuje poziom. Wiadomość `V1` wstrzyknięta na topic testowy nadal daje lekcję bez poziomu, nie DLQ.

## Umiesz wyjaśnić

Dlaczego nie dodać pola do `V1`?

Bo `V1` jest kształtem bajtów, które już umiemy nazwać. Ciche pole wymagane w tym samym typie psuje stary JSON albo uczy konsumenta, że „czasem pole jest”. Nowy numer wersji jest tańszy niż zgadywanie.

Co wolno zmienić w `V2`?

Wolno dodać pole, które stary czytnik może zignorować, i wolno nadać mu domyślną wartość w upcasterze. Nie wolno zmienić znaczenia `order` ani typu sluga.

Dlaczego stary projektor nie czyta `V2`?

Bo nie zna typu. Dlatego w wydaniu, w którym procesy jadą osobno, najpierw jest czytnik. W tym tagu kod jest ten sam w obu procesach. Zasada zostaje na rolling update.

Gdzie jest rejestr schematów?

W teście i w atrybucie. Osobny serwer rejestru nie jedzie. Obraz, który go zwykle niesie, i tak nie jest na liście licencji, a sam rejestr nie zastępuje testu, który trzymamy.

Co z wiadomością `V1` po roku?

Upcaster zostaje. Nie kasujemy `LessonPublishedV1`, dopóki na topicu i w logach może leżeć stary JSON. Skasowanie typu jest osobną decyzją, gdy retencja topicu na pewno wyrzuciła stare segmenty i nikt nie trzyma zrzutu.

## Zasady

- `V1` nie dostaje nowego pola.
- Producent wysyła najwyższą wersję, którą ten tag umie czytać.
- Nieznana `schema-version` idzie na DLQ.
- Upcaster jest w `Learning.Contracts`, nie kopiowany do projektora jako prywatna metoda.
- `Level` null nie zamienia się na `intro`.

## Poza zakresem

- Rolling update w klastrze jest w [04-deploy-k3s.md](../13-kubernetes/04-deploy-k3s.md). Tam wraca kolejność konsument przed producentem, jeśli obrazy kiedykolwiek się rozjadą.
- Pact na ten kontrakt jest w [02-kontraktowe.md](../10-testy/02-kontraktowe.md).
- CV nie czyta lekcji.

## Źródła

- [Versioning in an Event Sourced System](https://leanpub.com/esversioning), Greg Young. Wiążące jest to, że zmiana znaczenia dostaje nowy numer, a stary zostaje czytelny. Dotyczy `V2` i upcastera. Ze źródła nie bierzemy upcastera w bazie jako drugiego modelu zapisu Learning: Learning nie jest event sourcingiem, wersjonujemy kontrakt na szynie.
- [JSON Schema evolution](https://docs.confluent.io/platform/current/schema-registry/fundamentals/schema-evolution.html). Wiążące są reguły dodawania pola opcjonalnego. Dotyczy `Level`. Ze źródła nie bierzemy serwera Schema Registry.
- Numer kontraktów w tym folderze. Wiążąca jest nazwa `learning.lesson-published.v1` zostawiona w spokoju. Dotyczy atrybutu `V2` z `v2`. Ze źródła nie bierzemy edycji atrybutu `V1`.

## Gotowe gdy

- Test starych bajtów zwraca `Level` null i ten sam slug.
- Wersja `99` nie jest zapisywana jako lekcja.
- Publikacja z poziomu wysyła `schema-version` 2.
- Klasa `LessonPublishedV1` nadal jest w projekcie.
- Projektor woła upcaster dla lekcji opublikowanej.
