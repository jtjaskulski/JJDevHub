# Learning

Usługa materiałów dydaktycznych w `src/services/Learning/`, w warstwach `Learning.Domain`, `Learning.Application`, `Learning.Infrastructure` i `Learning.API`. Ma cztery agregaty: `Technology`, `Track` z lekcjami (kurs od A do Z pod jedną technologią), `Lecture` (sylabus wykładu) i `Term` (hasło kompendium). Każdy ma widoczność `Private`, `Students` albo `Public`. Markdown renderuje się przy zapisie, a lekcje można zaimportować z repozytorium na GitHubie. Każda zmiana wysyła pełny stan agregatu na `learning.integration` przez outbox z [12-outbox-inbox.md](12-outbox-inbox.md). Role i polityki przychodzą z [15-identity.md](15-identity.md), CI usługi z [18-ci-uslug.md](18-ci-uslug.md). Wyszukiwanie po tej treści jest w [20-wyszukiwanie.md](20-wyszukiwanie.md).

## Po co

Dziś Wykłady i Kompendium to pliki [pl.json](../../src/Clients/web/src/content/pl.json) i [en.json](../../src/Clients/web/src/content/en.json) w bundlu klienta web. Poprawka literówki w haśle to commit, CI i deploy frontu. Lekcje kursu mobilnego to linki do plików Markdown na GitHubie, więc student wychodzi ze strony po każdą treść.

Strona ma uczyć w dwóch trybach. Kompendium to technologia, a pod nią krótki kurs od pierwszej lekcji do ostatniej i słownik haseł. Służy studentom i autorowi jako osobista baza wiedzy, więc część treści jest prywatna. Wykłady to sylabusy uczelniane: opis, lekcje, rozdziały i plan na kolejne zajęcia. Oba tryby dzielą technologie, Markdown, widoczność i import, więc żyją w jednej usłudze.

Learning jest treścią właściciela obok notatek. News trzyma artykuł w strumieniu, Users to jedna warstwa CRUD. Learning ma bogatszą domenę (kolejność lekcji, unikalne slugi, widoczność efektywna, lekcje tylko do odczytu po imporcie) i tę samą historię: każda zmiana technologii, kursu, wykładu i hasła zostaje faktem. Postęp studenta zostaje tabelą.

## Dlaczego ta technologia

### Event sourcing treści właściciela

Technologia, kurs z lekcjami, wykład i hasło są agregatami na `AggregateRoot` z [03-agregat.md](03-agregat.md). Każda metoda dopisuje zdarzenie, a `Apply` jest jedynym miejscem zmiany pól. Zapis idzie przez `IRepository<T>` z [07-repozytorium.md](07-repozytorium.md) do `learning_write`. Odczyt strony i intranetu idzie z projekcji w `learning_read`, jak artykuł w [10-projekcja.md](10-projekcja.md). `ETag` i pole `Version` na szynie to wersja strumienia.

Lekcja zaimportowana z GitHuba ma historię pliku w gicie. Strumień kursu i tak dostaje fakt importu: co weszło do huba, z jakim `sha` i jaką treścią. Drugi import z tym samym `sha` nie dopisuje zdarzenia. Lekcja pisana w intranecie nie ma gita, więc strumień jest jej jedyną historią.

Postęp studenta nie jest tym agregatem. Tabela `LessonProgress` w bazie zapisu trzyma „kto odhaczył lekcję”. Nie da się jej złożyć ze strumienia kursu i nie wychodzi na szynę.

Agregaty wskazują się id, nie nawigacją. Metody zwracają `Result<T>`, reguły są klasami `IBusinessRule`. Warstwy są cztery, bo komend jest kilkanaście, a import z GitHuba i potok Markdown to infrastruktura, której domena nie powinna znać.

### Jedna usługa na kompendium i wykłady

Osobne usługi Compendium i Lectures rozcięłyby jedną rzecz. Wykład wskazuje technologie, hasło wskazuje technologie, lekcja linkuje do haseł. Przy dwóch usługach strona wykładu potrzebowałaby dwóch wywołań, a link `[[cqrs]]` w lekcji wymagałby kopii słownika w drugiej usłudze. Granica kontekstu to „materiały dydaktyczne”: jeden autor, jeden potok Markdown, jedna widoczność. Notatki zostają w News, bo mają inny cykl życia: krótki wpis z historią, publikowany i wycofywany.

### Widoczność jako pole agregatu

`Visibility` ma trzy wartości. `Private` to baza wiedzy ownera, `Students` widzą zalogowani z rolą `student`, `Public` widzą wszyscy. Lekcja ma własną widoczność, a widoczność efektywna to surowsza z dwóch: lekcja `Private` w kursie `Public` jest szkicem, którego nikt poza ownerem nie zobaczy.

Filtr działa w zapytaniu SQL, a nie po odczycie. Treść niewidoczna dla wywołującego kończy się `404`, nie `403`, żeby odpowiedź nie zdradzała, że prywatne hasło istnieje.

Alternatywy:

- **Obieg Draft → Review → Published** — sens przy wielu autorach i redaktorze. Jeden autor nie recenzuje sam siebie.
- **ACL na element** (lista osób z dostępem) — odpowiada na zapisy na konkretny kurs, które są poza zakresem [15-identity.md](15-identity.md).
- **Osobne tabele szkiców** — dwa modele tej samej lekcji i kopiowanie przy publikacji.

### Markdown renderowany przy zapisie: Markdig i HtmlSanitizer

Treść pisze się w Markdown, bo tak są napisane kursy na GitHubie, a autor nie chce drugiego formatu. Pytanie brzmi, kto zamienia Markdown na HTML i kiedy.

**Markdig** (BSD-2-Clause) to parser CommonMark dla .NET z rozszerzeniami GitHuba: tabele, listy zadań, przypisy, identyfikatory nagłówków jak na GitHubie. Daje drzewo składni, więc własny link `[[slug]]` to jedna klasa parsera, a nie wyrażenie regularne na HTML-u. **HtmlSanitizer** (MIT) przepuszcza wynik przez listę dozwolonych tagów, atrybutów i schematów URL. Markdown z GitHuba pochodzi od kontrybutorów repozytorium kursu, więc nie jest zaufany, nawet gdy owner jest jedynym autorem w intranecie. `[x](javascript:alert(1))` to poprawny Markdown, a Markdig renderuje adres tak, jak przyszedł.

Potok jest złożony ręcznie, a nie przez `UseAdvancedExtensions()`. Pakiet zaawansowany włącza `GenericAttributes`, czyli składnię `{onclick=...}`, która dopisuje dowolny atrybut do elementu. Surowy HTML w Markdown jest wyłączony (`DisableHtml()`), a sanitizer to druga warstwa na to, czego wyłączenie nie obejmuje.

Render przy zapisie, bo odczytów jest tysiąc razy więcej niż zapisów. Wiersz trzyma źródło, HTML i czysty tekst. Czysty tekst zasila wyszukiwanie z [20-wyszukiwanie.md](20-wyszukiwanie.md). Alternatywy:

- **Render przy odczycie** — CPU na każde żądanie i cache do unieważniania.
- **Render w kliencie** (`react-markdown`, `marked`) — większy bundle, inny parser niż przy imporcie i brak czystego tekstu po stronie serwera. Podgląd w intranecie różniłby się od publikacji w szczegółach (tabele, `[[slug]]`).

Podgląd (`POST /api/learning/preview`) woła ten sam `IMarkdownRenderer` co zapis. To, co owner widzi w edytorze, jest bajt w bajt tym, co zobaczy student. `MarkdownRenderer.Version` rośnie przy każdej zmianie potoku. Worker przy starcie renderuje ponownie wiersze ze starszą wersją, więc nowe rozszerzenie nie wymaga ręcznej migracji treści.

### Linki `[[slug]]` i odnośniki zwrotne

Hasło kompendium ma sens w sieci: CQRS linkuje do Event Sourcingu, lekcja o Commands linkuje do hasła CQRS. Własny parser inline czyta `[[slug]]` i `[[slug|tekst]]`. Render zbiera wszystkie slugi do tabeli `TermLinks` (źródło, slug hasła). Strona hasła pokazuje „Gdzie występuje” z jednego zapytania po `TermSlug`.

