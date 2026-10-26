# RoombaNet — protocol notes & improvement roadmap

Single source of truth for the Roomba local protocol we've verified, the current RoombaNet
architecture, and the plan to improve it + move from the REST/OpenAPI surface to Home Assistant.
Firmware-level reverse-engineering (OTA, package format, secure boot) lives one level up in
`../../FIRMWARE_FINDINGS.md`, `../../APKG_FORMAT.md`, `../../HARDWARE_PLAN.md`,
`../../I3_TEARDOWN_GUIDE.md`, `../../OTA_CAPTURE.md`. This doc is about the *local control* layer.

---

## 1. Verified facts (this network, 2026-10-24)

### Our robots
| | Robot A (live) | Robot B (live) |
|---|---|---|
| IP | 192.168.20.65 | 192.168.20.66 |
| BLID (MQTT user) | `<see secrets.Development.json>` | `<see secrets.Development.json>` |
| password | `<see secrets.Development.json>` | `<see secrets.Development.json>` |
| model | i3+ (sku **i355840**, codename Daredevil) | **Braava jet m6** (sku **m611020**, codename Sanmarino) |
| firmware | **daredevil+2.6.0+daredevil-release+163** | **sanmarino+22.29.6+2022-12-01-82f3372a65c+Firmware-Build+2321** |
| MAC | 50:14:79:40:F0:C5 | 50:14:79:8B:CD:8F |

> Creds are in `src/RoombaNet.Api/secrets.Development.json` (keys `RoombaSettingsX/Y`). These are
> secrets — keep them out of git history; the roadmap moves them into the encrypted registry DB.

### Network reality
- Robots live on a **separate IoT VLAN/SSID** (192.168.20.0/24); dev machine is on 192.168.1.0/24.
- The router **routes** main→IoT and **8883 is reachable across subnets**. Good enough for a
  headless bridge; if you lock the IoT VLAN down later, the bridge host must sit on/route to it.
- UDP **broadcast discovery (:5678) does NOT cross subnets** — that's why `discover` found nothing
  from the main net. **Unicast discovery to a known IP DOES work** (we got a full reply from .65).
  → Roadmap item: support unicast/seed-IP discovery, not just broadcast.
- Roombas are **2.4 GHz only** and allow **ONE MQTT connection at a time** (connecting kicks the
  app/cloud off, and vice-versa).

### Protocol (all confirmed working)
- **Discovery:** UDP, send `irobotmcs` to `:5678` (broadcast *or* unicast). Reply is JSON with
  `robotid`(=BLID), `ip`, `mac`, `sku`, `sw`, `hostname`, `proto`, and a `cap` map
  (e.g. `maps`, `pmaps`, `ota`, `multiPass`, `5ghz`). Great for zero-config onboarding.
