# 02. Porty i przypadki użycia

Poprzedni numer to [jądro i domena](01-kernel-i-domena.md). Następny to [Postgres](03-postgres.md).

Ten numer stawia `Learning.Application`: porty zapisu i handlery. Handler dostaje interfejs, nie `DbContext`. Implementacji bazy jeszcze nie ma. Test podstawia magazyn w pamięci. Mediatora z paczki nie ma. Handler jest klasą z jedną metodą `HandleAsync`.

## Po co

Endpoint, który sam sprawdza tytuł, sam otwiera transakcję i sam pisze SQL, nie da się wywołać z drugiego miejsca bez kopiowania. Handler zamyka jeden przypadek użycia. Port zamyka kierunek zależności: aplikacja nie referencjonuje EF, infrastruktura referencjonuje aplikację.

Bez tego podziału test reguły „nie publikuj kursu bez technologii” podnosiłby bazę. Reguła nie potrzebuje bazy. Potrzebuje portu, który w teście pamięta, czy technologia istnieje.

## Dlaczego ta technologia

### Handler zamiast MediatR

Klasa `PublishCourseHandler` przyjmuje komendę i zwraca `Task<Result<Guid>>`. Rejestracja jest `AddScoped` w rozszerzeniu `AddLearningApplication`. Problem, który zamyka, to paczka MediatR od linii 13, której licencja produkcyjna nie jest wolna. Odpada też starsza linia MediatR przypięta po to, żeby licencję ominąć. Koszt: nie ma magicznego `Send`. Widać, kto woła handler. Przy pięciu komendach na usługę to jest zaleta, nie brak.

### Porty są małe

`ICourseRepository` umie dodać kurs, pobrać go po slugu i po identyfikatorze. Nie umie „wykonać dowolnego zapytania”. Problem, który zamyka, to interfejs z dwudziestoma metodami, który jest drugim `DbContext`. Odpada generyczny repozytorium na wszystko. Koszt: nowy odczyt to nowa metoda albo, później, osobny port odczytu. Odczyt Dapperem nie implementuje tego samego interfejsu co zapis.

`IUnitOfWork.SaveChangesAsync` jest jedynym miejscem, w którym handler prosi o commit. Handler nie wie, że commitem będzie transakcja EF.

### Komenda jest rekordem

`PublishCourseCommand` jest `sealed record` z polami wejścia. Nie jest encją. Problem, który zamyka, to endpoint przekazujący encję z półproduktu, którą ktoś już podmienił. Odpada przyjmowanie `Course` z ciała HTTP. Koszt: jest mapowanie z komendy na fabrykę. Jest ręczne i jest w handlerze. AutoMapper nie wchodzi.

### Zegar i token

Handler dostaje `TimeProvider` i `CancellationToken`. Nie tworzy `CancellationToken.None` w środku, poza testem, który celowo nie anuluje. Problem, który zamyka, to operacja, której nie da się przerwać przy zatrzymaniu procesu. Odpada `async` bez tokenu.

## Pojęcia

| Słowo | Znaczenie |
| --- | --- |
| port | interfejs, którego implementacja leży w infrastrukturze |
| handler | jedna klasa, jedna komenda, jedna metoda `HandleAsync` |
| komenda | rekord wejścia, nie encja |
| `IUnitOfWork` | prośba o commit, bez wiedzy o EF |

## Klasy i ścieżki

