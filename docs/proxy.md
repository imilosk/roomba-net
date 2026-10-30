# RoombaNet MQTT Proxy

## Why

iRobot robots accept **only one** local MQTT connection at a time; a second client with the same
identity kicks the first. Today each RoombaNet surface (API, MCP, CLI) opens its own direct MQTT
connection per robot, so they fight over the robot's single slot. The proxy holds the one real
upstream connection per robot and multiplexes it to many clients.

## Architecture

```
                          RoombaNet.Proxy (RoombaProxyService)
                          ┌───────────────────────────────────────────────┐
  robot A (TLS 8883) ───► │ UpstreamRobotConnection(A)  ┐                  │
  robot B (TLS 8883) ───► │ UpstreamRobotConnection(B)  │ merged shadow    │
                          │   - client-id = BLID        │ state per robot  │
                          │   - TLS, cert not verified  │                  │
                          │   - auto-reconnect+backoff  ▼                  │
                          │                      ┌──────────────┐          │
                          │   inject (per-robot) │ NativeDropIn │ TLS :8883│ ◄── API / MCP / CLI
                          │ ────────────────────►│   Broker     │          │     (drop-in clients,
                          │                      └──────────────┘          │      auth = BLID+pw)
                          │                      ┌──────────────┐          │
                          │   inject (namespaced)│ Aggregated   │ :1883    │ ◄── mosquitto_sub, HA,
                          │ ────────────────────►│ PlainBroker  │ loopback │     scripts (no auth)
                          │                      └──────────────┘          │
                          └───────────────────────────────────────────────┘
```

Each component:

- **`UpstreamRobotConnection`** — one per robot. Connects with the robot's BLID as the client-id
  (required by the robot for TLS), subscribes `#` plus `$aws/things/<blid>/shadow/#`, requests the
  full shadow on every (re)connect, auto-reconnects with 1s→30s backoff, and keeps a deep-merged
  `state.reported` snapshot. Mirrors `RoombaConnectionManager`'s upstream options.
- **`NativeDropInBroker`** — an embedded MQTTnet broker exposing a **TLS listener** on 8883. This is
  the drop-in surface: clients authenticate and see the robot's **native** topics.
- **`AggregatedPlainBroker`** — an embedded MQTTnet broker exposing a **plain listener** on
  127.0.0.1:1883 for tooling.
- **`RoombaProxyService`** — the orchestrator (`IHostedService`). Wires upstream messages into both
  brokers, publishes merged snapshots and online/offline status, and relays allow-listed client
  messages back to the robot.

## The drop-in model (native TLS listener)

A client connects **exactly as it would to the real robot** — the only change is the host IP:

| Setting   | Value                        |
|-----------|------------------------------|
| Transport | TLS on port 8883 (cert not verified by the client) |
| Username  | robot BLID                   |
| Password  | robot's local password       |
| Client-id | robot BLID (`<blid>-<suffix>` also accepted, to avoid session takeover when several clients share one robot) |

Behaviour:

- **Auth & routing by BLID.** The proxy authenticates each client against the known robot credentials
  and binds the connection to that robot. Routing is keyed off the authenticated username (== BLID).
  Unknown or mismatched credentials are rejected (`BadUserNameOrPassword`). Non-TLS connections are
  rejected on this listener.
