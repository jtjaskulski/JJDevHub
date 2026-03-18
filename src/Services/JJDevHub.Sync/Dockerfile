FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/JJDevHub.Sync/JJDevHub.Sync.csproj", "src/Services/JJDevHub.Sync/"]

RUN dotnet restore "src/Services/JJDevHub.Sync/JJDevHub.Sync.csproj"

COPY src/Services/JJDevHub.Sync/ src/Services/JJDevHub.Sync/

WORKDIR /src/src/Services/JJDevHub.Sync
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JJDevHub.Sync.dll"]
