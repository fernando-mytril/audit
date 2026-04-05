FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Mytril.Audit.Domain/Mytril.Audit.Domain.csproj", "src/Mytril.Audit.Domain/"]
COPY ["src/Mytril.Audit.Application/Mytril.Audit.Application.csproj", "src/Mytril.Audit.Application/"]
COPY ["src/Mytril.Audit.Infrastructure/Mytril.Audit.Infrastructure.csproj", "src/Mytril.Audit.Infrastructure/"]
COPY ["src/Mytril.Audit.Sdk/Mytril.Audit.Sdk.csproj", "src/Mytril.Audit.Sdk/"]
COPY ["src/Mytril.Audit.Api/Mytril.Audit.Api.csproj", "src/Mytril.Audit.Api/"]
RUN dotnet restore "src/Mytril.Audit.Api/Mytril.Audit.Api.csproj"

COPY src/ src/
WORKDIR "/src/src/Mytril.Audit.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mytril.Audit.Api.dll"]
