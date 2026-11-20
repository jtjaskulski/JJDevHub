# 02. Test kontraktu

Poprzedni numer to [integracja](01-integracyjne.md). Następny to [wydajność](03-wydajnosciowe.md).

Ten numer spisuje kontrakt między Learning a Cv: kształt `IntegrationStateV1` na `learning.integration`. Cv jest konsumentem. Learning jest dostawcą. Pact sprawdza, że dostawca nadal wystawia to, czego konsument oczekuje. Nie zastępuje testu kopii.

## Po co

Test projektora CV podstawia JSON, który sam napisał. Learning może zmienić pole i ten test zostanie zielony, bo JSON w teście CV jest stary. Kontrakt zamyka tę zgodę na rozjazd. Zmiana pola w Learning bez aktualizacji oczekiwania CV pada w CI.

## Dlaczego ta technologia

### PactNet

PactNet 5 jest na MIT. Konsument w teście Cv generuje plik paktu. Dostawca w teście Learning weryfikuje go na hoście, który wystawia stan tak, jak relay by go zserializował, albo weryfikuje serializację `ContractJson` bez brokera. Problem, który zamyka, to wspólna baza „integracyjna”, która tylko sprawdza, że oba procesy żyją, a nie że JSON się zgadza. Odpada porównywanie dwóch ręcznie napisanych stringów w dwóch repozytoriach: repozytorium jest jedno, pokusa jest ta sama. Koszt: plik paktu jest generowany do `tests/pacts/` i jest commitowany, żeby widać było zmianę w diffie. Generowany plik nie liczy się do `pr-size` tylko jeśli jest w wyłączeniu. Lepiej, żeby się liczył: zmiana kontraktu ma być widoczna. Nie dodajemy wyłączenia.

### Zakres

Pakt obejmuje: slug, nazwy `pl` i `en`, `deleted`, `stateVersion`, `kind=technology`. Nie obejmuje lekcji. Problem, który zamyka, to pakt na całe API Learning, który pęka przy każdej zmianie REST i nie mówi nic o CV. Odpada pakt na `GET /cv`. To jest inny klient, przeglądarka, i ma OpenAPI.

### Weryfikacja dostawcy

Test Learning buduje `IntegrationStateV1` i sprawdza pakt. Stan dostawcy bierze się z handlera albo z czystej serializacji, jeśli broker nie jest potrzebny do kształtu. Kształt nie potrzebuje brokera. Test jest jednostkowy z cechą `Category=Contract`. Job `integration-tests` ją uruchamia, zgodnie ze słownikiem. Nie wymaga kontenera, więc może być szybki. Zostaje w tym jobie, nie w `unit-tests`, żeby `unit-tests` nie zaczął zależeć od narzędzia paktu, jeśli ściąga natywny rdzeń. Jeśli PactNet w tej wersji ściąga binarkę, job integracyjny to uniesie. `unit-tests` jej nie dotyka.

### Zmiana łamiąca

Zmiana nazwy pola jest zmianą `V2` stanu, nie cichą edycją `V1`. Pakt `V1` zostaje, dopóki CV nie przejdzie na `V2`. Problem, który zamyka, to „poprawiliśmy nazwę w JSON i pakt”. Odpada edycja `IntegrationStateV1` w miejscu. Nowy pakt jest na nowy typ.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| pakt | zapis oczekiwania konsumenta wobec dostawcy |
| konsument | Cv |
| dostawca | Learning |
| `Category=Contract` | cecha faktu, leci w `integration-tests` |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `tests/Cv.ContractTests/LearningIntegrationConsumerPact.cs` | oczekiwanie CV |
| `tests/Learning.ContractTests/IntegrationStateProviderPact.cs` | weryfikacja |
| `tests/pacts/` | wygenerowany JSON paktu, commitowany |

## Plan

### 1. Konsument

Test opisuje interakcję: wiadomość o slugu `dotnet`, nazwie i `deleted=false`. Generuje pakt.

### 2. Dostawca

Test czyta pakt i sprawdza, że `ContractJson` serializacja przykładu z Learning spełnia oczekiwania. Przykład jest w teście dostawcy i musi być tym, co handler naprawdę wysyła. Dlatego przykład buduje się tą samą metodą mapującą co handler, wystawioną internal i widoczną dla testu przez `InternalsVisibleTo`. Nie ma drugiej, ręcznej kopii pól w teście dostawcy.

