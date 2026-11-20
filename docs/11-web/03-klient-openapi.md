# 03. Klient OpenAPI

Poprzedni numer to [szkielet](02-szkielet.md). Następny to [logowanie](04-logowanie-pkce.md).

Ten numer generuje typy z dokumentu OpenAPI bramy albo usług i woła je przez `openapi-fetch`. Komponent nie składa URL ręcznie i nie deklaruje kształtu odpowiedzi. TanStack Query trzyma odczyt.

## Po co

Ręczny `fetch` i interfejs obok niego rozjadą się z `ProblemDetails` w pierwszym tygodniu. Generator zamyka kształt. Query zamyka ponawianie `GET` tuż po `POST`, gdy projekcja jeszcze nie doszła: ponowienie jest w kliencie, nie w `useEffect` pisanym na każdym ekranie inaczej.

## Dlaczego ta technologia

### `openapi-typescript` i `openapi-fetch`

Oba na MIT. Typy lądują w `src/clients/shared/api-client/generated`. Problem, który zamyka, to DTO przepisane z głowy. Odpada ręczny interfejs odpowiedzi lekcji. Koszt: katalog generated jest w git albo jest wynikiem skryptu w CI. Jest w git, żeby recenzja widziała zmianę kontraktu. Jest na liście wyłączeń `pr-size`, bo generator bywa głośny, ale zmiana i tak jest w diffie do przeczytania. Wyłączenie z limitu linii nie wyłącza obowiązku przeczytania. Skrypt `pnpm openapi` odtwarza plik. Check w CI odpala skrypt i `git diff --exit-code` na tym katalogu. Rozjazd pada.

Źródłem jest `GET /openapi/v1.json` z Development każdej usługi, złożone w jeden dokument albo kilka klientów. Jeden klient na bramę jest prostszy, jeśli brama publikuje dokument. Brama nie składa dziś jednego OpenAPI. Dlatego są moduły `learning`, `news`, `cv`, `contact`, `identity` generowane z dokumentów usług. Web i tak woła je przez jeden bazowy URL bramy, z prefiksem ścieżki. Problem, który zamyka, to wołanie portu usługi z przeglądarki. Odpada `localhost:5xxx` w kliencie. Koszt: pięć generacji. Skrypt je spina.

### TanStack Query

`useQuery` i `useMutation`. Po mutacji, która tworzy zasób, odczyt ponawia się, dopóki nie minie okno projekcji albo nie przyjdzie `200`. Limit ponowień jest 5, odstęp rośnie. Problem, który zamyka, to `404` pokazane użytkownikowi w tej samej sekundzie co sukces `POST`. Odpada natychmiastowy błąd bez ponowienia na ścieżce „właśnie utworzyłem”. Zwykły `GET` istniejącej strony nie ponawia `404`: brak lekcji jest brakiem. Rozróżnienie jest w kluczu mutacji, nie globalnie.

### Błąd

`type` z `ProblemDetails` jest polem błędu w Query. Komponent wybiera klucz chromu albo `title` z odpowiedzi. Nie porównuje polskiego zdania. Problem, który zamyka, to `if (message === "Nie znaleziono")`. Odpada zgadywanie po statusie tam, gdzie `type` jest stabilny. Status i tak jest. `401` czyści sesję w numerze logowania. Tu klient zwraca błąd do Query.

### `Accept-Language`

Nagłówek jest tym samym kodem co `resolveLocale`. Problem, który zamyka, to polski chrom i angielski `title` błędu. Odpada nagłówek na sztywno `pl`.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| generated | typy z OpenAPI, niepisane ręcznie |
| `useQuery` | odczyt z cache i ponowieniem |
| `type` | kod błędu, nie zdanie |
| bazowy URL | adres bramy, jeden |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `src/clients/shared/api-client/generated/` | typy |
| `src/clients/shared/api-client/client.ts` | `openapi-fetch` z bazowym URL |
| `src/clients/shared/api-client/query.ts` | klient Query |
| `src/clients/web/package.json` | skrypt `openapi` |
| `tests` w `api-client` | nagłówek języka i brak portu usługi w URL |

## Plan

### 1. Generacja

Skrypt czyta pliki OpenAPI z `deploy/openapi/*.json` commitowane przy zmianie API, albo ściąga je w Development. W CI bierze pliki z repozytorium, nie żywą maszynę. Zmiana endpointu w usłudze aktualizuje JSON w tym samym pull requeście. Rozjazd łapie `git diff --exit-code`.

