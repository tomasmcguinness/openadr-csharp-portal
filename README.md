# openadr-csharp-portal

A minimal OpenADR 3.1 VTN (Virtual Top Node) in C#, built as a demo backend for the
[ESP32 Home Energy Manager](https://github.com/tomasmcguinness/matter-esp32-home-energy-manager),
which sends an hourly whole-home demand forecast per the Matter↔OpenADR 3.x Interworking
Spec v1.0, scenario IW 2.5.1 ("EMS provides forecast with flexibility").

**This is a demo, not a production system.** There is no TLS, no MQTT authentication, and no
access control. Don't expose it to anything you don't trust.

## What's here

| Path | What |
|---|---|
| `src/OpenAdr.Vtn.Core` | Shared DTOs matching the OpenADR 3.1 OpenAPI schemas, plus target filtering, MQTT topic naming, DST-safe local-day bucketing, and report validation |
| `src/OpenAdr.Vtn.Server` | The VTN itself: REST API, in-process MQTT broker, SQLite via EF Core, SignalR, and the dashboard |
| `src/OpenAdr.Vtn.FakeVen` | A console app that plays the VEN role end-to-end, for testing without real hardware |
| `dashboard/` | The "My Home" dashboard (React + Vite + TS), built into `wwwroot/` automatically |
| `tests/OpenAdr.Vtn.Core.Tests` | xUnit tests: report validation, target filtering, DST bucketing |
| `Dockerfile` / `docker-compose.yml` | Container build (VTN + fake VEN) for running without a .NET/Node toolchain installed |
| `Caddyfile` / `docker-compose.caddy.yml` + `caddy/Caddyfile` | Two ways to front the VTN with HTTPS — paste into an existing Caddy elsewhere, or bundle one into this stack |
| `docs/ven-flow.md` | The exact HTTP/MQTT sequence a VEN follows, with real example payloads — written for porting to ESP-IDF C |

The spec used as source of truth is the real OpenADR 3.1 OpenAPI definition at
[`grid-coordination/openadr3-specification`](https://github.com/grid-coordination/openadr3-specification/tree/main/3.1.0)
(`openadr3.yaml` / `Definition.md`).

## Scope

Implemented: `/auth/token` (OAuth2 client-credentials shape — any client_id/secret is
accepted), `/programs`, `/events`, `/vens`, `/reports` (full CRUD), `/notifiers`, and
`/notifiers/mqtt/topics/*` for programs, events, `vens/{venID}/*`, plus top-level `vens` and
`reports` topics for symmetry with what the VTN actually publishes.

Not implemented (out of scope for this demo): `/subscriptions`, `/resources`, and webhook
delivery (the `/notifiers` response reports `WEBHOOK: true` as the spec requires, but no
webhook is ever actually sent — this demo only implements the MQTT notifier binding).

### Interworking extensions

The Matter↔OpenADR interworking profile uses a few fields the 3.1 schema doesn't define
(or defines more loosely). This VTN accepts them leniently rather than rejecting the
request:

- `event.duration` (overall event duration) and `reportDescriptor.reportIntervals` are
  accepted and stored, but never validated against an enum or enforced.
- Report intervals may be any length and any count — not just 24×PT1H. The VTN never
  assumes a fixed interval count, which is what makes 23/25-hour DST days work without
  special-casing on either side.
- `target` is a flat string in this version of the spec (not a `{type, values}` object).
  The `VEN_NAME`/`PROGRAM_NAME` targeting convention from the interworking profile is
  encoded as a single string, `"TYPE:value"` (e.g. `"PROGRAM_NAME:HomeForecast"`) — this
  project's own choice of encoding for that convention, not something the spec defines.
- The forecast/actual report split is encoded as `reportDescriptor.payloadType` /
  `reportPayloadDescriptor.payloadType` of `FORECAST` or `DIRECT_READ`, with
  `readingType: "DEMAND"` and `units: "KW"` on both — see `docs/ven-flow.md` for the exact
  shape.

## Configuration

`src/OpenAdr.Vtn.Server/appsettings.json` plus environment variables (`Vtn__DatabasePath`,
`Vtn__MqttPort`, `Vtn__MqttEnabled`, `Kestrel__Endpoints__Http__Url`):

| Key | Default | What |
|---|---|---|
| `Vtn:DatabasePath` | `/var/lib/openadr-vtn/vtn.db` (`./db/vtn.db` in Development) | SQLite file path; the directory is created automatically if missing |
| `Vtn:MqttEnabled` | `false` | Whether the in-process MQTT broker runs at all. When `false`, `/notifiers` reports `mqtt: null` (VENs must poll instead of subscribing) and the dashboard hides the MQTT status row |
| `Vtn:MqttPort` | `1883` | Port the in-process MQTT broker listens on, when enabled |
| `Kestrel:Endpoints:Http:Url` | `http://0.0.0.0:80` (`http://localhost:5080` in Development) | What Kestrel binds to |

**The HTTP port is baked into `appsettings.json` as an explicit Kestrel endpoint (port 80),
not left to Kestrel's own built-in default (port 5000)** — so the app binds the right port
no matter how or from where it's launched, including by hand (`./OpenAdr.Vtn.Server`,
no flags) rather than only through the systemd unit. `appsettings.Development.json`
overrides it to `http://localhost:5080` so local `dotnet run` doesn't need root/admin for a
privileged port; `ASPNETCORE_ENVIRONMENT` unset defaults to Production, so a plain deploy
with no environment variable set still gets port 80.

One consequence: because an explicit Kestrel endpoint is configured, **`--urls` and
`ASPNETCORE_URLS` are both ignored** (Kestrel endpoint config takes precedence over them).
To override the port without rebuilding, set `Kestrel__Endpoints__Http__Url` instead, e.g.
`Kestrel__Endpoints__Http__Url=http://0.0.0.0:8080`.

The app also explicitly anchors its content root to its own executable directory
(`AppContext.BaseDirectory` in `Program.cs`), not the caller's current working directory —
otherwise `appsettings.json` (and this whole port setup) would silently go unfound, and
Kestrel would fall back to port 5000, if the binary is ever launched from somewhere other
than its own folder.

## Local run

Requires the .NET 9 SDK and Node.js (for the dashboard build — see below).

```sh
dotnet run --project src/OpenAdr.Vtn.Server
```

This single command builds the dashboard (first run only — see "Dashboard build" below),
applies EF Core migrations, seeds the `HomeForecast` program and its two demo events on
first run, and starts the VTN: REST + dashboard on `http://localhost:5080`. The in-process
MQTT broker is off by default (`Vtn:MqttEnabled: false`); set it to `true` to also listen on
`localhost:1883` for VENs that want push notifications instead of polling.

Open `http://localhost:5080` for the dashboard. It'll show "Waiting for a VEN to report in…"
until something posts a report — run the fake VEN to see it update live:

```sh
dotnet run --project src/OpenAdr.Vtn.FakeVen -- http://localhost:5080 localhost --fast
```

`--fast` posts every 10 seconds with a synthetic, slightly drifting demand curve instead of
waiting for the top of the hour — the quickest way to watch the dashboard chart move. Drop
`--fast` to run on the real hourly cadence. Other flags: `--ven-name`, `--client-id`,
`--client-secret`, `--mqtt-port` (all have demo defaults; see `--help`-equivalent usage text
if run with too few arguments).

### Dashboard build

`dashboard/` is a separate React/Vite/TypeScript project, built into
`src/OpenAdr.Vtn.Server/wwwroot/` by an MSBuild target
(`BuildDashboard` in `OpenAdr.Vtn.Server.csproj`) that runs automatically before every
`dotnet build`/`dotnet run`/`dotnet publish` — there's no separate manual build step. It's
incremental (keyed off the dashboard's own source files), so a plain C#-only edit-and-run
stays fast.

To iterate on the dashboard with hot reload instead, run it standalone — its dev server
proxies API/SignalR calls to the backend (see `dashboard/vite.config.ts`):

```sh
cd dashboard
npm install
npm run dev
```

### Tests

```sh
dotnet test tests/OpenAdr.Vtn.Core.Tests
```

Covers report validation, target filtering (the additive-AND semantics `/programs` and
`/events` use), and local-day forecast bucketing across both 2026 UK DST transitions
(2026-03-29 spring-forward, 23-hour day; 2026-10-25 fall-back, 25-hour day).

### Restart behavior

All state lives in the SQLite file — restarting the server keeps every program, event, ven,
and report (including forecast history). The fake VEN (and the real HEMS, once pointed at
this VTN) reconnects on its own: see "Recovery paths" in `docs/ven-flow.md`.

## Pointing the real HEMS at this VTN

The HEMS firmware should follow the exact sequence in `docs/ven-flow.md` — it was written
from the fake VEN's actual traffic specifically so it can be ported line-for-line to
`esp_http_client` + `esp-mqtt` + `cJSON`. In short: get a token from `/auth/token`,
find-or-create a ven by `venName`, read `/notifiers` and connect to the advertised MQTT
broker (plain TCP, anonymous, port 1883), discover the `HomeForecast` program and its
forecast event, then `POST /reports` hourly with a 24-interval forecast plus a 1-interval
actual reading for the hour just completed.

## Run with Docker

```sh
cp .env.example .env   # adjust VTN_PORT/VTN_MQTT_ENABLED if needed
docker compose up --build
```

Builds and starts `vtn` (published to `http://localhost:8080`, SQLite persisted in the
`vtn-data` named volume) — no .NET/Node toolchain needed on the host, the image builds the
dashboard and the server the same way `dotnet publish` does locally. Add the fake VEN too:

```sh
docker compose --profile demo up --build
```

`vtn` listens on 8080, not 80 — intentionally, so the container can run as the non-root
`$APP_UID` user without needing `CAP_NET_BIND_SERVICE`. Point a reverse proxy at it for a
real domain + HTTPS. Two options, depending on what's easiest for your setup:

**Option A — an existing Caddy instance elsewhere on the LAN.** Copy the block in this
repo's `Caddyfile` into *that* instance's config, alongside whatever else it already
fronts. Set `VTN_DOMAIN=vtn.tomasmcguinness.com` wherever that Caddy instance gets its
other `_DOMAIN` vars from, and replace `<docker-host-lan-ip>` with the LAN IP of the
machine running `docker compose` here (not `localhost` — Caddy is on a different host).

**Option B — bundle Caddy into this project's own stack instead**, if editing that other
instance's config isn't practical (it belongs to another service, is managed by a
GUI/orchestrator, you don't want to risk reloading it, etc.):

```sh
# set VTN_DOMAIN=vtn.tomasmcguinness.com in .env first
docker compose -f docker-compose.yml -f docker-compose.caddy.yml up --build
```

This adds a `caddy` service (config in `caddy/Caddyfile`) that reverse-proxies to `vtn`
over this stack's own Docker network — nothing to coordinate with any other service, and
no `<docker-host-lan-ip>` substitution needed since it's all one compose project. It
publishes 80/443 on this host, so skip this option if something else here already binds
those ports.

Either way, Caddy issues the Let's Encrypt certificate automatically, as long as the DNS
record points at whichever host is running Caddy and ports 80/443 reach it from the
internet.