Link wskazuje slug, a nie id. Owner może napisać `[[outbox]]`, zanim hasło powstanie. Odczyt lekcji i hasła zwraca listę slugów, które nie istnieją albo są niewidoczne dla wywołującego, a klient rysuje takie linki jak czerwony link w Wikipedii. Stan „brakuje” liczy się przy odczycie, bo zapisany w HTML-u przestałby być prawdą w chwili utworzenia hasła.

### Import z GitHuba przez REST contents API

Kurs mobilny leży w repozytorium jako pliki `00-Wstep-React-Native.md`, `01-…`. Import zamienia katalog na lekcje kursu. Opcje:

- **REST contents API** — `GET /repos/{owner}/{repo}/contents/{path}?ref={ref}` zwraca listę plików z `sha` blobu, a ten sam adres z `Accept: application/vnd.github.raw+json` zwraca treść pliku. Publiczne repo nie wymaga tokenu. Limit bez tokenu: 60 żądań na godzinę z jednego IP.
- **`git clone`** (LibGit2Sharp, MIT) — natywna biblioteka w obrazie, cała historia na dysku, żeby odczytać dwanaście plików.
- **`raw.githubusercontent.com`** — bez listy plików i bez `sha`, więc każdy import pobiera wszystko.
- **Webhook `push`** — import sam z siebie po każdym commicie, ale wymaga publicznego endpointu i sekretu webhooka. To naturalny następny krok, poza tym numerem.

Contents API wygrywa, bo `sha` blobu mówi, które pliki się zmieniły. Pierwszy import kursu z dwunastoma lekcjami to trzynaście żądań. Kolejny, bez zmian, to jedno żądanie listy. Limit 60 na godzinę wystarcza z zapasem.

Import jest idempotentny. Lekcja pamięta źródło (repo, ref, ścieżka, `sha`) osobno dla każdego języka. Ten sam plik z tym samym `sha` nie jest pobierany. Plik usunięty z repo nie usuwa lekcji, tylko trafia do raportu jako osierocony, bo decyzja o usunięciu treści należy do ownera. Lekcja ze źródłem jest w intranecie tylko do odczytu (poza kolejnością i widocznością), żeby następny import nie nadpisał ręcznej poprawki. Edycja wymaga świadomego odłączenia źródła.

### Ochrona przed SSRF w imporcie

Serwer, który pobiera coś według danych od użytkownika, to podręcznikowy server-side request forgery. Endpoint jest tylko dla ownera, ale token ukradziony przez XSS dałby napastnikowi skaner sieci wewnętrznej: `http://postgres:5432`, `http://kafka:9092`, metadane hypervisora. Obrona nie polega na filtrowaniu adresów, tylko na tym, że adresu w żądaniu nie ma:

- żądanie niesie `repo` w postaci `owner/name`, sprawdzane wyrażeniem regularnym, a nie URL,
- typowany `HttpClient` ma stały `BaseAddress` `https://api.github.com/`,
- `AllowAutoRedirect = false`, więc odpowiedź `301` nie przeniesie żądania gdzie indziej,
- klient nigdy nie podąża za `download_url` z odpowiedzi, treść pobiera tym samym adresem API z innym nagłówkiem `Accept`,
- `path` i `ref` nie mogą zawierać `..`, `//` ani zaczynać się od `/`.

Lista dozwolonych hostów ma więc jeden wpis i jest w kodzie, nie w konfiguracji. Alternatywa z filtrem adresów prywatnych w `ConnectCallback` jest potrzebna, gdy host przychodzi od użytkownika. Tu nie przychodzi.

### Event-carried state transfer na `learning.integration`

CV z [22-cv.md](22-cv.md) pokaże przy umiejętności „kurs React Native: 12 lekcji, wykład na uczelni”. Musi wiedzieć, jakie kursy, lekcje i wykłady są publiczne. Reguła z [README.md](README.md) zabrania czytania cudzej bazy. Opcje:

- **Event-carried state transfer** — zdarzenie `track-changed.v1` niesie pełny bieżący stan agregatu: slug, tytuł, widoczność, listę lekcji z ich widocznością, `Version`. Konsument robi upsert i ma kopię potrzebnych danych.
- **Zdarzenie-powiadomienie i wywołanie zwrotne** — `track-changed` z samym id, konsument woła `GET /api/learning/tracks/{id}`. Konsument zależy wtedy od dostępności Learning w chwili odbioru.
- **Zdarzenia drobne** (`lesson-added`, `lesson-renamed`, `lesson-reordered`) — konsument składa stan z kilkunastu kontraktów i musi znać reguły domeny, np. widoczność efektywną.
- **Change data capture** (Debezium) — Kafka Connect na VM i schemat tabel jako kontrakt publiczny.

Stan pełny jest większy (kurs z dwunastoma lekcjami to kilka kilobajtów), ale konsument jest najprostszy i odporny na kolejność: wiadomość z `Version` mniejszym niż zapisane jest pomijana. Usunięcie to ostatni stan z `IsDeleted = true`. `Version` na szynie jest wersją strumienia.

Zdarzenia płyną dla każdej widoczności, także `Private`. Gdyby treść prywatna nie była publikowana, przejście `Public` → `Private` nie dotarłoby do konsumenta, a CV dalej pokazywałoby ukryty kurs. Filtr widoczności robi konsument. CV bierze tylko `Public`.

Temat `learning.integration` ma `cleanup.policy=compact`. Kafka zostawia wtedy ostatnią wiadomość dla każdego klucza, a kluczem jest id agregatu. Usługa Cv, która powstanie trzy numery później, czyta temat od początku i dostaje pełny bieżący stan bez osobnego endpointu do zasilenia. Zasada z [13-kafka.md](13-kafka.md) się nie zmienia: Kafka nie jest źródłem prawdy. `POST /api/learning/republish` zapisuje do outboxa bieżący stan wszystkich agregatów, gdy kontrakt dostaje `.v2` albo konsument musi zacząć od zera.

### Postęp studenta w Learning

Student zaznacza lekcję jako przerobioną. Postęp wskazuje id lekcji i zależy od jej widoczności, więc należy do Learning, nie do Users. Tabela `LessonProgress` nie jest agregatem ani nie trafia na szynę: to stan użytkownika, nie treść. Owner widzi liczby (ilu studentów przerobiło lekcję 3), bez nazwisk. Do decyzji, która lekcja jest za trudna, liczby wystarczają, a lista nazwisk byłaby danymi osobowymi bez celu.

## Klasy i ścieżki

