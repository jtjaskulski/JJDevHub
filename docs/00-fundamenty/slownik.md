# Słownik

Nazwa w tym pliku jest stała. Numer nie wprowadza drugiej nazwy na ten sam byt. Gdy numer i słownik się różnią, wygrywa słownik, a numer jest poprawiany w tym samym pull requeście.

## Repozytorium

| Nazwa | Znaczenie |
| --- | --- |
| `develop`, `release`, `main` | trzy długie gałęzie |
| gałąź robocza | gałąź wycięta z `develop` |
| scope | zakres w tytule: komponent, `infra` albo `shared` |
| ruleset | zestaw reguł GitHub w `.github/rulesets` |
| check | nazwa joba, której ruleset wymaga przed scaleniem |
| komponent | `identity`, `news`, `learning`, `cv`, `contact`, `gateway`, `web`, `mobile` |
| `shared` | zakres dla `JJDevHub.Shared.Kernel`, bez własnego tagu obrazu |
| `infra` | zakres bez tagu obrazu usługi |
| tag | `<komponent>-vX.Y.Z`, na przykład `news-v1.4.0` |
| SBOM | wykaz składników obrazu w SPDX |
| atestacja | powiązanie digestu z przebiegiem GitHub Actions |

Zakresy `feat` i `fix`: `identity`, `news`, `learning`, `cv`, `contact`, `gateway`, `web`, `mobile`, `infra`, `shared`.

Tytuł wzorcowy: `feat(news): publish article revision`. Nazwa wzorcowa: `feature/lesson-markdown`. Nazwa, która pada: `feature/lesson_markdown`.

Prefiksy gałęzi: `feature/`, `fix/`, `docs/`, `chore/`, `ci/`, `refactor/`, `test/`, dalej kebab-case.

## Checki

Nazwa joba jest nazwą checka.

| Check | Workflow |
| --- | --- |
| `branch-name` | `.github/workflows/branch-name.yml` |
| `pr-title` | `.github/workflows/pr-title.yml` |
| `pr-size` | `.github/workflows/pr-size.yml` |
| `build` | `.github/workflows/build.yml` |
| `unit-tests` | `.github/workflows/unit-tests.yml` |
| `format` | `.github/workflows/format.yml` |
| `integration-tests` | `.github/workflows/integration-tests.yml` |
| `performance` | `.github/workflows/performance.yml` |
| `codeql` | `.github/workflows/codeql.yml` |
| `dependency-vulnerabilities` | `.github/workflows/dependencies.yml` |
| `sonarqube` | `.github/workflows/sonarqube.yml` |
| `gitleaks` | `.github/workflows/gitleaks.yml` |
| `zizmor` | `.github/workflows/zizmor.yml` |
| `action-pins` | `.github/workflows/action-pins.yml` |
| `scorecard` | `.github/workflows/scorecard.yml` |
| `image-scan`, `sbom`, `attestations` | `.github/workflows/artifacts.yml` |
| `release-tags` | `.github/workflows/versions.yml` |
| `web-e2e` | `.github/workflows/web-e2e.yml` |
| `terraform-fmt` | `.github/workflows/terraform-fmt.yml` |

Deploy nie jest checkiem pull requestu. Jego plik to `.github/workflows/deploy.yml`. Job `publish` buduje obraz na `ubuntu-latest`. Job `deploy` działa na runnerze `jjdevhub`.

`develop`, `release` i `main` wymagają tej samej listy checków z tabeli. Wejście na `develop` jest squashem. Wejście na `release` i `main` jest merge commitem.

## Role i osoby

| Nazwa | Znaczenie |
| --- | --- |
| `owner` | właściciel treści, dwie skrzynki, intranet |
| `student` | czytelnik lekcji i autor `StudentInquiry` |
| rekruter | nie ma konta; formularz publiczny tworzy `RecruiterInquiry` |

Claim roli w tokenie nazywa się `role`. Wartość to `owner` albo `student`.

## Usługi i projekty

Usługa z domeną ma projekty `Domain`, `Application`, `Infrastructure`, `Api`, `Contracts`, `Relay`, `Projector`. Ścieżka to `src/services/<Nazwa>/`. `<Nazwa>` przyjmuje `Identity`, `News`, `Learning`, `Cv`, `Contact`.

| Nazwa | Ścieżka |
| --- | --- |
| `JJDevHub.Shared.Kernel` | `src/shared/JJDevHub.Shared.Kernel` |
| `JJDevHub.ServiceDefaults` | `src/service-defaults/JJDevHub.ServiceDefaults` |
| `JJDevHub.AppHost` | `src/apphost/JJDevHub.AppHost` |
| `JJDevHub.Gateway` | `src/gateway/JJDevHub.Gateway` |
| `JJDevHub.EventStore` | `src/shared/JJDevHub.EventStore`, od numeru strumienia |
| web | `src/clients/web` |
| telefon | `src/clients/mobile` |
| `tokens` | `src/clients/shared/theme/tokens.ts` |
| chrom | `src/clients/shared/i18n/pl.json` i `en.json` |

