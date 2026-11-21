# Technologie

Ten plik jest rejestrem decyzji. Numer, który dodaje pakiet, dopisuje tu wiersz w tym samym pull requeście. Wersja ląduje w `Directory.Packages.props`. Projekt używa `PackageReference` bez własnego numeru wersji.

Pakiet wolno dodać, gdy licencja jest otwarta i darmowa także produkcyjnie: MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, PostgreSQL. OpenBao jest serwerem na MPL-2.0 i jest wyjątkiem zapisanym w tabeli, nie wzorem dla kolejnych serwerów. Starszej wersji nie przypina się tylko po to, żeby ominąć zmianę licencji.

## Platforma

| Decyzja | Wybór | Co odpadło |
| --- | --- | --- |
| Framework | `net10.0`, SDK przypięty w `global.json` | osobny target na usługę |
| Skład lokalny | .NET Aspire 13, pakiety `13.6.5` | ręczny compose jako jedyne źródło lokalnego stosu |
| Publikacja compose | `Aspire.Hosting.Docker` i `aspire publish` | pisany ręcznie plik, który rozjeżdża się z AppHostem |
| Obraz aplikacji | jeden Dockerfile z `ARG PROJECT` | Dockerfile na usługę |
| Baza | PostgreSQL 18, obraz `postgres:18` | SQL Server, MongoDB |
| Zapis stanu | EF Core 10 i `Npgsql.EntityFrameworkCore.PostgreSQL` | Dapper na ścieżce komendy |
| Odczyt | Dapper | LINQ na ścieżce `GET` listy |
| Szyna | `apache/kafka:4.4.0` w trybie KRaft, klient `Confluent.Kafka` 2.16.0 | RabbitMQ, obraz Confluent |
| Tożsamość | OpenIddict 7.8.0 | Duende, Keycloak, ręcznie składany JWT jako docelowy mechanizm |
| Brama | YARP 2.3.0 | druga brama obok YARP, Traefik jako brama aplikacji |
| Telemetria | OpenTelemetry, na maszynie `grafana/otel-lgtm` | osobny Jaeger i osobny Prometheus jako pierwszy skład |
| Odporność | `Microsoft.Extensions.Http.Resilience` i Polly 8 | własna pętla retry |
| Dokumentacja HTTP | OpenAPI i Scalar | Swagger UI |
| GraphQL | HotChocolate 16 | drugi endpoint GraphQL w bramie |
| Asystent | `Microsoft.Extensions.AI` | model wołany z przeglądarki z kluczem w kliencie |
| Test jednostkowy | xunit.v3, NSubstitute, Shouldly, ArchUnitNET | drugi runner |
| Test integracyjny | Testcontainers, `Aspire.Hosting.Testing` | wspólna baza na laptopie dewelopera |
| Kontrakt | PactNet 5, MIT | współdzielona baza testowa zamiast kontraktu |
| Obciążenie | harness w repozytorium na `HttpClient` | NBomber, k6 |
| Web | React 19, Vite, TypeScript, TanStack Query, Tailwind CSS 4 | drugi framework UI |
| Telefon | Expo i React Native, NativeWind | osobny klient natywny |
| Pakiety JS | pnpm | drugi lockfile w kliencie |
| Klaster | k3s, manifesty Kustomize, klaster ćwiczebny kind | Helm jako jedyne źródło manifestu |
| Maszyna jako kod | Terraform | klikany Proxmox jako jedyny opis |
| Sekrety docelowe | OpenBao | HashiCorp Vault |
| Cache docelowy | Redis, prefiks nazwy usługi | Redis jako baza odczytu |
| Rejestr obrazów | GHCR | rejestr na laptopie |
| CI | GitHub Actions na `ubuntu-latest` | runner własny na pull requeście |

Aspire 13.6.5 jest linią pakietów hostingu: `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.Docker`, `Aspire.AppHost.Sdk`. `Aspire.Hosting.Kafka` nie wchodzi. Jego domyślny obraz to `confluentinc/confluent-local`, a licencja tego obrazu nie jest na liście dozwolonych. Broker jest zwykłym kontenerem `apache/kafka:4.4.0`, z topicami tworzonymi przez kontener jednorazowy. Klient `Confluent.Kafka` zostaje, bo sam pakiet jest na Apache-2.0.

NBomber od linii 5 nie jest wolny produkcyjnie. Linii 4 nie przypinamy. k6 jest na AGPL i też nie wchodzi. Job `performance` uruchamia projekt `tests/JJDevHub.Performance.Tests`: stała liczba żądań, percentyl i próg. Próg jest w numerze testów wydajnościowych.