| Plik | Odpowiedzialność |
| --- | --- |
| `src/services/Learning/Learning.Domain/Shared/Slug.cs` | `a-z`, `0-9`, `-`, do 80 znaków; `Slug.From(title)` usuwa polskie znaki |
| `src/services/Learning/Learning.Domain/Shared/LocalizedText.cs` | `Pl` wymagane, `En` opcjonalne, `For(Language)` z powrotem do `Pl` |
| `src/services/Learning/Learning.Domain/Shared/RenderedMarkdown.cs` | `Source`, `Html`, `PlainText`, `RendererVersion` |
| `src/services/Learning/Learning.Domain/Shared/LocalizedMarkdown.cs` | `RenderedMarkdown` dla `pl` i opcjonalnie `en` |
| `src/services/Learning/Learning.Domain/Shared/ExternalUrl.cs` | tylko `https` |
| `src/services/Learning/Learning.Domain/Shared/Visibility.cs`, `Audience.cs`, `VisibilityExtensions.cs` | `Private`, `Students`, `Public`; `IsVisibleTo`, `Stricter` |
| `src/services/Learning/Learning.Domain/Technologies/Technology.cs` | slug, nazwa, opis, widoczność; wersja ze strumienia |
| `src/services/Learning/Learning.Domain/Tracks/Track.cs` | kurs pod technologią, lekcje, import, kolejność |
| `src/services/Learning/Learning.Domain/Tracks/Lesson.cs` | encja w `Track`: slug, pozycja, tytuł, treść, minuty, widoczność, źródła |
| `src/services/Learning/Learning.Domain/Tracks/LessonSource.cs` | `Language`, `Repo`, `Ref`, `Path`, `BlobSha` |
| `src/services/Learning/Learning.Domain/Tracks/*Rule.cs` | unikalny slug lekcji, pełna permutacja kolejności, lekcja ze źródłem tylko do odczytu |
| `src/services/Learning/Learning.Domain/Lectures/Lecture.cs` | sylabus: opis, repozytorium, laboratorium, linki lekcji, rozdziały, plan, technologie |
| `src/services/Learning/Learning.Domain/Terms/Term.cs` | hasło: tytuł, definicja, technologie |
| `src/services/Learning/Learning.Application/Abstractions/` | `ILearningReader`, `IMarkdownRenderer`, `IGitHubContentSource`, rejestry sluga i użyć technologii |
| `src/services/Learning/Learning.Application/Technologies/`, `Tracks/`, `Lectures/`, `Terms/` | komendy, walidatory, handlery, zapytania |
| `src/services/Learning/Learning.Application/Import/` | `ImportTrackFromGitHubCommand`, `ImportLegacyContentCommand`, raporty |
| `src/services/Learning/Learning.Application/Progress/` | `MarkLessonProgressCommand`, `GetTrackProgressQuery` |
| `src/services/Learning/Learning.Application/StateTransfer/StateEvents.cs` | agregat → zdarzenie `*-changed.v1` |
| `src/services/Learning/Learning.Application/StateTransfer/IContentChanges.cs`, `ContentChanges.cs` | po `SaveAsync` samo zdarzenie do outboxa |
| `src/services/Learning/Learning.Infrastructure/Data/LearningWriteContext.cs` | baza `learning_write`: strumienie, rezerwacje, postęp, outbox |
| `src/services/Learning/Learning.Infrastructure/Data/LearningReadContext.cs` | baza `learning_read`: projekcje treści, `TermLinks`, checkpoint |
| `src/services/Learning/Learning.Infrastructure/Data/ContentSlugRow.cs`, `TechnologyUseRow.cs` | unikalny slug i „technologia jest użyta” |
| `src/services/Learning/Learning.Infrastructure/Data/LessonProgress.cs` | tabela postępu, nie strumień |
| `src/services/Learning/Learning.Infrastructure/Projections/ContentProjection.cs` | HTML, widoki i `TermLinks` ze strumienia |
| `src/services/Learning/Learning.Infrastructure/Markdown/MarkdownRenderer.cs` | potok Markdig, sanitizer, czysty tekst |
| `src/services/Learning/Learning.Infrastructure/Markdown/WikiLinkParser.cs`, `WikiLinkRenderer.cs`, `WikiLinkExtension.cs` | `[[slug]]` |
| `src/services/Learning/Learning.Infrastructure/Data/TermLink.cs` | odnośnik zwrotny, wiersz projekcji |
| `src/services/Learning/Learning.Infrastructure/GitHub/GitHubContentClient.cs` | typowany klient contents API |
| `src/services/Learning/Learning.Infrastructure/GitHub/GitHubUnavailableException.cs` | limit albo awaria GitHuba, z `RetryAfter` |
| `src/services/Learning/Learning.Contracts/` | `TechnologyChanged…`, `TrackChanged…`, `LectureChanged…`, `TermChangedIntegrationEvent` |
| `src/services/Learning/Learning.API/Apis/` | `TechnologyEndpoints`, `TrackEndpoints`, `LectureEndpoints`, `TermEndpoints`, `AuthoringEndpoints`, `ProgressEndpoints` |
| `src/services/Learning/Learning.API/Http/CurrentAudience.cs` | `Audience` z polityk `Owner` i `Student` |
| `src/services/Learning/Learning.API/Http/GitHubUnavailableHandler.cs` | `IExceptionHandler`: `503` z `Retry-After` |
| `tests/Learning.Domain.Tests/`, `tests/Learning.Infrastructure.Tests/`, `tests/Learning.API.Tests/` | domena, Markdown i GitHub, endpointy |

## Plan

### 1. Projekty

```bash
for layer in Domain Application Infrastructure Contracts; do
  dotnet new classlib -n Learning.$layer -o src/services/Learning/Learning.$layer -f net11.0
done
dotnet new web -n Learning.API -o src/services/Learning/Learning.API -f net11.0
for kind in Domain Infrastructure API; do
  dotnet new xunit -n Learning.$kind.Tests -o tests/Learning.$kind.Tests -f net11.0
done
dotnet sln add src/services/Learning/* tests/Learning.*.Tests
dotnet add src/services/Learning/Learning.Domain reference src/building-blocks/JJDevHub.Domain
dotnet add src/services/Learning/Learning.Contracts reference src/building-blocks/JJDevHub.EventBus
dotnet add src/services/Learning/Learning.Application reference src/services/Learning/Learning.Domain src/services/Learning/Learning.Contracts src/building-blocks/JJDevHub.Cqrs
dotnet add src/services/Learning/Learning.Infrastructure reference src/services/Learning/Learning.Application src/building-blocks/JJDevHub.EventStore src/building-blocks/JJDevHub.EventStore.Postgres src/building-blocks/JJDevHub.Projections src/building-blocks/JJDevHub.IntegrationEventLog
dotnet add src/services/Learning/Learning.Infrastructure package Markdig
dotnet add src/services/Learning/Learning.Infrastructure package HtmlSanitizer
dotnet add src/services/Learning/Learning.API reference src/services/Learning/Learning.Infrastructure src/aspire/JJDevHub.ServiceDefaults src/building-blocks/JJDevHub.EventBus.Kafka
```

`Learning.Application` nie zna EF, Markdiga ani `HttpClient`. Zna porty z `Abstractions/`.

### 2. Value objecty i widoczność

`Slug.Create(string?)` przyjmuje `^[a-z0-9]+(-[a-z0-9]+)*$` do 80 znaków. `Slug.From(string title)` zamienia na małe litery, `ą`→`a`, `ł`→`l` i resztę polskich znaków przez `NormalizationForm.FormD` z usunięciem znaków diakrytycznych, a ciągi innych znaków na jeden `-`. Slug nie zmienia się po utworzeniu. Wszystkie zdarzenia i linki mogą więc na nim polegać.

`Language` to enum `Pl`, `En`. `LocalizedText.Create(string? pl, string? en)`: `Pl` po przycięciu niepuste, `En` puste zamienia się na `null`. `For(Language)` zwraca `En ?? Pl` i język, który naprawdę wrócił. Klient pokazuje plakietkę „tylko po polsku”, gdy język się różni.

```csharp
namespace JJDevHub.Learning.Domain.Shared
{
    public enum Visibility
    {
        Private,
        Students,
        Public,
    }
}
```

`Audience` to `Anonymous`, `Student`, `Owner`. `VisibilityExtensions`:

- `IsVisibleTo(this Visibility visibility, Audience audience)` — `Public` dla wszystkich, `Students` dla `Student` i `Owner`, `Private` tylko dla `Owner`,
- `VisibleLevels(this Audience audience)` — lista poziomów do `WHERE "Visibility" = ANY(...)` w zapytaniu,
- `Stricter(this Visibility a, Visibility b)` — widoczność efektywna lekcji.

`ExternalUrl.Create(string?)` wymaga absolutnego URI ze schematem `https`. Link `javascript:` albo `http:` w sylabusie odpada przy zapisie, nie przy renderze.

### 3. Agregaty

Każdy agregat ma `Slug`, `Visibility` i `UpdatedOn`. `Version` pochodzi z `AggregateRoot`: to numer ostatniego faktu w strumieniu, `ETag` w HTTP i pole `Version` na szynie. Zdarzenie niesie źródło Markdown, nie HTML. Nazwa faktu to `learning.{czynność}.v1`, na przykład `learning.lesson-added.v1`.

`Technology`: `Name` (nazwa własna, bez tłumaczenia, np. „React Native”), `Summary : LocalizedText`. Metody `Create`, `Revise`, `ChangeVisibility`.

`Track`: `TechnologyId`, `Title`, `Summary : LocalizedText`, `IReadOnlyList<Lesson> Lessons` posortowane po `Position`.

| Metoda | Reguła |
| --- | --- |
| `AddLesson(Slug, LocalizedText title, LocalizedMarkdown body, int? minutes, Visibility)` | `LessonSlugMustBeUniqueInTrackRule`; lekcja dostaje ostatnią pozycję |
| `ReviseLesson(Guid lessonId, …)` | `ImportedLessonIsReadOnlyRule`: lekcja z dowolnym źródłem → `Invalid` |
| `ChangeLessonVisibility(Guid lessonId, Visibility)` | działa także dla lekcji ze źródłem |
| `ReorderLessons(IReadOnlyList<Guid> order)` | `ReorderMustListEveryLessonRule`: lista to permutacja wszystkich id |
| `RemoveLesson(Guid lessonId)` | pozycje za nią przesuwają się o jeden |
| `DetachSource(Guid lessonId)` | usuwa źródła, lekcja staje się edytowalna |
| `ImportLesson(LessonSource, Slug, LocalizedText title, string body, int? minutes)` | zwraca `Added`, `Updated`, `Unchanged` albo `Skipped` |

