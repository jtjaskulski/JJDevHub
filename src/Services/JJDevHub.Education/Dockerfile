FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/JJDevHub.Education/JJDevHub.Education.csproj", "src/Services/JJDevHub.Education/"]

RUN dotnet restore "src/Services/JJDevHub.Education/JJDevHub.Education.csproj"

COPY src/Services/JJDevHub.Education/ src/Services/JJDevHub.Education/

WORKDIR /src/src/Services/JJDevHub.Education
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JJDevHub.Education.dll"]