## Pakiety .NET

Wersje poniżej są pierwszym pinem. Podbicie idzie przez Dependabota i przechodzi te same checki co zmiana kodu.

| Pakiet | Wersja | Licencja | Wchodzi |
| --- | --- | --- | --- |
| `Aspire.Hosting.PostgreSQL` | 13.6.5 | MIT | bazy |
| `Aspire.Hosting.Docker` | 13.6.5 | MIT | compose |
| `Aspire.AppHost.Sdk` | 13.6.5 | MIT | szkielet |
| `OpenTelemetry.Extensions.Hosting` | 1.20.1 | Apache-2.0 | szkielet |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.20.1 | Apache-2.0 | szkielet |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.20.1 | Apache-2.0 | szkielet |
| `OpenTelemetry.Instrumentation.Http` | 1.20.1 | Apache-2.0 | szkielet |
| `OpenTelemetry.Instrumentation.Runtime` | 1.20.1 | Apache-2.0 | szkielet |
| `Microsoft.Extensions.Http.Resilience` | 10.11.0 | MIT | szkielet |
| `Microsoft.Extensions.ServiceDiscovery` | 10.11.0 | MIT | szkielet |
| `Yarp.ReverseProxy` | 2.3.0 | MIT | brama |
| `Microsoft.AspNetCore.OpenApi` | 10.0.14 | MIT | REST |
| `Scalar.AspNetCore` | 2.19.0 | MIT | REST |
| `Microsoft.EntityFrameworkCore` | 10.0.14 | MIT | zapis |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | PostgreSQL | zapis |
| `EFCore.NamingConventions` | 10.0.1 | MIT | zapis |
| `Npgsql` | 10.0.3 | PostgreSQL | odczyt i test łączności |
| `Dapper` | 2.1.89 | Apache-2.0 | odczyt |
| `Confluent.Kafka` | 2.16.0 | Apache-2.0 | szyna |
| `OpenIddict.AspNetCore` | 7.8.0 | Apache-2.0 | tożsamość |
| `OpenIddict.EntityFrameworkCore` | 7.8.0 | Apache-2.0 | tożsamość |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.14 | MIT | usługi |
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 | Apache-2.0 | probe |
| `AspNetCore.HealthChecks.Kafka` | 9.0.0 | Apache-2.0 | probe |
| `HotChocolate.AspNetCore` | 16.7.3 | MIT | GraphQL |
| `HotChocolate.Data` | 16.7.3 | MIT | GraphQL |
| `Microsoft.Extensions.AI` | 10.11.0 | MIT | asystent |
| `Microsoft.Extensions.AI.OpenAI` | 10.11.0 | MIT | asystent |
| `Markdig` | pin przy numerze notatki | BSD-2-Clause | Markdown |
| `HtmlSanitizer` | pin przy numerze notatki | MIT | sanitacja |
| `xunit.v3` | 4.0.2 | Apache-2.0 | testy |
| `xunit.runner.visualstudio` | 4.0.1 | Apache-2.0 | testy |
| `NSubstitute` | 6.2.0 | MIT | testy |
| `Shouldly` | 4.3.0 | MIT | testy |
| `Testcontainers.PostgreSql` | 4.16.0 | MIT | testy |
| `Testcontainers.Kafka` | 4.16.0 | MIT | testy |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.14 | MIT | testy |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.11.0 | MIT | testy |
| `TngTech.ArchUnitNET.xUnitV3` | 0.13.5 | Apache-2.0 | testy |
| `PactNet` | 5.0.1 | MIT | kontrakt |
| `Aspire.Hosting.Redis` | 13.6.5 | MIT | cache, faza klastra |
| `Aspire.StackExchange.Redis` | z linii 13.6.5 | MIT | cache, faza klastra |

`Microsoft.Extensions.ServiceDiscovery` stoi na `10.11.0`, tej samej poprawce co `Microsoft.Extensions.Http.Resilience`. Wyszukanie w dniu numeru szkieletu pokazuje też `11.0.0`; to już następna linia i ten numer jej nie bierze. Klient Redisa nadal czeka na numer cache i bierze wersję, jaką wskaże `dotnet package search` tego dnia, z linii 13.6.5. Nie wpisuje się jej z pamięci.

Serwery, które nie są pakietem NuGet:

| Obraz | Licencja | Rola |
| --- | --- | --- |
| `postgres:18` | PostgreSQL | dziesięć baz |
| `apache/kafka:4.4.0` | Apache-2.0 | broker |
| `grafana/otel-lgtm` | AGPL-3.0 dla Grafany, reszta stosu według obrazu | telemetria na maszynie i na laptopie |
| `openbao/openbao` | MPL-2.0 | sekrety w fazie klastra |
| `sonarqube:community` | LGPL-3.0 | bramka jakości, nie jest biblioteką aplikacji |

Grafana i SonarQube stoją obok aplikacji. Ich licencja nie wchodzi do binarki usługi. Nie linkujemy kodu Grafany. Obraz jest uruchamiany jako osobny proces.

## Pakiety frontu

| Pakiet | Licencja | Wchodzi |
| --- | --- | --- |
| `react`, `react-dom` | MIT | web |
| `react-router` | MIT | web |
| `vite`, `@vitejs/plugin-react` | MIT | web |
| `typescript` | Apache-2.0 | web i telefon |
| `vitest`, `jsdom` | MIT | test widoku |
| `@testing-library/react` | MIT | test widoku |
| `tailwindcss`, `@tailwindcss/vite` | MIT | web |
| `@tanstack/react-query` | MIT | odczyt |
| `openapi-fetch`, `openapi-typescript` | MIT | klient |
| `dompurify` | Apache-2.0 albo MPL-2.0; bierzemy wydanie Apache-2.0 | drugie czyszczenie HTML |
| `motion` | MIT | ruch webu |
| `expo` | MIT | telefon |
| `nativewind` | MIT | klasy telefonu |
| `react-native-reanimated` | MIT | ruch telefonu |
| `expo-notifications` | MIT | token urządzenia |
| `@playwright/test` | Apache-2.0 | przeglądarka |

Wersję JS przypina lockfile pnpm w numerze, który pakiet dodaje.

## Odrzucone

| Odrzucone | Powód | Zamiast tego |
| --- | --- | --- |
| MediatR 13 i nowsze | licencja produkcyjna nie jest wolna | handler z jedną metodą `HandleAsync`, rejestracja w DI |
| AutoMapper 15 i nowsze | licencja produkcyjna nie jest wolna | mapowanie ręczne |
| MassTransit 9 i nowsze | licencja produkcyjna nie jest wolna | własny outbox, relay i `Confluent.Kafka` |
| Duende IdentityServer | licencja produkcyjna nie jest wolna | OpenIddict |
| QuestPDF | próg licencji społecznościowej | ten produkt nie składa PDF |
| HashiCorp Vault | licencja BSL | OpenBao |
| Keycloak | drugi produkt kont | `Account`, `Invite`, OpenIddict |
| MongoDB | SSPL i drugi silnik | projekcja w PostgreSQL |
| RabbitMQ | brak kompaktowanego tematu | Kafka |
| Obraz `confluentinc/confluent-local` | licencja obrazu spoza listy | `apache/kafka` |
| NBomber 5 i nowsze | licencja biznesowa | harness `HttpClient` |
| NBomber 4 | przypięcie starej linii, żeby ominąć licencję | to samo |
| k6 | AGPL jako narzędzie bramki | to samo |
| Jaeger jako osobny skład | drugi magazyn śladów obok LGTM | Tempo w `otel-lgtm` |
| Bicep jako opis Azure | drugi język IaC obok Terraforma | Terraform także dla wariantu Azure |
| Helm jako źródło manifestu | szablon zasłania obiekt, którego numer uczy | Kustomize, base i overlay |

## Runtime

Laptop: `aspire run`. CI pull requestu: `ubuntu-latest`, bez sekretu aplikacji. Test, który potrzebuje bazy, podnosi kontener sam.

Maszyna: jedna VM, 4 vCPU, 16 GiB bez balonowania, dysk 120 GiB, UEFI i vTPM. Do fazy klastra jest na niej Docker i compose. Potem k3s bez Traefika i bez ServiceLB, bo bramą HTTP zostaje YARP. Publiczny hostname kończy się na tunelu Cloudflare w trybie Full. Rekord A na adres domu nie powstaje.

Azure: katalog `deploy/terraform/azure`, uruchamiany ręcznie. AKS, ACR, PostgreSQL Flexible Server, Key Vault, Event Hubs z punktem końcowym Kafka. Wejście z GitHub Actions przez OIDC, bez hasła aplikacji w Entra ID. Ten wariant nie jest celem domyślnym i nie działa równolegle z maszyną na tym samym tagu bez świadomej decyzji.

## Czego ten plik nie rozstrzyga

Kształt agregatu, nazwa topicu i adres ekranu są w słowniku i w numerze, który je wprowadza. Ten plik mówi, jakim narzędziem wolno je zbudować.