`ImportLesson` szuka lekcji po slugu. Brak → nowa lekcja `Private` na końcu, żeby świeży import nie pojawił się studentom, zanim owner go przejrzy. Jest i źródło dla tego języka ma ten sam `BlobSha` → `Unchanged` i zero nowych zdarzeń. Inny `sha` → zdarzenie z nowym źródłem Markdown dla tego języka. Lekcja bez źródła w tym języku, ale z ręczną treścią → `Skipped`: import nie nadpisuje treści pisanej w intranecie, a reszta plików importuje się dalej.

`Lecture` odwzorowuje dzisiejszy `CourseContent` z [content.model.ts](../../src/Clients/web/src/app/content/content.model.ts): `Title`, `Subtitle : LocalizedText`, `Summary : LocalizedMarkdown`, `Repository` i `Lab` jako `ExternalLink(ExternalUrl Url, LocalizedText Label)`, `Links` (dzisiejsze `lessons`: slug, tytuł, opis, URL), `Chapters`, `Planned` (slug, tytuł, opis), `Institution` i `Period` (np. „2025/2026, semestr letni”), `TechnologyIds`. Nagłówki sekcji (`lessonsTitle`, `plannedLead`) to tekst interfejsu i zostają w bundlu.

`Term`: `Title : LocalizedText`, `Definition : LocalizedMarkdown`, `TechnologyIds` (zero albo więcej: CQRS dotyczy i .NET, i React Native).

Agregaty wskazują się nawzajem po id (`TechnologyId`). Usunięcie technologii, która ma kurs, wykład albo hasło, kończy się `Invalid`: handler sprawdza tabelę `TechnologyUses` w bazie zapisu, uzupełnianą w tej samej transakcji co dopisanie strumienia.

### 4. Potok Markdown

Port w Application:

```csharp
namespace JJDevHub.Learning.Application.Abstractions
{
    public interface IMarkdownRenderer
    {
        MarkdownRenderResult Render(string source, MarkdownContext context);
    }
}
```

`MarkdownRenderResult(RenderedMarkdown Markdown, IReadOnlyList<string> TermSlugs)`. `MarkdownContext` niesie bazę dla linków względnych (repo, ref, katalog) przy imporcie i `null` przy pisaniu w intranecie.

`MarkdownRenderer` w Infrastructure, singleton:

```csharp
_pipeline = new MarkdownPipelineBuilder()
    .UsePipeTables()
    .UseTaskLists()
    .UseFootnotes()
    .UseEmphasisExtras()
    .UseAutoLinks()
    .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
    .UseYamlFrontMatter()
    .Use<WikiLinkExtension>()
    .DisableHtml()
    .Build();
```

Kroki `Render`:

- `Markdown.Parse(source, _pipeline)` daje `MarkdownDocument`,
- przejście po `LinkInline`: przy imporcie obraz względny dostaje adres `raw.githubusercontent.com/{repo}/{ref}/{katalog}/{plik}`, a link względny do `.md` adres pliku na `github.com`; link do pliku z tego samego importu dostaje też `data-lesson="{slug}"`, który klient zamienia na trasę w hubie,
- `document.ToHtml(_pipeline)`, potem `HtmlSanitizer` z tagami treści (`p`, `h1`–`h6`, `ul`, `ol`, `li`, `table` z dziećmi, `pre`, `code`, `blockquote`, `a`, `img`, `em`, `strong`, `del`, `sup`, `input` dla list zadań), atrybutami `href`, `src`, `alt`, `title`, `id`, `class`, `data-lesson`, `data-term`, `checked`, `disabled` i schematami `https`, `http`, `mailto`,
- `Markdown.ToPlainText(source, _pipeline)` dla wyszukiwania,
- `TermSlugs` z węzłów `WikiLinkInline`.

`WikiLinkParser : InlineParser` otwiera się na `[`, czyta `[[slug]]` albo `[[slug|tekst]]` i odrzuca slug niezgodny ze `Slug.Create`. `WikiLinkRenderer` pisze `<a class="wikilink" data-term="{slug}">tekst</a>`. Adres strony hasła składa klient, bo zna trasy językowe. `GetLessonQuery`, `GetTermQuery` i `PreviewMarkdownQuery` zwracają `MissingTermSlugs`: slugi z `TermLinks` źródła, których nie ma wśród haseł widocznych dla wywołującego.

Kolorowanie kodu zostaje po stronie klienta. Markdig pisze `class="language-csharp"` na `code`, a sanitizer przepuszcza klasy z prefiksem `language-`.

Front matter w imporcie: blok `---` na początku pliku z liniami `klucz: wartość`. Czytane są `title`, `summary` i `minutes`. Bez YamlDotNet, bo trzy klucze to podział linii po pierwszym `:`.

`MarkdownRenderer.Version = 1`. HTML powstaje w `ContentProjection`, nie w agregacie. Projekcja trzyma przy checkpointcie wersję renderera. Gdy `MarkdownRenderer.Version` jest wyższa, worker czyta strumień od pozycji 0 i buduje widok od nowa. Nowy HTML nie dopisuje faktu domenowego i nie wysyła zdarzenia na szynę.

### 5. Baza

Dwa konteksty. `LearningWriteContext` woła `ApplyEventStore()` i `ApplyIntegrationEventLog()`. Obok strumieni:

- `ContentSlugs`: klucz (`Kind`, `Slug`) → id agregatu. Jeden strumień nie widzi sluga sąsiada. Naruszenie klucza w transakcji dopisania kończy się `Conflict`, zanim strumień powstanie. Slug lekcji jest unikalny wewnątrz kursu i pilnuje go reguła agregatu, nie ta tabela,
- `TechnologyUses`: klucz (`TechnologyId`, `SourceKind`, `SourceId`). Handler kursu, wykładu i hasła wstawia albo usuwa wiersze w tej samej transakcji co strumień,
- `LessonProgress`: klucz (`IdentityGuid`, `LessonId`), `CompletedOn`.

`LearningReadContext` trzyma projekcję: wiersz technologii, kursu, lekcji, wykładu i hasła ze źródłem Markdown, HTML-em, czystym tekstem, widocznością i `Version`. Lekcja jest osobnym wierszem z `TrackId`, bo postęp i wyszukiwanie adresują ją id. `TermLinks`: (`SourceKind`, `SourceId`, `TermSlug`), indeks na `TermSlug`. `Checkpoints` jak w [10-projekcja.md](10-projekcja.md).

Kształt kolumn widoku:

- `LocalizedText` jako `ComplexProperty` (kolumny `Title_Pl`, `Title_En`),
- treść jako jsonb ze źródłem, HTML-em i czystym tekstem dla obu języków,
- `Lecture.Links`, `Chapters`, `Planned` i `Lesson.Sources` jako kolekcje jsonb,
- `TechnologyIds` jako `uuid[]` z indeksem GIN; filtr `?technology=` to `= ANY`,
- `Visibility` jako tekst (`HasConversion<string>()`).

Migracje `LearningWriteInitial` i `LearningReadInitial`. Bazy `learning_write` i `learning_read`, zasoby Aspire `learning-write` i `learning-read`.

### 6. Komendy i zapytania

Mediator z [08-mediator.md](08-mediator.md), `AddCqrs(typeof(CreateTrackCommand).Assembly)`. `LearningUnitOfWork : IUnitOfWork` na `LearningWriteContext`, jak `NewsUnitOfWork` z [09-komenda.md](09-komenda.md).

Cztery agregaty idą przez `IRepository<T>`. Komendy zapisu przyjmują `ExpectedVersion` z `If-Match`. `SaveAsync` zwraca `Conflict`, gdy strumień ma już inną wersję. To ten sam układ co `ExpectedVersion` w [09-komenda.md](09-komenda.md).

