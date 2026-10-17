# syntax=docker/dockerfile:1

# ---- build: same thing `dotnet publish` does locally — OpenAdr.Vtn.Server.csproj's own
# BuildDashboard MSBuild target shells out to npm, so Node needs to be on PATH here too. ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Project files first (restore is the slow, cacheable step; source changes shouldn't bust it).
COPY src/OpenAdr.Vtn.Core/OpenAdr.Vtn.Core.csproj src/OpenAdr.Vtn.Core/
COPY src/OpenAdr.Vtn.Server/OpenAdr.Vtn.Server.csproj src/OpenAdr.Vtn.Server/
COPY src/OpenAdr.Vtn.FakeVen/OpenAdr.Vtn.FakeVen.csproj src/OpenAdr.Vtn.FakeVen/
RUN dotnet restore src/OpenAdr.Vtn.Server/OpenAdr.Vtn.Server.csproj \
    && dotnet restore src/OpenAdr.Vtn.FakeVen/OpenAdr.Vtn.FakeVen.csproj

COPY src/ src/
COPY dashboard/ dashboard/

RUN dotnet publish src/OpenAdr.Vtn.Server/OpenAdr.Vtn.Server.csproj -c Release --no-restore -o /app/server
RUN dotnet publish src/OpenAdr.Vtn.FakeVen/OpenAdr.Vtn.FakeVen.csproj -c Release --no-restore -o /app/fakeven

# ---- fakeven: the console app that plays the VEN role, for exercising the VTN without
# real hardware (see docker-compose.yml's "demo" profile) ----
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS fakeven
WORKDIR /app
COPY --from=build /app/fakeven .
ENTRYPOINT ["dotnet", "OpenAdr.Vtn.FakeVen.dll"]

# ---- server: the VTN itself. Listens on 8080, not 80 — a non-root container user can't
# bind a sub-1024 port without extra capabilities, and there's no need to grant any here
# since Caddy (see docker-compose.yml) is what actually faces 80/443. ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS server
WORKDIR /app
COPY --from=build /app/server .

ENV ASPNETCORE_ENVIRONMENT=Production \
    Vtn__DatabasePath=/var/lib/openadr-vtn/vtn.db \
    Kestrel__Endpoints__Http__Url=http://0.0.0.0:8080

EXPOSE 8080

# Owned by $APP_UID up front so a fresh named volume mounted here (Docker seeds a new
# volume's content *and* ownership from whatever's already at the mount point in the image)
# is writable once the container drops to that non-root user below.
RUN mkdir -p /var/lib/openadr-vtn && chown $APP_UID:$APP_UID /var/lib/openadr-vtn
VOLUME ["/var/lib/openadr-vtn"]

USER $APP_UID
ENTRYPOINT ["dotnet", "OpenAdr.Vtn.Server.dll"]
