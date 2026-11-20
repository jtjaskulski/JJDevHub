# Mapa technologii

Ten plik jest jedynym miejscem specyfikacji, które wskazuje rozdziały w `gigatutorial/app/docs`. Rozdział jest źródłem wzorca: kształtu klas, kolejności warstw i pytań, które numer ma zostawić wyjaśnione. Domena ogrzewania z tamtych rozdziałów nie wchodzi do JJDevHub. Nazwy produktu są w [slownik.md](slownik.md).

Kolumna „Obszar” zbiera punkt z mapy kompetencji .NET: język, API, dane, DevOps, inżynieria, testy, bezpieczeństwo, obserwowalność, chmura, frontend. Punkt, którego produkt świadomie nie buduje, wskazuje [technologie.md](technologie.md), a nie pusty numer.

## Rozdziały

| Rozdział | Numer JJDevHub | Co z rozdziału zostaje |
| --- | --- | --- |
| `02-srodowisko-i-solucja.md` | [01-solucja](../01-bramki/01-solucja.md) | `global.json`, centralne wersje, `Directory.Build.props` |
| `dzien-1/01-dotnet-platforma.md` | [01-solucja](../01-bramki/01-solucja.md), [05-rest](../03-learning-zapis/05-rest.md) | Generic Host, Minimal API, OpenAPI, Scalar |
| `dzien-1/02-architektura-heksagonalna.md` | [01-kernel](../03-learning-zapis/01-kernel-i-domena.md), [02-porty](../03-learning-zapis/02-use-case-i-porty.md) | port, adapter, kierunek referencji |
| `dzien-1/03-csharp-async-i-watki.md` | [04-relay](../04-zdarzenia/04-relay.md), [05-projektor](../04-zdarzenia/05-projektor-i-inbox.md) | `CancellationToken`, jedna pętla konsumenta, brak `lock` przez `await` |
| `dzien-1/04-csharp-pamiec-i-typy.md` | [01-kernel](../03-learning-zapis/01-kernel-i-domena.md), [01-kontrakty](../04-zdarzenia/01-kontrakty.md) | `record`, `readonly record struct`, source generator JSON |
| `dzien-1/05-postgresql.md` | [03-postgres](../03-learning-zapis/03-postgres.md) | transakcja, izolacja, indeks, `EXPLAIN` |
| `dzien-1/06-ef-core-i-dapper.md` | [04-ef](../03-learning-zapis/04-ef-core.md), [07-ef](../03-learning-zapis/07-ef-zaawansowany.md), [06-dapper](../04-zdarzenia/06-odczyt-dapper.md) | zapis EF, odczyt Dapper, interceptory, konwertery |
| `dzien-1/07-rest-api-design.md` | [05-rest](../03-learning-zapis/05-rest.md) | dojrzałość REST, `ProblemDetails`, `ETag`, idempotencja, wersja URL |
| `dzien-2/01-architektura-zdarzeniowa.md` | [01-kontrakty](../04-zdarzenia/01-kontrakty.md), [01-agregat](../06-news/01-agregat-i-zdarzenie.md) | zdarzenie domenowe kontra integracyjne, spójność ostateczna |
| `dzien-2/02-kafka-fundamenty.md` | [03-kafka](../04-zdarzenia/03-kafka.md) | partycja, klucz, offset, KRaft, kompaktowanie |
| `dzien-2/03-kafka-dotnet-producer.md` | [04-relay](../04-zdarzenia/04-relay.md) | idempotentny producent, `Acks.All` |
| `dzien-2/04-kafka-dotnet-consumer.md` | [05-projektor](../04-zdarzenia/05-projektor-i-inbox.md) | ręczny commit, DLQ |
| `dzien-2/05-outbox-inbox-relay.md` | [02-outbox](../04-zdarzenia/02-outbox.md), [05-projektor](../04-zdarzenia/05-projektor-i-inbox.md) | jeden commit, `SKIP LOCKED`, inbox |
| `dzien-2/06-saga-i-state-machine.md` | [05-saga](../05-tozsamosc/05-saga-rejestracji.md) | stany, timeout, kompensacja |
| `dzien-2/07-schema-evolution.md` | [07-ewolucja](../04-zdarzenia/07-ewolucja-schematu.md) | `v2`, upcaster, kompatybilność wstecz |
| `dzien-3/01-observability-fundamenty.md` | [08-telemetria](../02-szkielet-i-deploy/08-telemetria.md) | trzy sygnały, RED, kardynalność |
| `dzien-3/02-opentelemetry-dotnet.md` | [01-aspire](../02-szkielet-i-deploy/01-aspire.md), [08-slady](../04-zdarzenia/08-slady-i-metryki.md) | `Activity`, `Meter`, propagacja `traceparent` |
| `dzien-3/03-grafana-stack.md` | [08-telemetria](../02-szkielet-i-deploy/08-telemetria.md) | Prometheus, Tempo, Loki, jeden kontener |
| `dzien-3/04-health-i-resilience.md` | [09-health](../04-zdarzenia/09-health-i-odpornosc.md) | live/ready, Polly v8, circuit breaker |
| `dzien-3/05-security-oauth-jwt.md` | [02-openiddict](../05-tozsamosc/02-openiddict.md), [03-jwt](../05-tozsamosc/03-jwt-i-policies.md) | authorization code, PKCE, JWKS, policies |
| `dzien-3/06-audit-logging-i-compliance.md` | [04-audyt](../05-tozsamosc/04-audyt.md) | append-only, hash chain |
| `dzien-3/07-graphql-i-gateway.md` | [02-gateway](../02-szkielet-i-deploy/02-gateway.md), [01-graphql](../09-rozszerzenia-api/01-graphql.md) | YARP, Hot Chocolate, DataLoader |
| `dzien-4/01-docker.md` | [03-dockerfile](../02-szkielet-i-deploy/03-dockerfile.md) | multi-stage, cache warstw, obraz chiseled |
| `dzien-4/02-kubernetes-fundamenty.md` | [01-kind](../13-kubernetes/01-kind.md) | Pod, Deployment, Service, probe, requests |
| `dzien-4/03-kubernetes-wdrozenie.md` | [02-manifesty](../13-kubernetes/02-manifesty.md), [07-skalowanie](../13-kubernetes/07-skalowanie.md) | StatefulSet, Kustomize, HPA, PDB |
| `dzien-4/04-azure.md` | [08-azure](../13-kubernetes/08-azure.md) | AKS, ACR, Postgres, Key Vault, tożsamość zarządzana; język opisu to Terraform, nie Bicep |
| `dzien-4/05-ci-cd.md` | [foldery 01 i 02](../01-bramki/01-solucja.md) | GitHub Actions, OIDC, GHCR, Dependabot |
| `dzien-5/01-testy.md` | [03-build](../01-bramki/03-build-testy-format.md), [folder 10](../10-testy/01-integracyjne.md) | xUnit, Testcontainers, czas, kontrakt, obciążenie |
| `dzien-5/02-react-typescript.md` | [folder 11](../11-web/02-szkielet.md) | Vite, React, TanStack Query |
| `dzien-5/03-ai-dla-seniora.md` | [02-asystent](../09-rozszerzenia-api/02-asystent.md) | tool calling, walidacja argumentu, brak klucza w kliencie |
| `dzien-5/04-wzorce-i-algorytmy.md` | numery, które wzorca używają | Strategy przy widoczności, State przy sadze, Outbox, Decorator przy odporności |
| `dzien-5/05-debugging-i-diagnostyka.md` | [08-telemetria](../02-szkielet-i-deploy/08-telemetria.md), [08-slady](../04-zdarzenia/08-slady-i-metryki.md) | korelacja logu i śladu, dump zostaje narzędziem maszyny, nie osobną usługą |