| Folder | Komendy |
| --- | --- |
| `Technologies/` | `CreateTechnologyCommand`, `ReviseTechnologyCommand`, `DeleteTechnologyCommand` |
| `Tracks/` | `CreateTrackCommand`, `ReviseTrackCommand`, `DeleteTrackCommand`, `AddLessonCommand`, `ReviseLessonCommand`, `ReorderLessonsCommand`, `RemoveLessonCommand`, `DetachLessonSourceCommand` |
| `Lectures/` | `CreateLectureCommand`, `ReviseLectureCommand`, `DeleteLectureCommand` |
| `Terms/` | `CreateTermCommand`, `ReviseTermCommand`, `DeleteTermCommand` |
| `Visibility/` | `ChangeVisibilityCommand(ContentKind Kind, Guid Id, int ExpectedVersion, Visibility Visibility)`, także dla lekcji |

Handler przekazuje do agregatu źródło Markdown. HTML i `TermLinks` powstają w projekcji. Odpowiedź zapisu to `ContentWritten(Guid Id, string Slug, int Version)`. `GET` tuż po `POST` może przez chwilę oddać `404`, jak przy artykule: klient ponawia odczyt.

Zapytania czytają przez `ILearningReader` (Infrastructure, `AsNoTracking`, `Select` do DTO). Każde dostaje `Audience` i `Language`:

- `ListTechnologiesQuery`, `GetTechnologyQuery` (z kursami i liczbą haseł),
- `GetTrackQuery` (lekcje bez treści, z `completed` dla zalogowanego), `GetLessonQuery` (HTML, poprzednia i następna widoczna lekcja),
- `ListLecturesQuery`, `GetLectureQuery`,
- `ListTermsQuery` z opcjonalnym `TechnologySlug`, `GetTermQuery` z odnośnikami zwrotnymi tylko do źródeł widocznych dla `Audience`,
- `PreviewMarkdownQuery` → `MarkdownPreview(string Html, IReadOnlyList<string> TermSlugs, IReadOnlyList<string> MissingTermSlugs)`.

### 7. Stan na szynie

`Learning.Contracts`:

```csharp
namespace JJDevHub.Learning.Contracts
{
    [EventName("learning.integration.track-changed.v1")]
    public sealed record TrackChangedIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredOn,
        Guid TrackId,
        int Version,
        string Slug,
        string TechnologySlug,
        LocalizedTextContract Title,
        string Visibility,
        IReadOnlyList<TrackLessonContract> Lessons,
        bool IsDeleted) : IIntegrationEvent
    {
        [JsonIgnore]
        public string PartitionKey => TrackId.ToString();
    }
}
```

`TrackLessonContract(Guid LessonId, string Slug, int Position, LocalizedTextContract Title, string Visibility, int? Minutes)`. Treść lekcji nie jedzie na szynę. Konsument potrzebuje faktu „jest lekcja”, a nie jej HTML-a.

Pozostałe zdarzenia w tym samym kształcie:

| Nazwa | Pola poza wspólnymi |
| --- | --- |
| `learning.integration.technology-changed.v1` | `TechnologyId`, `Name`, `Summary` |
| `learning.integration.lecture-changed.v1` | `LectureId`, `Title`, `Subtitle`, `Institution`, `Period`, `TechnologySlugs`, `LinkCount` |
| `learning.integration.term-changed.v1` | `TermId`, `Title`, `TechnologySlugs` |

Zdarzenia niosą slugi technologii, a nie id. Slug się nie zmienia, więc konsument nie musi łączyć dwóch tematów, żeby wiedzieć, której umiejętności dotyczy kurs.

`StateEvents.For(Track track, string technologySlug, TimeProvider time)` i odpowiedniki dla reszty agregatów składają zdarzenie z agregatu. `Version` w kontrakcie to `track.Version` po zapisie. Handler komendy nie woła outboxa wprost. Po `SaveAsync` woła port `IContentChanges` (`TechnologyChangedAsync`, `TrackChangedAsync`, `LectureChangedAsync`, `TermChangedAsync`, każda z flagą `deleted`). Implementacja `ContentChanges` składa zdarzenie przez `StateEvents` i woła `outbox.AddAsync`. Indeks wyszukiwania z [20-wyszukiwanie.md](20-wyszukiwanie.md) buduje projekcja, nie ta klasa. Strumień, rezerwacje i outbox wchodzą w transakcję `TransactionBehavior`. Komenda usunięcia dopisuje fakt usunięcia i woła port z `deleted: true`. Wiersz strumienia zostaje.

Skład w `Program.cs`:

```csharp
builder.Services.AddKafkaEventBus(builder.Configuration, options =>
{
    options.PublishedTopics = ["learning.integration"];
    options.TopicConfigs["learning.integration"] = new() { ["cleanup.policy"] = "compact" };
});
```

`KafkaOptions.TopicConfigs` (`Dictionary<string, Dictionary<string, string>>`) dochodzi do [13-kafka.md](13-kafka.md) w tym numerze. `KafkaTopicInitializer` przekazuje go do `TopicSpecification.Configs`. Tabela tematów w 13 dostaje wiersz `learning.integration` | Learning | 3 | `cv` (od [22-cv.md](22-cv.md)).

`RepublishStateCommand` ładuje agregaty ze strumieni paczkami po 100 i dla każdego zapisuje zdarzenie stanu do outboxa, paczka na transakcję. Zwraca liczbę zdarzeń. Usunięte agregaty jadą z `IsDeleted`.

### 8. Import z GitHuba

Port w Application:

```csharp
namespace JJDevHub.Learning.Application.Abstractions
{
    public interface IGitHubContentSource
    {
        Task<Result<IReadOnlyList<GitHubFile>>> ListMarkdownAsync(GitHubLocation location, CancellationToken cancellationToken);

        Task<string> ReadAsync(GitHubLocation location, string filePath, CancellationToken cancellationToken);
    }
}
```

`GitHubLocation(string Repo, string Ref, string Path)`, `GitHubFile(string Path, string Name, string Sha, long Size)`.

`GitHubContentClient(HttpClient http) : IGitHubContentSource`, rejestrowany przez `AddHttpClient`:

- `BaseAddress` `https://api.github.com/`, nagłówki `User-Agent: JJDevHub-Learning`, `X-GitHub-Api-Version: 2022-11-28`,
- `ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })`,
- `MaxResponseContentBufferSize` 1 MB, `Timeout` 30 s, `AddStandardResilienceHandler()` z ServiceDefaults,
- lista: `GET repos/{repo}/contents/{path}?ref={ref}` z `Accept: application/vnd.github+json`, tylko `type == "file"` z rozszerzeniem `.md`, posortowane po nazwie porządkiem ordinalnym,
- treść: ten sam adres dla pliku z `Accept: application/vnd.github.raw+json`,
- `404` → `ResultError.NotFound`, `301` → `Invalid` („repozytorium zmieniło nazwę”), `403` albo `429` z `x-ratelimit-remaining: 0` → `GitHubUnavailableException` z czasem z `x-ratelimit-reset`.

`ImportTrackFromGitHubCommand(Guid TrackId, string Repo, string Ref, string Path, Language Language)`. Walidator: `Repo` pasuje do `^[A-Za-z0-9-]{1,39}/[A-Za-z0-9._-]{1,100}$`, `Ref` do `^[A-Za-z0-9._/-]{1,100}$` bez `..`, `Path` bez `..`, `//` i początkowego `/`.

Handler:

- lista plików; więcej niż 200 → `Invalid`,
- dla każdego pliku slug z nazwy bez prefiksu `NN-` i rozszerzenia (`00-Wstep-React-Native.md` → `wstep-react-native`, czyli dzisiejszy slug z `pl.json`),
- plik z tym samym `sha` co w źródle lekcji → `Unchanged` bez pobrania,
- plik większy niż 1 MB albo lekcja z ręczną treścią w tym języku → `Skipped`,
- reszta: pobranie, front matter, tytuł z `title` albo pierwszego nagłówka `#` (usuwanego z treści), render z `MarkdownContext` importu, `Track.ImportLesson`,
- lekcje ze źródłem w tym języku, których plików nie ma na liście → `Orphaned`,
- jeden `SaveAsync`, jedno zdarzenie `track-changed.v1`, gdy cokolwiek się zmieniło.

Odpowiedź: `ImportReport(IReadOnlyList<string> Added, Updated, Unchanged, Skipped, Orphaned)`.

