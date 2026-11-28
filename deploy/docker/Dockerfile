# syntax=docker/dockerfile:1
ARG PROJECT
ARG ASSEMBLY
ARG BASE_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.15
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
ARG ASSEMBLY
ARG TARGETARCH
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY --parents src/*/*/*.csproj src/*/*/*/*.csproj ./
RUN set -eu; \
    case "$TARGETARCH" in \
        amd64) rid=linux-x64 ;; \
        arm64) rid=linux-arm64 ;; \
        *) echo "nieobsługiwana architektura: ${TARGETARCH}" >&2; exit 1 ;; \
    esac; \
    printf '%s\n' "$rid" > /tmp/rid; \
    case "$PROJECT" in \
        "" | *"/"* | *".."*) echo "zła nazwa projektu" >&2; exit 1 ;; \
    esac; \
    csproj=$(find src -path "*/${PROJECT}/${PROJECT}.csproj" -print -quit); \
    test -n "$csproj"; \
    dotnet restore "$csproj" -r "$rid"
COPY src/ src/
RUN set -eu; \
    rid=$(cat /tmp/rid); \
    case "$ASSEMBLY" in \
        "" | *"/"* | *".."*) echo "zła nazwa zestawu" >&2; exit 1 ;; \
    esac; \
    csproj=$(find src -path "*/${PROJECT}/${PROJECT}.csproj" -print -quit); \
    test -n "$csproj"; \
    dotnet publish "$csproj" -c Release -o /out --no-restore -r "$rid" --no-self-contained; \
    test -f "/out/${ASSEMBLY}"; \
    base=${ASSEMBLY%.dll}; \
    test -f "/out/${base}.runtimeconfig.json"; \
    test -f "/out/${base}.deps.json"; \
    ln -s "$ASSEMBLY" /out/entrypoint.dll; \
    ln -s "${base}.runtimeconfig.json" /out/entrypoint.runtimeconfig.json; \
    ln -s "${base}.deps.json" /out/entrypoint.deps.json
FROM ${BASE_IMAGE} AS final
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_HTTP_PORTS=8080
USER 10001
ENTRYPOINT ["dotnet", "/app/entrypoint.dll"]