Telefonu w tych rozdziałach nie ma. [Folder 12](../12-mobile/01-szkielet-expo.md) bierze ten sam kontrakt HTTP, te same tokeny PKCE i te same klucze chromu co web.

## Obszary, które nie dostają własnego modułu

| Punkt | Gdzie jest wyjaśniony |
| --- | --- |
| RabbitMQ, Azure Service Bus, MassTransit, NServiceBus | [technologie.md](technologie.md), tabela odrzuconych; protokół AMQP nie jest transportem produktu |
| FTP, SFTP, SMTP | nie ma transferu plików ani poczty; push idzie przez FCM HTTP |
| MongoDB, Cosmos, drugi silnik dokumentów | [technologie.md](technologie.md); odczyt jest w PostgreSQL |
| Bicep i CloudFormation | [08-azure](../13-kubernetes/08-azure.md) mówi, czym się różnią, i zostawia Terraform |
| Azure DevOps i Jenkins | [07-deploy](../02-szkielet-i-deploy/07-deploy.md); runner i OIDC są na GitHub Actions |
| Expression trees | [07-ef](../03-learning-zapis/07-ef-zaawansowany.md), blok o tym, jak EF tłumaczy LINQ |
| `ref`, `in`, `out`, `ref struct` | [01-kernel](../03-learning-zapis/01-kernel-i-domena.md); `Slug` jest `readonly record struct`, gorąca ścieżka nie alokuje drugiej kopii napisu bez powodu |
| Reflection kontra source generator | [01-kontrakty](../04-zdarzenia/01-kontrakty.md), `JsonSerializerContext` |
| Publikacja self-contained kontra framework-dependent | [03-dockerfile](../02-szkielet-i-deploy/03-dockerfile.md) |
| Skalowanie pionowe kontra poziome, IaaS, PaaS, SaaS | [07-skalowanie](../13-kubernetes/07-skalowanie.md) i [08-azure](../13-kubernetes/08-azure.md) |
| Testy akceptacyjne | [07-playwright](../11-web/07-playwright.md) |
| Debug: stos, PDB, zrzut pamięci | [08-slady](../04-zdarzenia/08-slady-i-metryki.md); zrzut nie jest krokiem wdrożenia |

## Jak używać mapy

Przed numerem czyta się wskazany rozdział do checkpointu, który dotyczy decyzji numeru, nie do końca dnia. Kod z rozdziału nie jest kopiowany. Kopiuje się odpowiedzialność: kto publikuje, kto commituje offset, gdzie leży inbox.

Po zamknięciu numeru wraca się do wiersza i sprawdza, czy `## Umiesz wyjaśnić` pokrywa kolumnę „Co z rozdziału zostaje”. Jeśli nie pokrywa, brakuje podsekcji w numerze, a nie kolejnego numeru.