| Ścieżka | Odpowiedzialność |
| --- | --- |
| `Learning.Application/Ports/ITechnologyRepository.cs` | zapis i odczyt technologii po slugu |
| `Learning.Application/Ports/ICourseRepository.cs` | to samo dla kursu |
| `Learning.Application/Ports/ILessonRepository.cs` | to samo dla lekcji |
| `Learning.Application/Ports/ICategoryRepository.cs` | półka |
| `Learning.Application/Ports/IStudentProgressStore.cs` | wiersz ukończenia |
| `Learning.Application/Ports/IUnitOfWork.cs` | `SaveChangesAsync` |
| `Learning.Application/UseCases/PublishTechnology.cs` | komenda i handler |
| `Learning.Application/UseCases/PublishCourse.cs` | odmawia, gdy nie ma technologii |
| `Learning.Application/UseCases/PublishLesson.cs` | odmawia, gdy nie ma kursu |
| `Learning.Application/UseCases/ReviseLesson.cs` | zmienia Markdown i nazwę, nie slug |
| `Learning.Application/UseCases/CompleteLesson.cs` | zapisuje postęp |
| `Learning.Application/LearningApplication.cs` | `AddLearningApplication` |
| `tests/Learning.Application.Tests/` | magazyn w pamięci |

Projekt aplikacji referencjonuje domenę i jądro oraz `Microsoft.Extensions.DependencyInjection.Abstractions`. Nie referencjonuje EF.

## Plan

### 1. Komenda kursu

```csharp
public sealed record PublishCourseCommand(
    string Slug,
    string TechnologySlug,
    string TitlePl,
    string TitleEn,
    string Visibility);

public sealed class PublishCourseHandler(
    ICourseRepository courses,
    ITechnologyRepository technologies,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<Result<Guid>> HandleAsync(PublishCourseCommand command, CancellationToken ct)
    {
        var technology = await technologies.GetBySlugAsync(command.TechnologySlug, ct);
        if (technology is null)
            return Result<Guid>.Fail(ResultErrorKind.NotFound, "learning.technology.missing");
        var published = Course.Publish(/* slug, tytuł, visibility, clock */);
        if (!published.IsSuccess)
            return Result<Guid>.Fail(published.Error);
        await courses.AddAsync(published.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Ok(published.Value.Id);
    }
}
```

Fragment jest kontraktem kolejności: najpierw sąsiad w tej samej usłudze, potem fabryka, potem port, potem commit. Technologia jest w tej samej bazie, więc to nie jest wołanie obcej usługi. Slug technologii jest wartością, nie kluczem z cudzej bazy.

### 2. Rejestracja

`AddLearningApplication` rejestruje handlery jako `Scoped`. `TimeProvider.System` jest `Singleton`, dopóki test nie poda `FakeTimeProvider`. Test nie woła rozszerzenia: składa handler ręcznie. Rozszerzenie jest dla hosta.

### 3. Postęp

`CompleteLessonHandler` sprawdza, że lekcja istnieje i że wołający ma identyfikator studenta. Identyfikator przychodzi w komendzie jako `string StudentId`. Skąd się bierze, powie numer JWT. Tu handler odmawia pustego identyfikatora kodem `learning.student.missing`. Nie czyta nagłówka HTTP. Nie wie, że istnieje HTTP.

### Co jest do czego

| Handler | Porty | Odmowa, którą widać w teście |
| --- | --- | --- |
| `PublishTechnologyHandler` | technologie, kategorie, unit of work | brak kategorii, zły slug |
| `PublishCourseHandler` | kursy, technologie, unit of work | brak technologii |
| `PublishLessonHandler` | lekcje, kursy, unit of work | brak kursu |
| `ReviseLessonHandler` | lekcje, unit of work | nieznany slug, zamrożony slug w komendzie zmiany sluga |
| `CompleteLessonHandler` | postęp, lekcje, unit of work | brak lekcji, drugi raz jest sukcesem |

## Testy

Nazwa: `Publish_course_without_technology_returns_missing`.

| Arrange | Act | Assert |
| --- | --- | --- |
| magazyn technologii pusty | `PublishCourseHandler.HandleAsync` | kod `learning.technology.missing`, `SaveChangesAsync` nie został wołany |

Magazyn w pamięci jest w projekcie testowym, nie w aplikacji. Aplikacja nie dostaje „oficjalnej” implementacji w pamięci, która potem pojedzie na produkcję przez pomyłkę.

```bash
dotnet test tests/Learning.Application.Tests --nologo
```

