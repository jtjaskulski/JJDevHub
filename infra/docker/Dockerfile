FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

ENV NUGET_PACKAGES=/root/.nuget/packages

COPY nuget.config Directory.Build.props Directory.Packages.props global.json ./
COPY src/api/ src/api/

RUN dotnet restore src/api/JJDevHub.Api.csproj --configfile nuget.config
RUN dotnet publish src/api/JJDevHub.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS runtime
# Tag still ships openssl 3.5.5-1ubuntu3.5 (CVE-2026-84782). Ubuntu has 3.5.5-1ubuntu3.6+.
# /usr/bin/pebble is an unused Go 1.26.5 binary (stdlib HIGH, fixed in 1.26.6+).
# Nothing in the image references it; the entrypoint is dotnet.
RUN apt-get update \
 && DEBIAN_FRONTEND=noninteractive apt-get install -y --only-upgrade --no-install-recommends libssl3t64 openssl openssl-provider-legacy \
 && rm -rf /var/lib/apt/lists/* /usr/bin/pebble /var/lib/pebble
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "JJDevHub.Api.dll"]
