FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/JJDevHub.Api/JJDevHub.Api.csproj src/JJDevHub.Api/
RUN dotnet restore src/JJDevHub.Api/JJDevHub.Api.csproj

COPY src/JJDevHub.Api/ src/JJDevHub.Api/
RUN dotnet publish src/JJDevHub.Api/JJDevHub.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "JJDevHub.Api.dll"]