### 3. CI

Job `integration-tests` nie kończy się `no material` dla tej cechy, gdy projekty istnieją. Padnięcie paktu jest czerwonym checkiem.

### Co jest do czego

| Strona | Co mówi |
| --- | --- |
| Cv | jakiego JSON potrzebuje kopia |
| Learning | czy nadal taki JSON wychodzi z mapowania |

## Testy

Nazwa: `Technology_state_matches_cv_pact`.

| Arrange | Act | Assert |
| --- | --- | --- |
| pakt konsumenta i przykład z mapowania Learning | weryfikacja PactNet | brak naruszeń |

```bash
dotnet test tests/Learning.ContractTests tests/Cv.ContractTests --nologo --filter "Category=Contract"
```

## Checkpoint

```bash
dotnet test tests/Cv.ContractTests --nologo
dotnet test tests/Learning.ContractTests --nologo
git diff -- tests/pacts
```

Po pierwszym przebiegu plik paktu jest w git. Drugi przebieg bez zmian kodu nie zmienia pliku. Usunięcie pola `deleted` z mapowania Learning powoduje czerwony test dostawcy.

## Umiesz wyjaśnić

Dlaczego nie wystarczy test projektora z wklejonym JSON?

Bo ten JSON napisał ten sam człowiek, co projektor. Learning może przestać wysyłać pole, a wklejka zostanie. Pakt wiąże oczekiwanie CV z serializacją Learning.

Dlaczego nie obejmujemy całego REST?

Bo przeglądarka ma OpenAPI i własne testy. Ten pakt jest o wiadomości, której nie ma w OpenAPI strony. Mieszanie obu daje pakt, którego nikt nie czyta.

Czemu plik paktu jest w git?

Bo zmiana oczekiwania ma być widoczna w diffie pull requestu, a nie tylko w artefakcie przebiegu, który znika.

Co z `V2`?

Nowy typ, nowy pakt. Stary pakt zostaje, dopóki konsument czyta `V1`. Nie edytujemy paktu `V1`, żeby pasował do niekompatybilnej zmiany.

Dlaczego cecha nie jest w `unit-tests`?

Bo narzędzie paktu może ciągnąć natywny komponent. Jednostkowe mają zostać szybkie i bez tego. Job integracyjny i tak jest wymagany.

## Zasady

- Pakt dotyczy `IntegrationStateV1`, nie całego Learning.
- Mapowanie w teście dostawcy jest tym samym kodem co w handlerze.
- Plik paktu jest commitowany.
- Zmiana łamiąca dostaje `V2`, nie edycję `V1`.
- Cv nie dostaje w pakcie connection stringa ani lekcji.
- `Category=Contract` nie leci w filtrze jednostkowym.

## Poza zakresem

- OpenAPI webu jest w [03-klient-openapi.md](../11-web/03-klient-openapi.md).
- Wydajność jest w [03-wydajnosciowe.md](03-wydajnosciowe.md).
- Inne pary usług dostaną pakt, gdy druga strona zacznie zależeć od kształtu. Nie robimy pustych paktów „na przyszłość”.

## Źródła

- [Pact](https://docs.pact.io/). Wiążące jest oczekiwanie konsumenta weryfikowane u dostawcy. Dotyczy stanu technologii. Ze źródła nie bierzemy paktu na każdy endpoint REST.
- [PactNet](https://github.com/pact-foundation/pact-net). Wiążąca jest licencja MIT i test w procesie. Dotyczy wyboru biblioteki. Ze źródła nie bierzemy brokera paktów jako usługi obowiązkowej: plik w git wystarcza w jednym repozytorium.
- Numer kopii technologii. Wiążące są pola, bez których strona kłamie. Dotyczy zakresu paktu. Ze źródła nie bierzemy kursu w tym pakcie.

## Gotowe gdy

- Weryfikacja dostawcy przechodzi na commitowanym pakcie.
- Usunięcie pola używanego przez CV psuje test.
- Pakt nie zawiera lekcji ani sekretu.
- Cecha `Contract` jest w jobie integracyjnym i poza filtrem jednostkowym.
- Handler i test dostawcy dzielą jedno mapowanie.
