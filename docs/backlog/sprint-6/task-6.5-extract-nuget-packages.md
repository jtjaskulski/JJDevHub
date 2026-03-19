# Task 6.5: Ekstrakcja Reusable Bibliotek do Wlasnych Pakietow NuGet

| Pole | Wartosc |
|------|---------|
| Sprint | 6 - Observability & DevOps |
| Status | TODO |
| Priorytet | High |
| Estymacja | 13 story points |
| Powiazane pliki | `src/Services/JJDevHub.Shared.Kernel/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Infrastructure/Messaging/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Application/Behaviors/`, `src/Services/JJDevHub.Content/JJDevHub.Content.Api/Middleware/` |

## Opis

W projekcie JJDevHub istnieje kilka w pelni generycznych komponentow, ktore nie sa powiazane z domena biznesowa i moga byc wyekstrahowane jako samodzielne pakiety NuGet. Dzieki temu mozna je ponownie uzywac w przyszlych projektach bez kopiowania kodu. Plan obejmuje 4 pakiety NuGet, od DDD building blocks po Kafka event bus.

### Analiza kodu - co jest generyczne vs domenowe

| Komponent | Generyczny | Domenowy | Kandydat na NuGet |
|-----------|:----------:|:--------:|:------------------:|
| Shared.Kernel/BuildingBlocks (Entity, AggregateRoot, ValueObject, etc.) | 100% | 0% | Tak |
| Shared.Kernel/CQRS (ICommand, IQuery, handlery) | 100% | 0% | Tak |
| Shared.Kernel/Messaging (IEventBus, IntegrationEvent) | 100% | 0% | Tak |
| Shared.Kernel/Exceptions (DomainException) | 100% | 0% | Tak |
| ValidationBehavior (MediatR + FluentValidation) | 100% | 0% | Tak |
| ExceptionHandlingMiddleware | 95% | 5% | Tak (zalezy od DomainException) |
| KafkaEventBus | 100% | 0% | Tak |
| MongoWorkExperienceReadStore | 0% | 100% | Nie |
| WorkExperienceDocument | 0% | 100% | Nie |
| ContentDbContext | 0% | 100% | Nie |

---

## Pakiety do stworzenia

### Pakiet 1: `JJ.BuildingBlocks`

**Cel:** DDD building blocks + CQRS abstrakcje + messaging kontrakty + bazowe wyjatki. Fundament kazdego projektu DDD/CQRS.

**Zrodlo:** Caly `JJDevHub.Shared.Kernel`

**Zawartoscz:**

```
JJ.BuildingBlocks/
├── BuildingBlocks/
│   ├── Entity.cs                    # Bazowa encja z Guid Id, equality
│   ├── AuditableEntity.cs           # IsActive, CreatedDate, ModifiedDate, soft delete
│   ├── AggregateRoot.cs             # Entity + DomainEvents collection
│   ├── AuditableAggregateRoot.cs    # AuditableEntity + DomainEvents
│   ├── IAggregateRoot.cs            # Interfejs marker
│   ├── ValueObject.cs               # Structural equality przez GetEqualityComponents()
│   ├── IRepository.cs               # IRepository<T> where T : Entity, IAggregateRoot
│   ├── IUnitOfWork.cs               # SaveChangesAsync
│   ├── IDomainEvent.cs              # Id, OccurredOn : MediatR.INotification
│   └── DomainEventBase.cs           # Bazowy record
├── CQRS/
│   ├── ICommand.cs                  # ICommand<TResponse> : IRequest<TResponse>
│   ├── ICommandHandler.cs           # ICommandHandler<TCommand, TResponse>
│   ├── IQuery.cs                    # IQuery<TResponse> : IRequest<TResponse>
│   └── IQueryHandler.cs             # IQueryHandler<TQuery, TResponse>
├── Messaging/
│   ├── IEventBus.cs                 # PublishAsync<T>(T) where T : IntegrationEvent
│   └── IntegrationEvent.cs          # Bazowy record z Id, OccurredOn
└── Exceptions/
    └── DomainException.cs           # Bazowy wyjatek domenowy
```

**Zaleznosci NuGet:** `MediatR` (jedyna)

**Uzycie w przyszlym projekcie:**
```csharp
dotnet add package JJ.BuildingBlocks

// W kodzie:
public class Order : AuditableAggregateRoot { ... }
public class Money : ValueObject { ... }
public class CreateOrderCommand : ICommand<Guid> { ... }
```

---