Framework projektów to `net10.0`. Jeden publiczny typ na plik. Klasa implementacji jest `sealed`. Rekord niesie dane. Metoda `async` przyjmuje `CancellationToken`.

## Jądro

| Nazwa | Znaczenie |
| --- | --- |
| `Result<T>` | sukces z wartością albo `ResultError` |
| `Result` | sukces bez wartości albo `ResultError` |
| `ResultError` | rodzaj i stabilny kod |
| `ResultErrorKind` | `None`, `Invalid`, `NotFound`, `Conflict`, `Forbidden` |
| `Entity` | tożsamość `Guid` |
| `ValueObject` | równość po składowych |
| `TimeProvider` | zegar z BCL; test podstawia `FakeTimeProvider` |
| `LocalizedText` | mapa kultury na tekst, na start `pl` i `en` |
| `Slug` | identyfikator w adresie; po publikacji się nie zmienia |
| `IDomainEvent` | fakt, który już zaszedł; wchodzi z agregatem News |
| `EventNameAttribute` | nazwa `segment.fakt.vN` |
| `AggregateRoot` | wersja, `Apply`, `Replay` |
| `IEventStore` | dopisanie i odczyt strumienia |
| `StreamKey` | klucz strumienia |

Kod błędu ma gramatykę `segment.element.powod`. Nazwa zdarzenia ma gramatykę `segment.fakt-z-myślnikami.vN`. Nazwa klasy CLR nie wchodzi do bazy ani do Kafki.

## Agregaty i stan

| Nazwa | Usługa | Trwałość |
| --- | --- | --- |
| `Account` | Identity | wiersz EF |
| `Invite` | Identity | wiersz EF |
| `RegistrationSaga` | Identity | maszyna stanów w `identity_write` |
| `Category` | Learning | wiersz EF, bez historii brzmień |
| `Technology` | Learning | wiersz EF, slug niezmienny |
| `Course` | Learning | wiersz EF |
| `Lesson` | Learning | wiersz EF, Markdown w `LocalizedText` |
| `StudentProgress` | Learning | wiersz stanu, bez strumienia i bez tematu integracyjnego |
| `Article` | News | strumień |
| `Profile`, `Position`, `Project`, `Education`, `Activity`, `Skill`, `Language` | Cv | strumień na sekcję |
| `StudentInquiry` | Contact | wiersz EF, tylko JWT `student` |
| `RecruiterInquiry` | Contact | wiersz EF, formularz bez konta |
| `DeviceToken` | Contact | wiersz, tylko zalogowany `owner` |

`Skill` trzyma slug technologii, nie klucz obcy do `learning_read`.

Widoczność treści: `public`, `students`, `private`.

## Bazy

Jeden serwer, zasób Aspire o nazwie `postgres`.

| Baza | Usługa |
| --- | --- |
| `identity_write`, `identity_read` | Identity |
| `news_write`, `news_read` | News |
| `learning_write`, `learning_read` | Learning |
| `cv_write`, `cv_read` | Cv |
| `contact_write`, `contact_read` | Contact |

## Szyna

Broker ma jedną nazwę zasobu: `kafka`. `auto.create.topics.enable` jest wyłączone.

| Topic | Klucz | Uwaga |
| --- | --- | --- |
| `learning.catalog` | slug | fakty katalogu, retention zwykła |
| `learning.integration` | slug | kompaktowany, ostatni stan technologii i kursu |
| `news.articles` | slug | fakty notatki |
| `identity.registration` | identyfikator sagi | prośba rejestracji, bez wyniku |
| `learning.enrollment` | identyfikator sagi | potwierdzenie albo odmowa zapisu studenta |
| `cv.sections` | identyfikator sekcji | fakty sekcji CV, bez kompaktowania |
| `contact.integration` | identyfikator zapytania | dwa fakty push |
| `audit.events` | identyfikator encji | skrót wpisu audytu, jedna partycja |

Nazwy faktów, które są stałe od numeru, który je wprowadza:

| Nazwa | Kto publikuje |
| --- | --- |
| `learning.technology-published.v1` | Learning |
| `learning.course-published.v1` | Learning |
| `learning.lesson-published.v1` | Learning |
| `learning.lesson-revised.v1` | Learning |
| `learning.integration-state.v1` | Learning, na temat kompaktowany |
| `news.article-created.v1` | News |
| `news.article-revised.v1` | News |
| `news.article-published.v1` | News |
| `identity.registration-requested.v1` | Identity |
| `learning.enrollment-confirmed.v1` | Learning |
| `learning.enrollment-rejected.v1` | Learning |
| `contact.student-inquiry-received.v1` | Contact |
| `contact.recruiter-inquiry-received.v1` | Contact |

Nagłówki wiadomości: `message-id`, `message-type`, `schema-version`, `correlation-id`, `traceparent`, `producer`.

Grupy konsumentów: `<usługa>-projector`, a dla Contact dodatkowo `contact-sse` i `contact-push`.

## HTTP

Brama jest jedynym adresem klientów. Trasy:

