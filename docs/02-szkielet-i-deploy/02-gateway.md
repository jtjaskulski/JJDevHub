# 02. Brama i `GET /versions`

Poprzedni numer to [Aspire](01-aspire.md). Następny to [Dockerfile](03-dockerfile.md).

Ten numer stawia `JJDevHub.Gateway` na YARP i endpoint `GET /versions`. Brama jest jedynym wejściem HTTP klientów. Na tym etapie nie ma jeszcze usług do przekazania, więc trasy usług istnieją w konfiguracji i wskazują nazwy, których AppHost jeszcze nie uruchamia. Działa trasa własna bramy: `/versions` i health.

## Po co

Przeglądarka, która zna pięć portów, zna pięć miejsc do podmiany i pięć certyfikatów. Jeden adres zamyka ten problem. `GET /versions` zamyka inne: na ekranie widać, który obraz odpowiada za treść. Bez tego „u mnie działa” nie ma numeru, do którego da się wrócić.

Pusty numer usługi to pusty napis, nie brak pola. Klient rysuje wiersz `version.missing` zamiast udawać, że usługi nie ma w produkcie.

## Dlaczego ta technologia

### YARP

YARP jest biblioteką odwrotnego proxy w procesie ASP.NET. Problem, który zamyka, to druga brama obok aplikacji, z własnym językiem reguł. Odpada Traefik i osobny Nginx jako brama produktu. Koszt: brama jest projektem, który trzeba wydawać tagiem `gateway-vX.Y.Z`. To jest zamierzone: brama jest komponentem, nie dodatkiem do klastra.

Na k3s Traefik i ServiceLB są wyłączone z tego samego powodu, w [numerze manifestów](../13-kubernetes/02-manifesty.md). Tu zapada decyzja, tam jej nie powtarzamy.

### Konfiguracja tras w kodzie i w pliku

Trasy produktu są w `appsettings.json` bez sekretów: klaster wskazuje nazwę usługi (`http://learning`), a nie adres laptopa. Problem, który zamyka, to trasa dopisana tylko w pamięci osoby, która stawia środowisko. Odpada trasa wyklinana na maszynie. Koszt: nowa usługa to zmiana tego pliku i AppHosta w numerze, który usługę dodaje.

### Wersja z środowiska

`SERVICE_VERSION` przychodzi z deployu i jest pełnym tagiem, na przykład `gateway-v0.1.0`. Brama woła `GET /version` sąsiadów, zbiera sześć pól i zwraca JSON. Problem, który zamyka, to wersja wpisana w kodzie, która kłamie po następnym obrazie. Odpada składanie numeru z `AssemblyVersion` bez tagu. Koszt: brak zmiennej daje pusty napis także dla samej bramy i test to widzi. Deploy ma tę zmienną ustawiać. Obraz bez niej nie jest wydaniem, co sprawdza numer deployu.