### Pakiet 2: `JJ.BuildingBlocks.MediatR`

**Cel:** Gotowe pipeline behaviors dla MediatR: walidacja FluentValidation, (opcjonalnie) logging, performance tracking. Plug-and-play do kazdego projektu CQRS.

**Zrodlo:** `ValidationBehavior` z Content.Application + nowe behaviors

**Zawartosc:**

```
JJ.BuildingBlocks.MediatR/
├── Behaviors/
│   ├── ValidationBehavior.cs        # FluentValidation w MediatR pipeline
│   ├── LoggingBehavior.cs           # [NOWY] Logowanie request/response + czas wykonania
│   └── PerformanceBehavior.cs       # [NOWY] Warning jesli handler > 500ms
└── Extensions/
    └── ServiceCollectionExtensions.cs  # AddBuildingBlocksBehaviors() - rejestracja wszystkich
```

**Istniejacy kod do ekstrakcji - `ValidationBehavior`:**
```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count != 0) throw new ValidationException(failures);
        return await next();
    }
}
```

**Nowy kod - `LoggingBehavior`:**
```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
        return response;
    }
}
```

**Zaleznosci NuGet:** `MediatR`, `FluentValidation`, `Microsoft.Extensions.Logging.Abstractions`

**Uzycie:**
```csharp
dotnet add package JJ.BuildingBlocks.MediatR

services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
services.AddBuildingBlocksBehaviors(); // rejestruje ValidationBehavior, LoggingBehavior, etc.
```

---

### Pakiet 3: `JJ.BuildingBlocks.AspNetCore`

**Cel:** Middleware do obslugi wyjatkow w ASP.NET Core Minimal API. Mapuje DomainException, ValidationException, KeyNotFoundException na odpowiednie HTTP status codes. Gotowy do uzycia w kazdym API.

**Zrodlo:** `ExceptionHandlingMiddleware` z Content.Api

**Zawartosc:**

```
JJ.BuildingBlocks.AspNetCore/
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs   # Mapowanie wyjatkow na HTTP codes
├── Models/
│   └── ErrorResponse.cs                # Standardowy model odpowiedzi blednej
└── Extensions/
    └── ApplicationBuilderExtensions.cs  # app.UseBuildingBlocksExceptionHandling()
```

**Kod do ekstrakcji (z generalizacja):**
```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex) { await HandleExceptionAsync(context, ex); }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("Validation failed",
                    validationEx.Errors.Select(e => e.ErrorMessage).ToArray())),

            DomainException domainEx => (
                HttpStatusCode.UnprocessableEntity,
                new ErrorResponse(domainEx.Message)),

            KeyNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ErrorResponse(notFoundEx.Message)),

            _ => (HttpStatusCode.InternalServerError,
                new ErrorResponse("An unexpected error occurred."))
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
```

**Zaleznosci NuGet:** `JJ.BuildingBlocks` (dla DomainException), `FluentValidation`, `Microsoft.AspNetCore.Http.Abstractions`

**Uzycie:**
```csharp
dotnet add package JJ.BuildingBlocks.AspNetCore

app.UseBuildingBlocksExceptionHandling();
```

---

### Pakiet 4: `JJ.Messaging.Kafka`

**Cel:** Gotowa implementacja event bus na Apache Kafka. Implementuje `IEventBus` z `JJ.BuildingBlocks`. Producer z idempotencja i Acks.All. Plug-and-play do kazdego projektu event-driven.

**Zrodlo:** `KafkaEventBus` z Content.Infrastructure

**Zawartosc:**

```
JJ.Messaging.Kafka/
├── KafkaEventBus.cs                    # IEventBus implementation (producer)
├── KafkaConsumerService.cs             # [NOWY] Generic BackgroundService consumer
├── Configuration/
│   └── KafkaOptions.cs                 # BootstrapServers, GroupId, etc.
└── Extensions/
    └── ServiceCollectionExtensions.cs  # services.AddKafkaEventBus(config)
                                        # services.AddKafkaConsumer<THandler>(config)
```

**Istniejacy kod - Producer:**
```csharp
public class KafkaEventBus : IEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventBus> _logger;

    public KafkaEventBus(IConfiguration configuration, ILogger<KafkaEventBus> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:29092",
            Acks = Acks.All,
            EnableIdempotence = true
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T integrationEvent) where T : IntegrationEvent
    {
        var topicName = typeof(T).Name;
        var message = new Message<string, string>
        {
            Key = integrationEvent.Id.ToString(),
            Value = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType())
        };
        var result = await _producer.ProduceAsync(topicName, message);
        _logger.LogInformation("Published {EventType} to {Topic} [{Partition}:{Offset}]",
            topicName, result.Topic, result.Partition.Value, result.Offset.Value);
    }

    public void Dispose() { _producer.Flush(TimeSpan.FromSeconds(5)); _producer.Dispose(); }
}
```

