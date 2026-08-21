# Plan ekstrakcji pakietów NuGet (Task 6.5)

Cel: wydzielić z [`JJDevHub.Shared.Kernel`](../../src/Services/JJDevHub.Shared.Kernel/) oraz warstw infrastruktury osobne pakiety:

| Pakiet | Zakres |
|--------|--------|
| `JJ.BuildingBlocks` | Entity, AggregateRoot, ValueObject, IDomainEvent |
| `JJ.BuildingBlocks.MediatR` | ICommand, IQuery, handlery |
| `JJ.BuildingBlocks.AspNetCore` | Middleware, health |
| `JJ.Messaging.Kafka` | IEventBus, Kafka producer |

## Kroki

1. Utworzyc solution `src/Packages/` lub repo satelitarne z CI `dotnet pack`.
2. Skopiowac / przeniesc kod z Shared.Kernel z zachowaniem testow.
3. `dotnet pack -c Release` i publikacja do feedu (nuget.org lub GitHub Packages).
4. W JJDevHub zamienic `ProjectReference` na `PackageReference` z wersja SemVer.
5. Utrzymywac changelog i wersjonowanie niezalezne od aplikacji.

Stan repozytorium: **plan dokumentacyjny** — migracja kodu w osobnym PR po stabilizacji API Shared.Kernel.
