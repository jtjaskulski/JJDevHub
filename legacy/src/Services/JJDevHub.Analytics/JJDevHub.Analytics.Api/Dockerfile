FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Core/JJDevHub.Analytics.Core.csproj", "src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Core/"]
COPY ["src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Application/JJDevHub.Analytics.Application.csproj", "src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Application/"]
COPY ["src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Infrastructure/JJDevHub.Analytics.Infrastructure.csproj", "src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Infrastructure/"]
COPY ["src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Api/JJDevHub.Analytics.Api.csproj", "src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Api/"]

RUN dotnet restore "src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Api/JJDevHub.Analytics.Api.csproj"

COPY src/Services/JJDevHub.Analytics/ src/Services/JJDevHub.Analytics/

WORKDIR /src/src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JJDevHub.Analytics.Api.dll"]