**Nowy kod - Generic Consumer:**
```csharp
public abstract class KafkaConsumerService<TEvent> : BackgroundService
    where TEvent : IntegrationEvent
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger _logger;

    protected KafkaConsumerService(IConfiguration config, ILogger logger)
    {
        _logger = logger;
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:29092",
            GroupId = config["Kafka:GroupId"] ?? "default-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _consumer.Subscribe(typeof(TEvent).Name);
    }

    protected abstract Task HandleAsync(TEvent @event, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                var @event = JsonSerializer.Deserialize<TEvent>(result.Message.Value)!;
                await HandleAsync(@event, stoppingToken);
                _consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
        }
        _consumer.Close();
    }
}
```

**Zaleznosci NuGet:** `JJ.BuildingBlocks` (dla IEventBus, IntegrationEvent), `Confluent.Kafka`, `Microsoft.Extensions.Hosting`

**Uzycie:**
```csharp
dotnet add package JJ.Messaging.Kafka

// Producer:
services.AddKafkaEventBus(configuration);

// Consumer:
public class OrderCreatedConsumer : KafkaConsumerService<OrderCreatedEvent>
{
    protected override async Task HandleAsync(OrderCreatedEvent e, CancellationToken ct)
    {
        // handle event
    }
}
services.AddHostedService<OrderCreatedConsumer>();
```

---

## Hierarchia pakietow

```
JJ.BuildingBlocks                          (zero dependencies poza MediatR)
├── JJ.BuildingBlocks.MediatR              (depends on: JJ.BuildingBlocks, FluentValidation)
├── JJ.BuildingBlocks.AspNetCore           (depends on: JJ.BuildingBlocks, FluentValidation)
└── JJ.Messaging.Kafka                     (depends on: JJ.BuildingBlocks, Confluent.Kafka)
```

**Kluczowe:** `JJ.BuildingBlocks` jest jedynym "core" pakietem. Pozostale rozszerzaja go o konkretne implementacje. Mozna uzyc samego `JJ.BuildingBlocks` bez Kafka czy AspNetCore.

---

## Kryteria akceptacji

- [ ] Nowe solution `JJ.BuildingBlocks.sln` (lub foldery w JJDevHub)
- [ ] Projekt `JJ.BuildingBlocks` z calym kodem z Shared.Kernel
- [ ] Projekt `JJ.BuildingBlocks.MediatR` z ValidationBehavior, LoggingBehavior, PerformanceBehavior
- [ ] Projekt `JJ.BuildingBlocks.AspNetCore` z ExceptionHandlingMiddleware
- [ ] Projekt `JJ.Messaging.Kafka` z KafkaEventBus i KafkaConsumerService
- [ ] Kazdy projekt ma `.csproj` z metadata NuGet (PackageId, Version, Description, Authors, License)
- [ ] README.md per pakiet z przykladami uzycia
- [ ] Testy jednostkowe per pakiet
- [ ] `dotnet pack` produkuje poprawne `.nupkg` pliki
- [ ] Pakiety opublikowane na NuGet.org lub prywatnym feedzie (GitHub Packages)
- [ ] JJDevHub zmieniony na konsumenta tych pakietow (zamiast ProjectReference)

## Kroki implementacji

1. **Stworz nowy folder/solution dla pakietow:**
   ```
   libs/
   ├── JJ.BuildingBlocks/
   │   ├── JJ.BuildingBlocks.csproj
   │   └── (kod z Shared.Kernel)
   ├── JJ.BuildingBlocks.MediatR/
   │   ├── JJ.BuildingBlocks.MediatR.csproj
   │   └── (ValidationBehavior + nowe behaviors)
   ├── JJ.BuildingBlocks.AspNetCore/
   │   ├── JJ.BuildingBlocks.AspNetCore.csproj
   │   └── (ExceptionHandlingMiddleware)
   └── JJ.Messaging.Kafka/
       ├── JJ.Messaging.Kafka.csproj
       └── (KafkaEventBus + KafkaConsumerService)
   ```

