# --- Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Socios.Api/Socios.Api.csproj Socios.Api/
COPY Socios.Application/Socios.Application.csproj Socios.Application/
COPY Socios.Domain/Socios.Domain.csproj Socios.Domain/
COPY Socios.Infrastructure/Socios.Infrastructure.csproj Socios.Infrastructure/
RUN dotnet restore Socios.Api/Socios.Api.csproj

COPY Socios.Api/ Socios.Api/
COPY Socios.Application/ Socios.Application/
COPY Socios.Domain/ Socios.Domain/
COPY Socios.Infrastructure/ Socios.Infrastructure/
RUN dotnet publish Socios.Api/Socios.Api.csproj -c Release -o /app/publish --no-restore

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
# Evita un SIGSEGV conocido del file-watcher de configuración de .NET en
# contenedores Linux (inotify). Se lee antes de que exista el host builder.
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
EXPOSE 8080

ENTRYPOINT ["dotnet", "Socios.Api.dll"]