## Checkpoint

```bash
dotnet test tests/Learning.Application.Tests --nologo --filter "Category!=Integration"
dotnet build src/services/Learning/Learning.Application/Learning.Application.csproj
```

Build aplikacji nie przywraca paczki EF. `dotnet list package` tego projektu pokazuje tylko abstrakcje DI.

## Umiesz wyjaśnić

Dlaczego nie ma `IMediator.Send`?

Bo paczka, która to daje w aktualnej linii, nie ma licencji wolnej produkcyjnie. Handler wołany po typie jest krótszy w czytaniu niż kontener komunikatów przy tej liczbie komend.

Dlaczego test nie podnosi Postgresa?

Bo sprawdza odmowę, którą da się wyrazić kolekcją w pamięci. Baza sprawdziłaby dodatkowo sieć i migrację, czyli dwa powody padnięcia. Jeden test, jeden powód.

Kto commituje?

`IUnitOfWork` w handlerze, raz, na końcu sukcesu. Przy odmowie fabryki commit nie następuje. Infrastruktura zdecyduje, czym jest commit.

Dlaczego aplikacja nie widzi `CancellationToken.None`?

Bo `None` mówi „nie da się przerwać”. Token przychodzi z hosta. Test przekazuje `CancellationToken.None` tylko wtedy, gdy sam nie sprawdza anulowania.

Czym port zapisu różni się od przyszłego odczytu?

Zapis zwraca encję albo jej brak. Odczyt zwróci DTO pod slug i język i nie będzie tym interfejsem. Handler komendy nie woła odczytu.

## Zasady

- Aplikacja nie referencjonuje EF, Kafki ani ASP.NET.
- Jedna komenda, jeden handler, jedna metoda publiczna `HandleAsync`.
- Mapowanie jest ręczne.
- Magazyn w pamięci nie leży w projekcie produkcyjnym.
- Handler nie czyta `HttpContext`.

## Poza zakresem

- Baza jest w [03-postgres.md](03-postgres.md), adapter EF w [04-ef-core.md](04-ef-core.md).
- HTTP jest w [05-rest.md](05-rest.md).
- Zdarzenie na szynę jest w [02-outbox.md](../04-zdarzenia/02-outbox.md). Ten numer jeszcze nic nie publikuje.
- Rola `owner` przy publikacji jest w [03-jwt.md](../05-tozsamosc/03-jwt-i-policies.md). Do tego czasu endpoint nie istnieje, więc nie ma też dziury w autoryzacji HTTP. Jest luka świadoma: handler nie sprawdza roli, bo nie zna aktora. Numer JWT doda port `ICurrentActor` i handler publikacji zacznie odmawiać brakowi `owner`.

## Źródła

- [Hexagonal architecture](https://alistair.cockburn.us/hexagonal-architecture/). Wiążący jest port po stronie aplikacji i adapter na zewnątrz. Dotyczy kierunku referencji. Ze źródła nie bierzemy frameworka portów.
- [Dependency injection in .NET](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection). Wiążące jest `Scoped` dla handlera i repozytorium. Dotyczy `AddLearningApplication`. Ze źródła nie bierzemy service locatora w handlerze.
- [Licencja MediatR](https://github.com/LuckyPennySoftware/MediatR). Wiążące jest to, że aktualna linia nie jest wolna produkcyjnie tak, jak wymaga [technologie.md](../00-fundamenty/technologie.md). Dotyczy braku paczki. Ze źródła nie bierzemy przypięcia starej linii.

## Gotowe gdy

- Test braku technologii przechodzi bez kontenera i bez wołania `SaveChanges` przy odmowie.
- `Learning.Application` nie ma referencji do EF.
- Handlery z tabeli są zarejestrowane w `AddLearningApplication`.
- W produkcyjnym kodzie nie ma klasy `InMemoryCourseRepository`.
- Komenda kursu nie przyjmuje encji `Course`.
