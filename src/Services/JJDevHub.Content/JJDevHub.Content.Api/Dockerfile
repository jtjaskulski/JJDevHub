FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/Services/JJDevHub.Shared.Kernel/JJDevHub.Shared.Kernel.csproj", "src/Services/JJDevHub.Shared.Kernel/"]
COPY ["src/Services/JJDevHub.Content/JJDevHub.Content.Core/JJDevHub.Content.Core.csproj", "src/Services/JJDevHub.Content/JJDevHub.Content.Core/"]
COPY ["src/Services/JJDevHub.Content/JJDevHub.Content.Application/JJDevHub.Content.Application.csproj", "src/Services/JJDevHub.Content/JJDevHub.Content.Application/"]
COPY ["src/Services/JJDevHub.Content/JJDevHub.Content.Infrastructure/JJDevHub.Content.Infrastructure.csproj", "src/Services/JJDevHub.Content/JJDevHub.Content.Infrastructure/"]
COPY ["src/Services/JJDevHub.Content/JJDevHub.Content.Persistence/JJDevHub.Content.Persistence.csproj", "src/Services/JJDevHub.Content/JJDevHub.Content.Persistence/"]
COPY ["src/Services/JJDevHub.Content/JJDevHub.Content.Api/JJDevHub.Content.Api.csproj", "src/Services/JJDevHub.Content/JJDevHub.Content.Api/"]

RUN dotnet restore "src/Services/JJDevHub.Content/JJDevHub.Content.Api/JJDevHub.Content.Api.csproj"

COPY src/Services/JJDevHub.Shared.Kernel/ src/Services/JJDevHub.Shared.Kernel/
COPY src/Services/JJDevHub.Content/ src/Services/JJDevHub.Content/

WORKDIR /src/src/Services/JJDevHub.Content/JJDevHub.Content.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JJDevHub.Content.Api.dll"]
