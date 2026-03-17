FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/JJDevHub.Identity/JJDevHub.Identity.csproj", "src/Services/JJDevHub.Identity/"]

RUN dotnet restore "src/Services/JJDevHub.Identity/JJDevHub.Identity.csproj"

COPY src/Services/JJDevHub.Identity/ src/Services/JJDevHub.Identity/

WORKDIR /src/src/Services/JJDevHub.Identity
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JJDevHub.Identity.dll"]
