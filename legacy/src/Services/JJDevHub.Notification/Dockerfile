FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/JJDevHub.Notification/JJDevHub.Notification.csproj", "src/Services/JJDevHub.Notification/"]

RUN dotnet restore "src/Services/JJDevHub.Notification/JJDevHub.Notification.csproj"

COPY src/Services/JJDevHub.Notification/ src/Services/JJDevHub.Notification/

WORKDIR /src/src/Services/JJDevHub.Notification
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JJDevHub.Notification.dll"]