Timeout sąsiada nie przewraca całego `GET /versions`. Pole sąsiada jest puste, brama zwraca `200` i swój numer, jeśli go ma.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| trasa | dopasowanie ścieżki do klastra |
| klaster | cel YARP, u nas nazwa usługi |
| `SERVICE_VERSION` | tag komponentu wstrzyknięty przy starcie procesu |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/gateway/JJDevHub.Gateway/Program.cs` | YARP, `/versions`, ServiceDefaults |
| `src/gateway/JJDevHub.Gateway/appsettings.json` | trasy bez adresów maszynowych |
| `src/gateway/JJDevHub.Gateway/Versions/VersionReport.cs` | złożenie sześciu pól |
| `tests/JJDevHub.Gateway.Tests/` | fakt kształtu JSON |

AppHost dostaje `AddProject` bramy. To jest pierwsza poprawka AppHosta od poprzedniego numeru.

## Plan

### 1. Trasy

W konfiguracji YARP klastry: `identity`, `news`, `learning`, `cv`, `contact`. Ścieżki zgodnie ze słownikiem: `/identity/{**catch-all}` i analogicznie, `/connect/{**catch-all}`, `/.well-known/{**catch-all}` na identity, `/graphql/{**catch-all}` na learning. `/versions` nie jest trasą proxy. Jest endpointem bramy.

Adres docelowy klastra na laptopie rozwiązuje service discovery Aspire. W pliku nie ma `localhost` z portem.

### 2. `GET /versions`

Rekord odpowiedzi ma dokładnie sześć napisów: `identity`, `news`, `learning`, `cv`, `contact`, `gateway`. Implementacja woła `GET /version` na każdy klaster z timeoutem dwóch sekund. Wyjątek albo status inny niż `200` daje pusty napis. `gateway` bierze się z `SERVICE_VERSION`, nie z wołania samej siebie.

`GET /version` na bramie zwraca tylko własny tag. Usługi dostaną taki sam endpoint, gdy powstaną. Kontrakt jest tu, żeby brama miała kogo wołać.

### 3. Autoryzacja

Na tym numerze brama nie wymaga tokenu na `/versions` i na health. Nie waliduje jeszcze JWT: nie ma klucza publicznego Identity. Walidacja dochodzi w [03-jwt](../05-tozsamosc/03-jwt-i-policies.md). Trasy proxy i tak nie mają celu, który odpowiada. To nie jest otwarte API produktu, tylko szkielet.

### 4. Test kształtu

Test hosta z `WebApplicationFactory` podstawia sąsiadów, którzy nie odpowiadają, i zmienną `SERVICE_VERSION=gateway-v0.1.0`.

### Co jest do czego

| Element | Kto go woła |
| --- | --- |
| YARP | każde żądanie klienta poza `/versions` i health |
| `VersionReport` | `GET /versions` |
| `GET /version` | brama, wobec każdej usługi, i człowiek przy diagnozie jednego procesu |

## Testy

Nazwa: `Versions_returns_six_fields_when_neighbors_are_down`.

| Arrange | Act | Assert |
| --- | --- | --- |
| `SERVICE_VERSION=gateway-v0.1.0`, sąsiedzi nie nasłuchują | `GET /versions` | `200`, sześć pól, `gateway` równe tagowi, reszta pusta |

```bash
dotnet test tests/JJDevHub.Gateway.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/JJDevHub.Gateway.Tests --nologo
aspire run
```

Przy działającym `aspire run` `GET` na port bramy `/versions` zwraca JSON z sześcioma kluczami. Wartość `gateway` zależy od tego, czy zmienna jest ustawiona. Lokalnie bez zmiennej pole jest puste i test z fabryki i tak przechodzi, bo zmienną ustawia test.

## Umiesz wyjaśnić

Dlaczego puste pole, a nie brak klucza?

Bo klient ma stałą kolejność wierszy. Brak klucza wygląda jak zmiana kontraktu. Pusty napis znaczy „ten komponent nie odpowiedział albo nie jest wdrożony”.

Dlaczego timeout sąsiada nie daje `502` na `/versions`?

Bo endpoint mówi o wersjach, nie jest proxy tej usługi. Jedna leżąca usługa nie może ukryć numeru bramy i numerów pozostałych.

Czemu YARP, a nie reguły w Cloudflare?

Cloudflare kończy tunel. Nie zna usług w środku. Podział tras produktu zostaje w repozytorium, obok kodu.

Co brama wolno robić z ciałem żądania?

Przekazać. Nie czyta treści notatki i nie składa strony CV. Nie ma bazy.

Kiedy trasa do Learning zacznie działać?

Gdy AppHost i deploy będą uruchamiać proces Learning. Trasa może być wcześniej. Żądanie dostanie błąd proxy, a `/versions` dalej zwróci pusty `learning`.

## Zasady

- W `appsettings.json` bramy nie ma hasła i nie ma adresu publicznego maszyny.
- `/versions` zawsze ma sześć pól.
- Brama nie dostaje connection stringa do żadnej z dziesięciu baz.
- Jedna brama. Drugi projekt proxy nie powstaje.
- `GET /versions` jest anonimowy.

## Poza zakresem

- Obraz jest w [03-dockerfile.md](03-dockerfile.md).
- Wystawienie na hostname jest w [05-cloudflare.md](05-cloudflare.md).
- Pierwszy rollout jest w [07-deploy.md](07-deploy.md).
- JWT na bramie jest w [03-jwt.md](../05-tozsamosc/03-jwt-i-policies.md).
- Stopka klienta jest w [02-szkielet.md](../11-web/02-szkielet.md).

## Źródła

- [YARP](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/yarp-overview). Wiążące są trasy i klastry w konfiguracji. Dotyczy jednego procesu bramy. Ze źródła nie bierzemy drugiej bramy dla GraphQL albo dla telefonu.
- [RFC 9110, statusy](https://www.rfc-editor.org/rfc/rfc9110). Wiążące jest to, że `200` na `/versions` opisuje raport, nie zdrowie sąsiada. Dotyczy timeoutu pola. Ze źródła nie bierzemy `502` jako odpowiedzi całego raportu.
- [Service discovery w Aspire](https://aspire.dev/fundamentals/service-discovery/). Wiążące jest wołanie usługi po nazwie. Dotyczy adresu klastra na laptopie. Ze źródła nie bierzemy wpisanego `localhost:port` w pliku, który jedzie na maszynę.

## Gotowe gdy

- Test sześciu pól przechodzi przy sąsiadach, którzy nie odpowiadają.
- `aspire run` uruchamia bramę i `GET /versions` zwraca JSON.
- W konfiguracji są trasy z słownika i nie ma hasła.
- AppHost referencjonuje projekt bramy.
- Brama woła `AddServiceDefaults` i `MapDefaultEndpoints`.
