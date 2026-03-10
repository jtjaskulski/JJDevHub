FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/Services/JJDevHub.AI.Gateway/JJDevHub.AI.Gateway/JJDevHub.AI.Gateway.csproj", "src/Services/JJDevHub.AI.Gateway/JJDevHub.AI.Gateway/"]

RUN dotnet restore "src/Services/JJDevHub.AI.Gateway/JJDevHub.AI.Gateway/JJDevHub.AI.Gateway.csproj"

COPY src/Services/JJDevHub.AI.Gateway/ src/Services/JJDevHub.AI.Gateway/

WORKDIR /src/src/Services/JJDevHub.AI.Gateway/JJDevHub.AI.Gateway
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JJDevHub.AI.Gateway.dll"]