`GitHubUnavailableHandler : IExceptionHandler` w `Learning.API`, zarejestrowany przed `GlobalExceptionHandler` z [14-aspire.md](14-aspire.md). Zamienia wyjątek na `503` ProblemDetails z nagłówkiem `Retry-After`. Awaria zewnętrznego systemu to wyjątek IO, nie `Result`, zgodnie z zasadami z [README.md](README.md).

### 9. Import treści strony

`ImportLegacyContentCommand(JsonElement Pl, JsonElement En)` przyjmuje całe pliki `pl.json` i `en.json`. Pary szuka po slugu (oba pliki mają te same slugi w tej samej kolejności).

| Źródło | Cel |
| --- | --- |
| `courses.items[]` | `Lecture`; `summary[]` sklejone w akapity Markdown |
| `courses.items[].lessons[]` | `Lecture.Links` |
| `courses.items[].chapters[]`, `planned[]` | `Lecture.Chapters`, `Lecture.Planned` |
| `repoUrl` + `repoLabel`, `labUrl` + `labLabel` | `Lecture.Repository`, `Lecture.Lab` |
| `compendium.items[]` | `Term`; `definition[]` sklejone w akapity Markdown, bez technologii |
| `notes.items[]` | pomijane, idą do News w [21-notatki.md](21-notatki.md) |

Wszystko dostaje `Public`, bo dziś jest publiczne na stronie. Istniejący slug jest aktualizowany, nowy tworzony. Drugie uruchomienie nie tworzy duplikatów. Odpowiedź: `LegacyImportReport(int LecturesAdded, int LecturesUpdated, int TermsAdded, int TermsUpdated)`.

Wywołanie raz na produkcji:

```bash
jq -n --slurpfile pl src/Clients/web/src/content/pl.json --slurpfile en src/Clients/web/src/content/en.json \
  '{pl: $pl[0], en: $en[0]}' \
  | curl -sf -X POST https://hub.example.com/api/learning/import/legacy \
      -H "Authorization: Bearer $OWNER_TOKEN" -H 'Content-Type: application/json' --data @-
```

Usunięcie sekcji `courses` i `compendium` z plików klienta jest w [docs/frontend/react/05-tresc-z-api.md](../frontend/react/05-tresc-z-api.md).

### 10. Postęp studenta

`MarkLessonProgressCommand(Guid LessonId, string IdentityGuid, bool Completed, Audience Audience)`. Handler ładuje kurs ze strumienia. Lekcja niewidoczna dla `Audience` → `NotFound`. `Completed = true` dodaje wiersz `LessonProgress`, jeśli go nie ma, `false` go usuwa. Komenda nie dopisuje faktu do kursu i nie wysyła zdarzenia na szynę. `GetTrackProgressQuery` czyta tę tabelę w bazie zapisu, bo postęp nie jest projekcją.

`GetTrackProgressQuery(Guid TrackId)` → `TrackProgress(int StudentsStarted, IReadOnlyList<LessonProgressCount> Lessons)`, gdzie `LessonProgressCount(Guid LessonId, int Completed)`. Bez `IdentityGuid` w odpowiedzi.

### 11. Endpointy

Grupa `/api/learning` z `WithApiVersionSet` jak w [16-usluga-news.md](16-usluga-news.md). Odczyty przyjmują `?lang=pl|en` (domyślnie `pl`, inna wartość → `400`) i ustawiają `Content-Language`. Odczyt anonimowy dostaje `Cache-Control: public, max-age=60`, zalogowany `private, no-cache`, oba `Vary: Authorization`. Szczegół ma `ETag` z `Version` projekcji i odpowiada `304` na `If-None-Match`.

| Metoda i ścieżka | Polityka | Wynik |
| --- | --- | --- |
| `GET /api/learning/technologies`, `GET …/technologies/{slug}` | anonimowo | lista, szczegół z kursami |
| `GET /api/learning/tracks/{slug}` | anonimowo | kurs z lekcjami i `completed` dla zalogowanego |
| `GET /api/learning/tracks/{slug}/lessons/{lessonSlug}` | anonimowo | lekcja z HTML, poprzednia i następna |
| `GET /api/learning/lectures`, `GET …/lectures/{slug}` | anonimowo | sylabusy |
| `GET /api/learning/terms?technology=`, `GET …/terms/{slug}` | anonimowo | hasła, szczegół z odnośnikami zwrotnymi |
| `GET /api/learning/{kind}/{id:guid}` | `Owner` | dokument do edycji: źródła Markdown obu języków, widoczność, `ETag` z `Version`; kurs z lekcjami, ich źródłami (`LessonSource`) i pozycjami |
| `POST`, `PUT /{id}`, `DELETE /{id}` na `technologies`, `tracks`, `lectures`, `terms` | `Owner` | `201`/`200` z `ETag`, `400`, `404`, `412`, `428` |
| `PUT /api/learning/{kind}/{id}/visibility` | `Owner` | body `{ visibility }` + `If-Match` → `200` z `ETag` |
| `POST /api/learning/tracks/{id}/lessons`, `PUT …/lessons/{lessonId}`, `DELETE …/lessons/{lessonId}` | `Owner` | lekcje |
| `PUT /api/learning/tracks/{id}/lessons/order` | `Owner` | body z listą id → `200` |
| `DELETE /api/learning/tracks/{id}/lessons/{lessonId}/source` | `Owner` | odłączenie źródła |
| `POST /api/learning/tracks/{id}/import` | `Owner` | body `{ repo, ref, path, language }` → `200` z `ImportReport`, `503` z `Retry-After` |
| `POST /api/learning/preview` | `Owner` | body `{ markdown }`, do 200 KB → `200` z `MarkdownPreview` |
| `POST /api/learning/import/legacy` | `Owner` | `200` z `LegacyImportReport` |
| `POST /api/learning/republish` | `Owner` | `202` z liczbą zdarzeń |
| `PUT /api/learning/lessons/{id}/progress` | `Student` | body `{ completed }` → `204`, `404` |
| `GET /api/learning/tracks/{id}/progress` | `Owner` | `TrackProgress` |

`CurrentAudience` (scoped) woła `IAuthorizationService.AuthorizeAsync` z politykami `Owner` i `Student` z [15-identity.md](15-identity.md). Nazwy ról nie pojawiają się w kodzie usługi. `ResultHttpExtensions`, `EntityTag` i `CurrentUser` są te same co w News: przenoszą się z `News.API/Http/` do `JJDevHub.ServiceDefaults/Http/` w tym numerze, bo korzystają z nich dwie usługi. ServiceDefaults dostaje referencję do `JJDevHub.Domain` dla `ResultError`.

### 12. Skład, AppHost, gateway, CI

`Program.cs` jak w News: `AddServiceDefaults`, `AddDefaultAuthentication`, `AddNpgsqlDbContext<LearningWriteContext>(ResourceNames.LearningWrite, s => s.DisableRetry = true)`, osobno `LearningReadContext` na `learning-read`, `AddLearningApplication`, `AddLearningInfrastructure`, `AddIntegrationEventLog<LearningWriteContext>(typeof(TrackChangedIntegrationEvent).Assembly)`, `AddKafkaEventBus` z kroku 7, `AddDefaultApiVersioning`, `AddExceptionHandler<GitHubUnavailableHandler>()`, `DevelopmentMigrator` w dev. Learning niczego nie konsumuje, więc nie ma `AddKafkaConsumer`. `ProjectionWorker` startuje razem z API.

```csharp
var learningWrite = postgres.AddDatabase(ResourceNames.LearningWrite, "learning_write");
var learningRead = postgres.AddDatabase(ResourceNames.LearningRead, "learning_read");

var learningApi = builder.AddProject<Projects.Learning_API>(ResourceNames.LearningApi)
    .WithReference(learningWrite).WaitFor(learningWrite)
    .WithReference(learningRead).WaitFor(learningRead)
    .WithReference(kafka).WaitFor(kafka)
    .WithEnvironment("Identity__Url", identityApi.GetEndpoint("http"));
```

`ResourceNames` dostaje `LearningWrite = "learning-write"`, `LearningRead = "learning-read"` i `LearningApi = "learning-api"`. Gateway dostaje `WithReference(learningApi)` i trasę `/api/learning/{**catch-all}` → klaster `learning` (`http://learning-api`).