- **Native topics.** A bound client sees that robot's native topics (`cmd`, `delta`, `wifistat`,
  `$aws/things/<blid>/...`, etc.) — identical to the real robot. To talk to two robots, open two
  connections (one per robot's credentials) to the same proxy IP.
- **Isolation.** A per-client enqueue interceptor guarantees a client bound to robot A never receives
  robot B's messages, even on shared native topics like `wifistat`.
- **Shadow replay on connect.** The merged reported shadow is kept as a **retained** message on
  `$aws/things/<blid>/shadow/update`, so a drop-in client that expects the full shadow on connect
  receives it immediately on subscribe (the robot only emits the full shadow on connect, and
  `shadow/get` is a no-op on some firmware).
- **Relay allow-list (client → robot).** Only these topics are relayed to the robot:
  `cmd`, `delta`, `wifictl`, `local/rrtp/request`, `cloud/rrtp/request`, and the robot's own
  `$aws/things/<blid>/shadow/get`. Everything else stays local (no relay loops: proxy-injected
  messages are tagged and never relayed back).
- **Status.** Retained `roombaproxy/<blid>/status` = `online` / `offline`.

## The plain listener (aggregated view)

A convenience **no-auth** listener bound to loopback (default 127.0.0.1:1883) for quick tooling
(`mosquitto_sub`, scripts, Home Assistant). When `Aggregated` is true, every robot is mirrored under
`roomba/<blid>/<topic>` with a merged retained snapshot on `roomba/<blid>/state` and retained status
on `roomba/<blid>/status`, so a single subscription (`roomba/#`) can watch all robots at once.
Allow-listed publishes to `roomba/<blid>/<topic>` are relayed to the robot.

## Configuration

```jsonc
"Proxy": {
  "Enabled": false,                 // master switch for the hosted service (opt-in)
  "Tls": {
    "Port": 8883,
    "CertificatePath": null,        // optional .pfx; a self-signed cert is generated if unset
    "CertificatePassword": null
  },
  "Plain": {
    "Enabled": true,
    "Host": "127.0.0.1",            // kept on loopback: this listener has no auth
    "Port": 1883,
    "Aggregated": true              // mirror each robot under roomba/<blid>/...
  },
  "Robots": []                      // explicit list; when empty, robots come from the robot source
}
```

Explicit `Proxy:Robots` entries: `{ "Blid", "Ip", "Port", "Password", "UseTls" }`.

### Where the robot list comes from

- **CLI / standalone**: `ConfigurationProxyRobotSource` — uses `Proxy:Robots` if present, otherwise
  derives the list from `Robots:Profiles` (the same profile map the CLI/API already use).
- **API**: `RegistryProxyRobotSource` — every robot in the API robot registry that has stored
  credentials. (Registered before `AddRoombaProxy`, so it wins over the configuration default.)

## Running it

- **CLI (foreground):** `dotnet run --project src/RoombaNet.Cli proxy`
- **API (hosted service):** set `Proxy:Enabled = true`; the proxy starts with the API.

## TLS certificate

The native listener presents a self-signed certificate (generated at startup, or loaded from
`Proxy:Tls:CertificatePath`). Drop-in clients do not verify it — exactly as they do not verify the
robot's own self-signed certificate.

## Phase 2 (deferred): point the surfaces at the proxy

Phase 1 (this change) is complete and self-contained: the proxy runs, is unit-tested, and is runnable
from the CLI and API. Phase 2 — refactoring the API/MCP/CLI so their `RoombaConnectionManager`
targets the proxy (loopback) instead of the robot directly — is **not** done yet. The groundwork is
in place:

- The proxy is a drop-in replacement for the robot, so a surface can point its existing
  `RoombaConnectionManager` at the proxy host with **no code change** — only the target IP changes.
- Suggested approach: add a config switch (e.g. `Mqtt:UseProxy` + `Mqtt:ProxyHost`) consumed by
  `RoombaClientFactory` (API) and the CLI/MCP connection setup. When off (the default), behaviour is
  unchanged and surfaces connect directly to the robot. When on, they connect to the proxy host with
  the same BLID credentials.
- Because the switch defaults to off, enabling the proxy never changes existing behaviour until the
  refactor is finished and explicitly turned on.

## Testing

`src/RoombaNet.Proxy.Tests` exercises the real brokers over loopback (TLS included), with each fake
robot modelled by an in-memory MQTTnet broker — no real robot is contacted. Coverage:

- BLID-based auth/routing (accept correct, reject wrong password / unknown BLID).
- Relay allow-list (only allow-listed topics reach the robot).
- Merged-state deep-merge and retained replay to a late-joining client.
- Multi-robot isolation (client A never sees robot B's native topics).
- Aggregated plain-listener mirroring.
- `MergedShadowState` deep-merge and non-JSON safety.

## Notes / assumptions

- **Two embedded brokers** (rather than one with two endpoints) keep the native and aggregated
  namespaces, their differing auth, and their bindings cleanly separated.
- **Session takeover:** strict drop-in clients all use client-id = BLID; the MQTT broker will take
  over an older session if two use the identical client-id. Clients that want several concurrent
  connections to the same robot can use `"<blid>-<suffix>"` (still routed by the BLID username).
- The CLI is AOT-published. The proxy lives in its own non-AOT-constrained assembly; the proxy and
  its dependencies (MQTTnet, MQTTnet.Server, the configuration binding source generator) build and
  test cleanly. If an AOT publish of the CLI surfaces trim warnings for the proxy path, run the proxy
  via the API hosted service (not AOT) or publish the CLI without AOT.
```