### 2. Klient

Funkcja `createClient(baseUrl, locale)` ustawia `Accept-Language`. `baseUrl` to `import.meta.env.VITE_API_BASE`. W Development `http://127.0.0.1:8080`. W obrazie hostname produktu. W kodzie nie ma `localhost:8080` wpisanego poza przykładem env.

### 3. Test

Fakt: URL żądania zaczyna się od bazowego i zawiera `/learning/`, nie zawiera `:5432` ani portu usługi spoza bramy. Fakt: przy `locale=en` nagłówek to `en`.

### Co jest do czego

| Warstwa | Czego nie robi |
| --- | --- |
| generated | nie woła sieci |
| `client.ts` | nie zna komponentu React |
| Query | nie składa SQL |

## Testy

Nazwa: `client_sends_accept_language_from_locale`.

| Arrange | Act | Assert |
| --- | --- | --- |
| klient z `en` i podstawionym `fetch` | wywołanie ścieżki lekcji | nagłówek `en`, URL na bazie bramy |

```bash
cd src/clients && pnpm exec vitest run shared/api-client
```

## Checkpoint

```bash
cd src/clients && pnpm openapi && git diff --exit-code -- shared/api-client/generated
pnpm exec vitest run shared/api-client
```

Gdy dokumenty OpenAPI są puste na tym etapie, skrypt generuje klienta z minimalnego dokumentu health, commitowanego w `deploy/openapi/gateway.json`, żeby katalog generated nie był pusty. Prawdziwe ścieżki kursu dochodzą, gdy dokument Learning jest w repozytorium. Ten numer wymaga co najmniej dokumentu z `/versions` i z jedną ścieżką Learning, wyeksportowanego z Development do `deploy/openapi`.

## Umiesz wyjaśnić

Dlaczego typy są w git?

Bo inaczej recenzja nie widzi, że zniknęło pole, a CI musiałby mieć żywe API. Plik w git jest kontraktem. Skrypt sprawdza, że nie odjechał od dokumentu.

Dlaczego przeglądarka nie woła portu Learning?

Bo z internetu widać tylko bramę. Port usługi nie jest adresem klienta. Prefiks `/learning` na bramie wystarcza.

Czemu `404` tuż po `POST` jest ponawiany, a `404` na starym linku nie?

Bo pierwsze jest oknem projekcji po mutacji, którą ten klient właśnie zrobił. Drugie jest prawdą, że sluga nie ma. Globalne ponawianie każdego `404` zamieniłoby brak strony w długie czekanie.

Dlaczego porównujemy `type`, nie `title`?

Bo `title` zmienia się z językiem. `type` jest kodem. Warunek na zdanie psuje się po tłumaczeniu.

Co jest sekretem w `VITE_`?

Nic. Adres bramy jest publiczny. Token, jak dojdzie, będzie w pamięci, nie w zmiennej wkompilowanej w bundle.

## Zasady

- Komponent ekranu nie woła `fetch` bezpośrednio.
- Bazowy URL jest jeden, brama.
- Generated nie jest poprawiany ręcznie.
- `Accept-Language` równa się lokalizacji chromu.
- Ponowienie `404` jest tylko po własnej mutacji tworzenia.
- W kliencie nie ma connection stringa.

## Poza zakresem

- PKCE i `Authorization` są w [04-logowanie-pkce.md](04-logowanie-pkce.md).
- Ekrany treści są w [05-tresc-publiczna.md](05-tresc-publiczna.md).
- GraphQL nie ma tu klienta.

## Źródła

- [openapi-fetch](https://openapi-ts.dev/openapi-fetch/). Wiążące są typy ścieżki. Dotyczy zakazu ręcznego DTO. Ze źródła nie bierzemy wołania poza wygenerowaną ścieżką „bo szybciej”.
- [TanStack Query](https://tanstack.com/query/latest). Wiążące są `useQuery` i `useMutation`. Dotyczy odczytu i ponowienia po utworzeniu. Ze źródła nie bierzemy ponawiania każdego błędu 4xx.
- [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457). Wiążący jest `type`. Dotyczy rozgałęzienia w kliencie. Ze źródła nie bierzemy porównania `title`.

## Gotowe gdy

- `git diff` po `pnpm openapi` jest pusty.
- Test nagłówka `en` przechodzi.
- W `src/clients/web/src` nie ma `fetch(`.
- URL w teście wskazuje bramę.
- Jest co najmniej jeden dokument OpenAPI w `deploy/openapi`.