Wpis w `infra/ci/services.json` z [18-ci-uslug.md](18-ci-uslug.md): klucz `learning`, `paths` `src/services/Learning/**` i `tests/Learning.*/**`, trzy projekty testowe, obraz `jjdevhub/learning-api`, migracje `LearningWriteContext` i `LearningReadContext` z `Learning.Infrastructure`, `sonarKey` `jjdevhub-learning`. Job `learning-api` w `infra/prometheus/prometheus.yml`. `JJDevHub.Contracts.Tests` dostaje `Learning.Contracts` w katalogu i próbki czterech zdarzeń.

### Co jest do czego

- `Technology` — korzeń kompendium. Kurs i hasło wiszą pod technologią, a CV dopasowuje do niej umiejętność.
- `Track` i `Lesson` — kurs od A do Z. Kolejność, szkice i import pilnuje agregat, nie endpoint.
- `Lecture` — sylabus z uczelni. Dzisiejszy `CourseContent`, tylko w bazie zamiast w bundlu.
- `Term` — hasło słownika. Sieć haseł powstaje z `[[slug]]`, bez ręcznego utrzymywania listy „zobacz też”.
- `Visibility` i `Audience` — jedno pytanie „czy ten człowiek to zobaczy”, zadawane w SQL. Prywatna baza wiedzy ownera i materiały dla studentów żyją obok treści publicznej.
- `Version` — numer faktu w strumieniu. `If-Match`, konflikt zapisu i konsument na szynie patrzą na tę samą liczbę.
- `MarkdownRenderer` — jedyne miejsce, które zamienia Markdown na HTML. Podgląd woła je wprost, a zapis i import zostawiają źródło w zdarzeniu. Projekcja renderuje widok.
- `WikiLinkParser` i `TermLinks` — linki między hasłami i odnośniki zwrotne.
- `GitHubContentClient` — kursy piszą się w repo, a hub je pokazuje. Rozmawia tylko z `api.github.com`.
- `ImportReport` — owner widzi, co się zmieniło i co zostało osierocone, zanim cokolwiek zniknie.
- `ImportLegacyContentCommand` — jednorazowe przeniesienie treści z bundla do bazy.
- `LessonProgress` — postęp studenta. Nie wychodzi z usługi i nie pokazuje nazwisk.
- `StateEvents` i `learning.integration` — kopia stanu dla innych usług. Kompaktowany temat zastępuje endpoint do zasilenia nowego konsumenta.
- `ContentProjection` — widok strony, HTML i `TermLinks` ze strumienia. Nowa wersja renderera przebudowuje widok od pozycji 0.
- `ContentSlugs` i `TechnologyUses` — unikalność i zakaz usunięcia technologii, których strumień jednego agregatu nie sprawdzi.
- `IContentChanges` — po zapisie tylko outbox. Handler nie zna Kafki, a indeksu nie dotyka.
- `GitHubUnavailableHandler` — limit GitHuba jako `503` z czasem ponowienia, a nie `500`.

## Testy

Projekt `tests/Learning.Domain.Tests`, bez bazy.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Slug_from_polish_title_strips_diacritics` | „Wstęp: czym jest React Native” | `Slug.From` | `wstep-czym-jest-react-native` |
| `Slug_rejects_uppercase` | `"Cqrs"` | `Slug.Create` | `Invalid` |
| `Localized_text_falls_back_to_polish` | `En` puste | `For(Language.En)` | tekst polski, język `Pl` |
| `External_url_rejects_javascript_scheme` | `javascript:alert(1)` | `ExternalUrl.Create` | `Invalid` |
| `Every_change_appends_event` | nowy kurs | `AddLesson` | na liście zdarzeń `LessonAdded`, `Version` nadal 0 do zapisu |
| `Duplicate_lesson_slug_is_invalid` | kurs z lekcją `intro` | `AddLesson` z `intro` | `Invalid` |
| `Reorder_must_list_every_lesson` | trzy lekcje | `ReorderLessons` z dwoma id | `Invalid` |
| `Imported_lesson_is_read_only` | lekcja ze źródłem | `ReviseLesson` | `Invalid` |
| `Import_with_same_sha_is_unchanged` | lekcja z `sha` `a1` | `ImportLesson` z `a1` | `Unchanged`, lista zdarzeń bez nowego faktu |
| `New_imported_lesson_is_private` | pusty kurs | `ImportLesson` | lekcja `Private` |
| `Effective_visibility_is_stricter` | `Public` i `Private` | `Stricter` | `Private` |
| `Student_does_not_see_private` | `Private` | `IsVisibleTo(Audience.Student)` | `false` |

Projekt `tests/Learning.Infrastructure.Tests`. Klasa `MarkdownRendererTests`, bez bazy.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Raw_html_is_not_rendered` | `<script>alert(1)</script>` | `Render` | brak tagu `script` w HTML |
| `Javascript_link_is_removed` | `[x](javascript:alert(1))` | `Render` | brak `javascript:` w HTML |
| `Generic_attributes_are_not_parsed` | `# T {onclick=alert(1)}` | `Render` | brak `onclick` |
| `Headings_get_github_ids` | `## Getting started` | `Render` | `id="getting-started"` |
| `Wiki_link_is_rendered_and_collected` | `Zobacz [[cqrs\|CQRS]]` | `Render` | `data-term="cqrs"`, `TermSlugs` zawiera `cqrs` |
| `Relative_image_points_to_raw_github` | `![a](img/a.png)` z kontekstem importu | `Render` | `src` na `raw.githubusercontent.com/...` |
| `Plain_text_has_no_markup` | `**a** [b](https://x)` | `Render` | `PlainText` po przycięciu równe `a b` |
| `Code_keeps_language_class` | blok ` ```csharp ` | `Render` | `class="language-csharp"` |

Klasa `GitHubContentClientTests`. `FakeGitHubHandler : HttpMessageHandler` z odpowiedziami po ścieżce zapisuje każde żądane URI.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Client_only_calls_api_github_com` | lista z `download_url` na obcy host | `ListMarkdownAsync` i `ReadAsync` | każde URI ma host `api.github.com` |
| `Listing_skips_directories_and_non_markdown` | katalog, `.png`, dwa `.md` | `ListMarkdownAsync` | dwa pliki |
| `Redirect_is_invalid` | `301` | `ListMarkdownAsync` | `Invalid` |
| `Rate_limit_throws_with_retry_after` | `403` z `x-ratelimit-remaining: 0` | `ListMarkdownAsync` | `GitHubUnavailableException`, `RetryAfter` z `x-ratelimit-reset` |

Projekt `tests/Learning.API.Tests`. `LearningApiFactory` z kontenerem Postgresa (bazy `learning_write` i `learning_read`), `RecordingEventBus`, wyłączonym workerem projekcji (test woła `RunOnceAsync`), testowymi tokenami z rolą (`TestTokens.Owner()`, `Student()`, `For(identityGuid)`) i `FakeGitHubHandler` w miejsce głównego handlera klienta. Fixture `Fixtures/pl.json` i `en.json` to kopie plików klienta z dnia napisania testu. Odczyt w teście idzie po przebiegu projekcji.

Klasa `VisibilityTests`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Anonymous_gets_404_for_students_track` | kurs `Students` | `GET` bez tokenu | `404` |
| `Student_sees_students_track` | kurs `Students` | `GET` z tokenem studenta | `200` |
| `Student_gets_404_for_private_term` | hasło `Private` | `GET` z tokenem studenta | `404` |
| `Owner_sees_private_term` | hasło `Private` | `GET` z tokenem ownera | `200` |
| `Private_lesson_is_hidden_in_public_track` | kurs `Public`, lekcja `Private` | `GET` kursu anonimowo | lekcji nie ma na liście |
| `Backlinks_hide_private_sources` | hasło `cqrs`, link z prywatnej lekcji | `GET /terms/cqrs` anonimowo | odnośników zwrotnych zero |

Klasa `AuthoringTests`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Student_cannot_write` | token studenta | `POST /api/learning/terms` | `403` |
| `Put_with_stale_if_match_returns_412` | hasło w wersji 2 | `PUT` z `If-Match: "1"` | `412` |
| `Preview_matches_saved_html` | Markdown z tabelą i `[[cqrs]]` | `POST /preview`, potem zapis lekcji i `GET` | HTML identyczny |
| `Preview_reports_missing_terms` | brak hasła `outbox` | `POST /preview` z `[[outbox]]` | `MissingTermSlugs` zawiera `outbox` |
| `Every_write_publishes_state` | `[Theory]` po komendach zapisu | komenda | jeden wiersz outboxa `learning.integration.*-changed.v1` z nową `Version` |
| `Delete_publishes_tombstone` | hasło `cqrs` | `DELETE` | zdarzenie z `IsDeleted` `true`, strumień nadal jest |
| `Renderer_bump_rebuilds_without_outbox` | lekcja w widoku ze starszym `RendererVersion` | projekcja od pozycji 0 | HTML odświeżony, zero nowych wierszy outboxa |
| `Republish_writes_event_per_aggregate` | 2 technologie, 1 kurs, 3 hasła | `POST /republish` | 6 wierszy outboxa |

