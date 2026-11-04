# REST API

`RoombaNet.Api` is an ASP.NET Core API for one or more robots. Robots are kept in a SQLite
registry, so the API does not need a config file per robot.

## Run

```bash
dotnet run --project src/RoombaNet.Api
```

The API listens on `http://localhost:5264`. The interactive API reference (Scalar) is at
`http://localhost:5264/scalar`, and the OpenAPI document is at `/openapi/v1.json`.

All robot endpoints take the robot BLID as the `robotId` query parameter. In the examples below,
set these first:

```bash
API=http://localhost:5264/api/roomba
BLID=1234567890ABCDEF
```

## Docker

The Dockerfile is in `src/RoombaNet.Api/`. Build from the repository root:

```bash
docker build -f src/RoombaNet.Api/Dockerfile -t roombanet-api .

docker run -d --name roombanet \
  -p 8080:8080 \
  -v "$PWD/data:/data" \
  -e RobotRegistry__DatabasePath=/data/roombanet.db \
  -e RobotRegistry__EncryptionKey="$(openssl rand -base64 32)" \
  roombanet-api
```

In the container the API listens on port 8080. The container runs as user `1000`, so the
mounted `data` directory must be writable by that user.

| Variable | Default | Meaning |
|---|---|---|
| `RobotRegistry__DatabasePath` | `roombanet.db` in the app directory | SQLite file for the registry. Put it on a volume to keep it. |
| `RobotRegistry__EncryptionKey` | none | Base64 32-byte key. When set, robot passwords are encrypted in the database. Keep the same key across restarts. |

The image does not include any `secrets.*.json` file.

## Robot registry

```bash
# Discover robots and save them. target is repeatable: an IP or a CIDR range.
curl "$API/discovery?save=true"
curl "$API/discovery?save=true&target=192.168.20.0/24"

# List, get, add, update, delete
curl "$API/robots"
curl "$API/robots/$BLID"
curl -X POST "$API/robots" -H "Content-Type: application/json" \
  -d '{"blid":"'$BLID'","name":"Roomba","ip":"192.168.20.65","password":"robot-password"}'
curl -X PUT "$API/robots/$BLID" -H "Content-Type: application/json" -d '{"ip":"192.168.20.70"}'
curl -X DELETE "$API/robots/$BLID"

# Read the password from a robot in pairing mode (API host on the robot's access point)
curl -X POST "$API/robots/$BLID/pair"       # reads and stores it
curl -X POST "$API/robots/$BLID/password"   # reads it, does not store it
```

A robot record has `blid`, `name`, `ip`, `port` (default 8883), `password`, and `useTls`
(default `true`). Set `useTls` to `false` and `ip` to the proxy host to reach the robot through
the proxy's plain listener.

## Status

```bash
curl "$API/status?robotId=$BLID"            # full state snapshot
curl "$API/status/summary?robotId=$BLID"    # human-readable summary
curl "$API/status/firmware?robotId=$BLID"   # firmware and module versions
curl -N "$API/status/stream?robotId=$BLID"  # live updates, Server-Sent Events
curl "$API/health"
```

## Commands

```bash
curl -X POST "$API/start?robotId=$BLID"
curl -X POST "$API/dock?robotId=$BLID"
curl -X POST "$API/cancel?robotId=$BLID&dock=true"
curl -X POST "$API/commands/spot?robotId=$BLID"
curl -X POST "$API/commands/raw/washpad?robotId=$BLID"
curl "$API/commands"                         # list of command names
```

Shortcut endpoints: `start`, `stop`, `pause`, `resume`, `dock`, `find`, `evac`, `reset`, `train`,
`cancel`. Any command from `GET /commands` also works as `POST /commands/{command}`.

Several commands in one call:

```bash
curl -X POST "$API/commands/batch?robotId=$BLID" -H "Content-Type: application/json" \
  -d '{"commands":["find","start"],"sequential":true}'
```

## Maps and room cleaning

```bash
curl "$API/maps?robotId=$BLID"
curl -X DELETE "$API/maps?robotId=$BLID"     # deletes ALL maps

curl -X POST "$API/clean-regions?robotId=$BLID" -H "Content-Type: application/json" \
  -d '{"pmapId":"<pmap-id>","regions":[{"regionId":"1"},{"regionId":"3"}],"ordered":true}'
```

Region `type` defaults to `rid` (a room). `userPmapvId` selects a map version.

## Settings

All settings are `POST $API/settings/<name>?robotId=$BLID` with a JSON body.

| Endpoint | Body | Values |
|---|---|---|
| `child-lock` | `{"enable":true}` | |
| `bin-pause` | `{"enable":true}` | |
| `edge-clean` | `{"enable":true}` | |
| `eco-charge` | `{"enable":true}` | |
| `perimeter-pass` | `{"enable":true}` | |
| `map-learning` | `{"enable":true}` | |
| `continuous-map-learning` | `{"enable":true}` | |
| `map-upload` | `{"enable":false}` | |
| `cleaning-passes` | `{"passes":2}` | `1` one pass, `2` two passes, `3` auto |
| `cleaning-power` | `{"power":2}` | `0` eco, `1` auto, `2` performance |
| `volume` | `{"value":50}` | `0` to `100` |
| `bin-type-detect` | `{"value":1}` | raw integer |
| `timezone` | `{"value":"Europe/Ljubljana"}` | IANA name |
| `language` | `{"value":"en-US_1"}` | voice language code |
| `name` | `{"name":"Dusty"}` | |
| `rank-overlap` (Braava) | `{"value":67}` | `0` to `100` percent |
| `liquid-amount` (Braava) | `{"value":2}` | `1` eco, `2` standard, `3` ultra |
| `charging-light-pattern` (Braava) | `{"value":1}` | `0` docking and charging, `1` docking only, `2` off |

`GET $API/settings/available?robotId=$BLID` lists the settings that this robot reports.

## Weekly schedule

```bash
curl "$API/settings/schedule?robotId=$BLID"

curl -X POST "$API/settings/schedule?robotId=$BLID" -H "Content-Type: application/json" \
  -d '{"entries":[{"days":[1,2,3,4,5],"time":"09:30"},{"days":[6],"time":"11:00"}]}'
```

Days are numbers: `0` is Sunday, `6` is Saturday. The time is `HH:MM`, 24-hour. The list
replaces the whole schedule. An empty list clears it.

## Wi-Fi setup

With the API host on the robot's access point:

```bash
curl -X POST "$API/wifi/configure?robotId=$BLID" -H "Content-Type: application/json" \
  -d '{"ssid":"YourNetwork","password":"YourWiFiPassword"}'
```

## Proxy

Set `Proxy:Enabled` to `true` to run the MQTT proxy inside the API. It serves every robot in the
registry that has a password. See [MQTT proxy](proxy.md).