2. **Skonfiguruj .csproj z metadata NuGet:**
   ```xml
   <PropertyGroup>
     <PackageId>JJ.BuildingBlocks</PackageId>
     <Version>1.0.0</Version>
     <Authors>JJ</Authors>
     <Description>DDD Building Blocks, CQRS interfaces, and Event-Driven messaging abstractions for .NET</Description>
     <PackageLicenseExpression>MIT</PackageLicenseExpression>
     <PackageTags>ddd;cqrs;event-driven;building-blocks;domain-driven-design</PackageTags>
     <RepositoryUrl>https://github.com/...</RepositoryUrl>
     <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
   </PropertyGroup>
   ```

3. **Przenieś kod z Shared.Kernel do JJ.BuildingBlocks:**
   - Zmien namespace z `JJDevHub.Shared.Kernel` na `JJ.BuildingBlocks`
   - Zachowaj identyczna strukture folderow

4. **Stworz extension methods dla latwej rejestracji:**
   ```csharp
   // JJ.BuildingBlocks.MediatR
   public static IServiceCollection AddBuildingBlocksBehaviors(this IServiceCollection services)
   {
       services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
       services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
       return services;
   }

   // JJ.BuildingBlocks.AspNetCore
   public static IApplicationBuilder UseBuildingBlocksExceptionHandling(this IApplicationBuilder app)
       => app.UseMiddleware<ExceptionHandlingMiddleware>();

   // JJ.Messaging.Kafka
   public static IServiceCollection AddKafkaEventBus(this IServiceCollection services, IConfiguration config)
   {
       services.AddSingleton<IEventBus, KafkaEventBus>();
       return services;
   }
   ```

5. **Napisz testy jednostkowe:**
   - `JJ.BuildingBlocks.Tests` - Entity equality, ValueObject equality, AggregateRoot domain events
   - `JJ.BuildingBlocks.MediatR.Tests` - ValidationBehavior z FluentValidation mock
   - `JJ.BuildingBlocks.AspNetCore.Tests` - ExceptionHandlingMiddleware z roznymi wyjatkami
   - `JJ.Messaging.Kafka.Tests` - KafkaEventBus z mock producer

6. **Spakuj i opublikuj:**
   ```bash
   dotnet pack --configuration Release
   dotnet nuget push *.nupkg --source https://api.nuget.org/v3/index.json --api-key $KEY
   ```

7. **Zmien JJDevHub na konsumenta:**
   - Usun ProjectReference do Shared.Kernel
   - Dodaj PackageReference do `JJ.BuildingBlocks`
   - Zaktualizuj namespaces w importach

## Opcje publikacji

| Zrodlo | Cena | Prywatnosc | Uzycie |
|--------|------|------------|--------|
| **NuGet.org** | Darmowy | Publiczne | `dotnet add package JJ.BuildingBlocks` |
| **GitHub Packages** | Darmowy (publiczne repo) | Publiczne/Prywatne | Wymaga auth w `nuget.config` |
| **Azure Artifacts** | Darmowy (do 2 GB) | Prywatne | Dobra integracja z Azure DevOps |
| **Lokalny folder** | Darmowy | Lokalne | `dotnet nuget add source ./packages` |

**Rekomendacja:** Start z GitHub Packages (prywatne, darmowe, zintegrowane z repo). Pozniej migracja na NuGet.org jesli pakiety beda publiczne.

## Zaleznosci

- **Wymaga:** Task 1.3 (Shared.Kernel musi byc stabilny)
- **Blokuje:** Nic (ale po ekstrakcji JJDevHub staje sie konsumentem)

## Notatki techniczne

- Zmiana namespace z `JJDevHub.Shared.Kernel` na `JJ.BuildingBlocks` wymaga aktualizacji importow we wszystkich projektach JJDevHub.
- Semantic Versioning (SemVer): `1.0.0` na start, `1.1.0` dla nowych features, `2.0.0` dla breaking changes.
- `GeneratePackageOnBuild = true` w .csproj oznacza ze `dotnet build` automatycznie tworzy `.nupkg`.
- Mozna uzyc `dotnet pack --include-symbols --include-source` aby dolaczac symbole debugowania do pakietu (Source Link).
- XML documentation comments (`///`) na publicznych API sa automatycznie dolaczane do NuGet i widoczne w IntelliSense konsumentow.
- Na przyszlosc: CI/CD pipeline w Jenkins/GitHub Actions automatycznie publikujacy nowe wersje po merge do main.