Klasa `GitHubImportTests`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `First_import_adds_lessons_in_file_order` | trzy pliki `00-`, `01-`, `02-` | `POST /import` | `Added` 3, pozycje 1–3, slugi bez prefiksów |
| `Second_import_downloads_only_listing` | po pierwszym imporcie | drugi `POST /import` | `Unchanged` 3, jedno żądanie do fałszywego GitHuba |
| `Changed_blob_updates_one_lesson` | `sha` jednego pliku zmieniony | `POST /import` | `Updated` 1, pobrany jeden plik |
| `Removed_file_is_orphaned` | plik zniknął z listy | `POST /import` | `Orphaned` 1, lekcja nadal jest |
| `Url_in_repo_is_rejected` | `repo: "https://evil.example/x"` | `POST /import` | `400`, zero żądań |
| `Dot_dot_in_path_is_rejected` | `path: "../secrets"` | `POST /import` | `400` |
| `Rate_limit_returns_503_with_retry_after` | fałszywy GitHub zwraca limit | `POST /import` | `503`, nagłówek `Retry-After` |

Klasa `LegacyImportTests`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Legacy_import_creates_lectures_and_terms` | fixture `pl.json` i `en.json` | `POST /import/legacy` | 2 wykłady, 12 haseł, `mobile` ma 12 linków |
| `Legacy_import_keeps_english` | ten sam import | `GET /lectures/mobile?lang=en` | tytuł z `en.json`, `Content-Language: en` |
| `Second_legacy_import_has_no_duplicates` | po imporcie | drugi import | `LecturesAdded` 0, nadal 2 wykłady |

Klasa `ProgressTests`.

| Fakt | Arrange | Act | Assert |
| --- | --- | --- | --- |
| `Student_marks_lesson_completed` | lekcja `Students` | `PUT …/progress` z `true`, potem `GET` kursu | `204`, lekcja `completed` |
| `Anonymous_progress_returns_401` | brak tokenu | `PUT …/progress` | `401` |
| `Progress_on_hidden_lesson_returns_404` | lekcja `Private` | `PUT …/progress` studentem | `404` |
| `Owner_stats_have_no_identities` | dwóch studentów | `GET …/progress` ownerem | liczby 2, w JSON-ie brak `identityGuid` |

`tests/JJDevHub.EventBus.Kafka.Tests` dostaje `Initializer_applies_topic_config`: temat z `TopicConfigs` ma po starcie `cleanup.policy=compact` w `DescribeConfigsAsync`. `AppHostTests` dostaje `Gateway_routes_learning`: `GET /api/learning/technologies` na `gateway` → `200`.

`dotnet test tests/Learning.Domain.Tests`, `dotnet test tests/Learning.Infrastructure.Tests`, `dotnet test tests/Learning.API.Tests`, `dotnet test tests/JJDevHub.EventBus.Kafka.Tests`, `dotnet test tests/JJDevHub.Contracts.Tests` i `dotnet test tests/JJDevHub.AppHost.Tests`.

## Zasady

- Każde zapytanie filtruje widoczność w SQL. Treść niewidoczna dla wywołującego to `404`.
- HTML powstaje tylko w `MarkdownRenderer`. Podgląd woła go w komendzie, widok strony w projekcji. Zapis trzyma w strumieniu źródło.
- Import rozmawia tylko z `api.github.com`. Żądanie niesie `owner/repo`, nie URL. Klient nie podąża za przekierowaniem ani za `download_url`.
- Lekcja ze źródłem jest tylko do odczytu, dopóki owner nie odłączy źródła.
- Każda zmiana agregatu dopisuje fakty do strumienia i jedno zdarzenie `*-changed.v1` w tej samej transakcji. `Version` na szynie jest wersją strumienia.
- Zdarzenie niesie pełny stan z widocznością. Filtr widoczności robi konsument.
- Postęp studenta nie wychodzi z usługi. Statystyki dla ownera nie mają `IdentityGuid`.
- Slug nie zmienia się po utworzeniu.
- Strumień jest źródłem prawdy technologii, kursu, wykładu i hasła. Postęp studenta jest tabelą w bazie zapisu.

## Poza zakresem

- Wyszukiwanie pełnotekstowe i semantyczne — [20-wyszukiwanie.md](20-wyszukiwanie.md).
- Notatki przypięte do lekcji — [21-notatki.md](21-notatki.md).
- Ekrany intranetu — [docs/frontend/intranet/01-dostep-i-trasy.md](../frontend/intranet/01-dostep-i-trasy.md) i [02-edytor.md](../frontend/intranet/02-edytor.md).
- Webhook `push` z GitHuba i token GitHuba (limit 5000 żądań na godzinę). Ten sam importer, inne wywołanie.
- Repozytoria prywatne.
- Zmiana sluga opublikowanej treści z przekierowaniem `301`.
- Zapisy na konkretny kurs, quizy, zadania i certyfikaty.
- Upload obrazów. Obrazy przychodzą z repozytorium kursu.

## Źródła

- [Markdig](https://github.com/xoofx/markdig) — parser CommonMark, rozszerzenia, własne parsery inline.
- [CommonMark Spec](https://spec.commonmark.org/) — składnia, którą Markdig implementuje.
- [HtmlSanitizer](https://github.com/mganss/HtmlSanitizer) — lista dozwolonych tagów, atrybutów i schematów.
- [OWASP Cross Site Scripting Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html) — sanitizacja HTML z treści użytkownika.
- [OWASP Server-Side Request Forgery Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Server_Side_Request_Forgery_Prevention_Cheat_Sheet.html) — stały host zamiast filtrowania adresów.
- [GitHub REST API: repository contents](https://docs.github.com/en/rest/repos/contents) — lista katalogu, `sha`, media type `raw`.
- [GitHub REST API: rate limits](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api) — 60 żądań na godzinę bez tokenu, nagłówki `x-ratelimit-*`.
- [What do you mean by “Event-Driven”?](https://martinfowler.com/articles/201701-event-driven.html) — Martin Fowler: powiadomienie kontra event-carried state transfer.
- [Apache Kafka: log compaction](https://kafka.apache.org/documentation/#compaction) — ostatnia wiadomość na klucz.
- [EF Core: complex types and JSON columns](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew#complex-types) — `ComplexProperty` i `ToJson()`.
- [Event Sourcing pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing) — strumień treści i widok złożony wstecz.
- [Npgsql: arrays](https://www.npgsql.org/efcore/mapping/array.html) — `uuid[]` i `= ANY` w projekcji.
- [dotnet/eShop — Ordering](https://github.com/dotnet/eShop/tree/main/src/Ordering.API) — usługa z domeną obok prostszego CRUD-u.

## Gotowe gdy

- `aspire run` stawia `learning-api`, a `GET /api/learning/technologies` przez gateway zwraca `200`.
- Import treści strony przenosi 2 wykłady i 12 haseł. Drugi import niczego nie dubluje.
- Import kursu z repozytorium tworzy lekcje w kolejności plików, a drugi import bez zmian kończy się samym `Unchanged`.
- Anonim i student nie widzą treści `Private`, a anonim nie widzi `Students`.
- Każdy zapis zostawia fakty w `learning_write` i jeden wiersz outboxa `learning.integration.*-changed.v1`. Temat `learning.integration` ma `cleanup.policy=compact`. Widok w `learning_read` dogania strumień.
- `services.json` i `prometheus.yml` mają wpis `learning`.
- `dotnet test` dla sześciu projektów z sekcji Testy kończy się kodem 0.