| Ścieżka | Cel |
| --- | --- |
| `/versions` | brama |
| `/health/live`, `/health/ready` | proces, który dostał żądanie |
| `/connect/` i `/.well-known/` | Identity |
| `/identity/` | Identity |
| `/learning/` | Learning |
| `/news/` | News |
| `/cv/` | Cv |
| `/contact/` | Contact |
| `/graphql` | Learning |

`GET /versions` zwraca sześć pól: `identity`, `news`, `learning`, `cv`, `contact`, `gateway`. Brak wdrożenia to pusty napis, nie pominięte pole.

Nagłówek `X-Correlation-Id` jest przyjmowany albo generowany. Zapis wersjonowany wymaga `If-Match`. Tworzenie, które nie może się powtórzyć, wymaga `Idempotency-Key`.

Mapowanie rodzaju `Result` na status jest w [architektura.md](architektura.md).

## Ekrany

Język publiczny jest pierwszym segmentem: `pl` albo `en`. `/login`, `/register` i `/intranet/` tego segmentu nie mają. Zły język wraca na `/pl`.

| Adres | Ekran |
| --- | --- |
| `/:lang` | skrót CV i ostatnie notatki |
| `/:lang/cv` | publiczne CV |
| `/:lang/technologies/:slug` | technologia i jej kursy |
| `/:lang/courses` | lista kursów |
| `/:lang/courses/:slug` | spis lekcji |
| `/:lang/courses/:slug/:lesson` | lekcja |
| `/:lang/notes` | lista notatek |
| `/:lang/notes/:slug` | notatka |
| `/:lang/contact` | formularz rekrutera, bez konta |
| `/login`, `/register` | gość |
| `/:lang/student/contact` | formularz studenta |
| `/intranet` | pulpit właściciela |
| `/intranet/cv` | edytor CV |
| `/intranet/news` | notatki |
| `/intranet/courses` | kursy |
| `/intranet/contact/students` | skrzynka studentów |
| `/intranet/contact/recruiters` | skrzynka rekruterów |

Telefon ma ekrany o tych samych nazwach rzeczy: `HomeScreen`, `CvScreen`, `TechnologyScreen`, `CoursesScreen`, `CourseScreen`, `LessonScreen`, `NotesScreen`, `NoteScreen`, `StudentContactScreen`, `LoginScreen`, `RegisterScreen`, `AboutScreen`, `StudentInboxScreen`, `RecruiterInboxScreen`. Edytory zostają w intranecie webu.

Stopka webu pokazuje `web-vX.Y.Z`. Ekran O aplikacji pokazuje `mobile-vX.Y.Z`. Pod spodem jest sześć wierszy z `GET /versions`.

## Klucze chromu

Oba pliki `pl.json` i `en.json` mają ten sam zestaw kluczy. Dopisanie klucza w jednym pliku bez drugiego jest błędem testu.

`nav.home`, `nav.cv`, `nav.courses`, `nav.notes`, `nav.contact`, `nav.login`, `nav.register`, `nav.logout`, `nav.intranet`, `nav.about`, `lang.pl`, `lang.en`, `footer.versionLabel`, `version.identity`, `version.news`, `version.learning`, `version.cv`, `version.contact`, `version.gateway`, `version.missing`, `auth.email`, `auth.password`, `auth.invite`, `auth.submitLogin`, `auth.submitRegister`, `auth.error`, `contact.name`, `contact.email`, `contact.message`, `contact.submit`, `contact.recruiter.title`, `contact.recruiter.success`, `contact.student.title`, `contact.student.success`, `contact.student.forbidden`, `inbox.students.title`, `inbox.recruiters.title`, `inbox.empty`, `about.title`, `empty.list`, `error.load`, `error.notFound`, `visibility.public`, `visibility.students`, `visibility.private`, `editor.save`, `editor.conflict`, `editor.reload`, `lesson.complete`.

## Wygląd

Paleta jasna: tło `#f3efe6`, atrament `#1a1814`, akcent `#9a3412`. Krój to Inter. Heks stoi tylko w `tokens.ts`. Klasy Tailwinda i NativeWind nazywają te liczby: `bg-canvas`, `text-ink`, `bg-accent`. Drugiej palety nie ma.

`TrustedHtml` jest jedynym miejscem, w którym HTML z API wchodzi do DOM. `dangerouslySetInnerHTML` występuje raz, w tym komponencie.

## Testy

| Cecha | Gdzie leci |
| --- | --- |
| brak | `unit-tests` |
| `Category=Integration` | `integration-tests` |
| `Category=Performance` | `performance` |
| `Category=Contract` | `integration-tests`, bo potrzebuje procesu dostawcy |

Kontener PostgreSQL w teście to `postgres:18`. Kafka w teście idzie przez `Testcontainers.Kafka` w trybie KRaft.

## Maszyna

| Nazwa | Znaczenie |
| --- | --- |
| `jjdevhub` | etykieta runnera deployu |
| `compose` | nazwa środowiska Docker Compose w AppHost |
| `/etc/jjdevhub/compose.env` | zmienne maszyny do czasu OpenBao, poza gitem |
| `gateway` | Service albo nazwa kontenera bramy, port 8080 |
| namespace `jjdevhub` | obciążenie aplikacji na k3s |
