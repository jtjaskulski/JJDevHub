FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

ENV NUGET_PACKAGES=/root/.nuget/packages

COPY nuget.config Directory.Build.props Directory.Packages.props global.json ./
COPY src/JJDevHub.Api/ src/JJDevHub.Api/

RUN dotnet restore src/JJDevHub.Api/JJDevHub.Api.csproj --configfile nuget.config
RUN dotnet publish src/JJDevHub.Api/JJDevHub.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "JJDevHub.Api.dll"]
