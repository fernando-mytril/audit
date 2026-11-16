FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Mytril.MaterialId.SharedKernel/Mytril.MaterialId.SharedKernel.csproj", "src/Mytril.MaterialId.SharedKernel/"]
COPY ["src/Mytril.MaterialId.Catalogo/Mytril.MaterialId.Catalogo.csproj", "src/Mytril.MaterialId.Catalogo/"]
COPY ["src/Mytril.MaterialId.Conferencias/Mytril.MaterialId.Conferencias.csproj", "src/Mytril.MaterialId.Conferencias/"]
COPY ["src/Mytril.MaterialId.Api/Mytril.MaterialId.Api.csproj", "src/Mytril.MaterialId.Api/"]
RUN dotnet restore "src/Mytril.MaterialId.Api/Mytril.MaterialId.Api.csproj"
COPY src/ src/
RUN dotnet publish "src/Mytril.MaterialId.Api/Mytril.MaterialId.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Mytril.MaterialId.Api.dll"]
