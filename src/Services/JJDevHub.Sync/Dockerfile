FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/JJDevHub.Shared.Kernel/JJDevHub.Shared.Kernel.csproj", "src/Services/JJDevHub.Shared.Kernel/"]
COPY ["src/Services/JJDevHub.Content/JJDevHub.Content.Core/JJDevHub.Content.Core.csproj", "src/Services/JJDevHub.Content/JJDevHub.Content.Core/"]
COPY ["src/Services/JJDevHub.Content/JJDevHub.Content.Application/JJDevHub.Content.Application.csproj", "src/Services/JJDevHub.Content/JJDevHub.Content.Application/"]
COPY ["src/Services/JJDevHub.Content/JJDevHub.Content.Persistence/JJDevHub.Content.Persistence.csproj", "src/Services/JJDevHub.Content/JJDevHub.Content.Persistence/"]
COPY ["src/Services/JJDevHub.Content/JJDevHub.Content.Infrastructure/JJDevHub.Content.Infrastructure.csproj", "src/Services/JJDevHub.Content/JJDevHub.Content.Infrastructure/"]
COPY ["src/Services/JJDevHub.Sync/JJDevHub.Sync.csproj", "src/Services/JJDevHub.Sync/"]

RUN dotnet restore "src/Services/JJDevHub.Sync/JJDevHub.Sync.csproj"

COPY src/Services/JJDevHub.Shared.Kernel/ src/Services/JJDevHub.Shared.Kernel/
COPY src/Services/JJDevHub.Content/ src/Services/JJDevHub.Content/
COPY src/Services/JJDevHub.Sync/ src/Services/JJDevHub.Sync/

WORKDIR /src/src/Services/JJDevHub.Sync
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JJDevHub.Sync.dll"]