- **Password retrieval:** TLS to `:8883`, send bytes `f0 05 ef cc 3b 29 00` while the robot is in
  password mode (hold HOME ~2 s until it chimes); password = response after a 13-byte header.
  (We didn't need this — creds were already stored — but keep it for first-time pairing.)
- **Control:** MQTT over TLS on `:8883`, username=BLID, password=stored pw, clientId=BLID.
  TLS is old (use a permissive context / `@SECLEVEL=0`; no cert verification).
  - Publish commands to topic **`cmd`**: `{"command":"<cmd>","time":<unix>,"initiator":"localApp"}`
  - Publish settings to topic **`delta`**: `{"state":{"desired":{...}}}`
  - Subscribe `#` (or the shadow topic) for state. Robot echoes `lastCommand`, and reports
    `cleanMissionStatus`, `pose`, `batPct`/`batInfo`, `signal`, `wifistat`, bin/err, etc.
- **Verified live:** connected (rc=0), read battery/wifi shadow, sent `find` → robot beeped and
  echoed `lastCommand:{command:find}`. Full two-way control confirmed.

### Command/settings reference (from the app's decompiled CommandType + our tests)
- Motion/mission: `start quick spot clean dock pause resume stop halt wake reset find evac train tidy skip off sleep`
- Wi-Fi: `wlapon wlapoff wlston wlstoff wifiscan`
- Braava/mop (not i3): `flrefill washpad drypad flushsluice ...`
- Settings via `delta`/`desired`: `binPause`, `cleaningPasses`+`vacuumHigh`, `openOnly`/`twoPass`
  (edge/passes), `childLock`, `schedule{cycle,h,m}`, language, etc.
- Status fields: `cleanMissionStatus{cycle,phase,error,nMssn,...}`, `pose{point,theta}`,
  `signal{rssi,snr}`, `batPct`, `bin{full}`. (Full dump: see `../../DecompiledCode/README.md`.)
- Braava m6 (verified 2026-10-25): **no `bin`** field; instead `tankPresent`, `tankLvl` (0–100),
  `lidOpen`, `detectedPad` (`"invalid"` with no pad), `padWetness{disposable,reusable}`,
  `rankOverlap`, `chrgLrPtrn`. Full shadow arrives in one message. Docked with no pad it reports
  `notReady: 34`. `subModSwVer` lists nav/mob/pwr/sft/mobBtl/linux/con versions.

---

## 2. Current RoombaNet architecture (~6.7k LOC C#, .NET 10)

```
RoombaNet.Api            Minimal-API REST + OpenAPI + SSE /stream   <-- "the UI" to replace
  Endpoints/RoombaEndpoints.cs   /api/roomba/{discovery,robots,settings,status,wifi,commands}
RoombaNet.Cli            System.CommandLine: discover, get, set, cmd, subscribe,
                         set-password, configure-wifi
RoombaNet.Core           Services + interfaces:
  RoombaCommandService / RoombaSettingsService / RoombaSubscriptionService /
  RoombaDiscoveryService / RoombaPasswordService / RoombaWifiService
  IMqttPublisher, Payloads/, Constants/, Extensions/
RoombaNet.Transport.Mqtt RoombaConnectionManager (MQTTnet) — the single-connection owner
RoombaNet.Transport.Tls  RoombaDiscoveryClient (UDP), RoombaPasswordClient (handshake), RoombaInfo
RoombaNet.Settings       config + encrypted RobotRegistry (SQLite at DatabasePath, EncryptionKey)
```
- Robot registry = SQLite (`/tmp/roombanet.db` in dev — **ephemeral, resets on reboot**; move to a
  persistent path/volume). `secrets.Development.json` also holds creds + the registry EncryptionKey.
- Tested working on i3; MQTT layer is the single source of the one-connection constraint.

---

## 3. Improvement roadmap

### A. Code / correctness (do first — small, high value)
- [ ] **Persistent DB path** (not `/tmp`); document the volume for Docker. Verify encryption-at-rest
      of stored passwords actually works end-to-end (we have the `EncryptionKey` mechanism).
- [ ] **Get creds out of `secrets.Development.json`** and into the encrypted registry; scrub them
      from git history. Provide a one-time `pair`/import flow.
- [x] **Unicast / seed-IP discovery** in `RoombaDiscoveryClient` (broadcast fails across VLANs — our
      exact situation). Done: `discover --target <ip|cidr>`, API `?target=`, MCP `targets`.
- [ ] **Resilient single connection**: auto-reconnect w/ backoff, surface "app stole the connection"
      cleanly, optional keepalive. Centralize in `RoombaConnectionManager`.
- [ ] **Multi-robot** first-class (we have 2 robots) — registry-driven, one connection each.
- [ ] TLS: pin to the robot's actual cert if we can capture it, instead of blanket `CERT_NONE`.

### B. Features
- [ ] **Full shadow model**: typed parse of `cleanMissionStatus`, `pose`, `bin`, `batInfo`,
      `signal`, error codes → clean events, not raw JSON.
- [ ] **Maps/pmaps** support (caps show `maps:3 pmaps:6`) — room/zone cleaning if we can drive the
      pmap command set.
- [ ] **Scheduling** passthrough (`delta` schedule), **cleaning passes / edge / child-lock** toggles
      (endpoints already exist — make them robust + typed).
- [ ] **OTA observability**: subscribe to OTA/deployment shadow fields and expose current fw +
      update state (ties into `../../OTA_CAPTURE.md`).
- [ ] **Event history / telemetry** persisted (mission logs, battery curves).

### C. Remove the REST/OpenAPI UI → Home Assistant
Recommended shape: **RoombaNet becomes a headless daemon that bridges Roomba ↔ an MQTT broker using
Home Assistant MQTT Discovery.** HA then auto-creates a `vacuum` entity + sensors, no custom
component needed.
- [ ] Add **`RoombaNet.Bridge`** (or a run-mode of the Api) that:
  - connects to the robot(s) over the existing Transport.Mqtt layer, and
  - connects to the **HA MQTT broker** (Mosquitto) as a second client.
- [ ] **Publish HA MQTT Discovery configs** on startup:
  - `homeassistant/vacuum/<blid>/config` → a `vacuum` entity mapping HA commands
    (start/stop/pause/return_to_base/locate/clean_spot) to our `cmd` topic commands.
  - `homeassistant/sensor/<blid>/battery/config`, `.../status`, `.../bin`, `.../wifi` → from shadow.
- [ ] **Translate** HA command topics → robot `cmd`/`delta`; **translate** robot shadow → HA state
      topics (JSON templates). Honor `availability` (online/offline, incl. "app took the connection").
- [ ] **Retire** the OpenAPI/Swagger + REST endpoints once the bridge covers the use-cases (or keep a
      thin `/health` + a debug endpoint). The CLI stays useful for pairing/diagnostics.
- [ ] **Deploy** as a container next to HA (persistent volume for the registry DB). Document compose.
- Alternative considered: native HA **Python custom component** — rejected, because it would
  duplicate the C# protocol work we already have. MQTT-discovery bridge reuses RoombaNet as-is.

### D. Nice-to-have / stretch
- [ ] Prometheus metrics, structured logs.
- [ ] Web dashboard **only if** wanted later (kept separate from the HA path).
- [ ] Tie in firmware/OTA tooling from the parent folder for a "robot admin" mode.

---

## 4. Quick repro (local control from scratch)
```bash
# discovery (unicast works cross-subnet; broadcast only same subnet)
printf 'irobotmcs' | nc -u -w3 192.168.20.65 5678      # or: RoombaNet.Cli discover --target 192.168.20.0/24
# control: MQTT/TLS :8883, user=BLID, pass=stored pw, clientId=BLID, publish to 'cmd'
#   {"command":"find","time":<unix>,"initiator":"localApp"}
```
Working reference scripts from this session: `../../get_password.py`, and the paho snippets in the
session scratchpad (connect + `find`).

## 5. Open questions to settle before building
1. Where does the bridge run (host on the IoT VLAN, or rely on the router's main→IoT route)?
2. Which MQTT broker — HA's built-in Mosquitto add-on, or a standalone one?
3. Keep the REST API as a debug surface, or fully remove it?
4. One bridge process for both robots, or one per robot? (single-connection constraint is per-robot,
   so one process multiplexing is fine.)
