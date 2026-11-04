# CLI

The CLI connects straight to one robot and runs a single command. Set up the robot connection
first, see [Getting started](getting-started.md).

```bash
dotnet run --project src/RoombaNet.Cli -- <command> [options]
dotnet run --project src/RoombaNet.Cli -- --help
dotnet run --project src/RoombaNet.Cli -- <command> --help
```

The examples below use `roombanet` as a short name for `dotnet run --project src/RoombaNet.Cli --`,
or for the published binary.

## Build a single binary

The CLI is published with native AOT, so the result is one executable with no .NET runtime needed:

```bash
dotnet publish src/RoombaNet.Cli -c Release -r osx-arm64   # or linux-x64, win-x64, ...
```

The binary and its `appsettings.json` are written to
`src/RoombaNet.Cli/bin/Release/net10.0/<rid>/publish/`. Put your `secrets.<Environment>.json` next
to the binary.

## Choosing a robot

Every command takes `-r` / `--robot <profile>`. The profile names come from `Robots:Profiles`
in the configuration, see [Several robots](getting-started.md#several-robots).

```bash
roombanet -r braava status
```

Remember that the robot allows only one MQTT connection. Running the CLI disconnects the iRobot
app, the API, or any other client that is connected straight to the same robot. To avoid this,
point the CLI at the [MQTT proxy](proxy.md).

## Command overview

| Command | Alias | What it does |
|---|---|---|
| `discover` | | Find robots on the network. |
| `get password` | `get pwd` | Read the password from a robot in pairing mode. |
| `set-password` | | Set a new password on a robot in pairing mode. |
| `configure-wifi` | | Send Wi-Fi settings to a robot in pairing mode. |
| `status` | `state` | Print a status snapshot and exit. |
| `subscribe` | | Print every message from the robot until Ctrl+C. |
| `command <name>` | `cmd` | Send a mission or device command. |
| `setting <name>` | `set` | Change a setting. |
| `schedule` | | Show or change the weekly schedule. |
| `maps` | | List or clear persistent maps. |
| `clean-regions` | `clean-rooms` | Clean selected rooms of a map. |
| `proxy` | | Run the multi-robot MQTT proxy. |

## Setup commands

```bash
# Discover robots with broadcast. Add --target for other subnets (IP or CIDR, repeatable).
roombanet discover
roombanet discover --target 192.168.20.0/24 --timeout 10

# With the computer on the robot's own access point (192.168.10.1):
roombanet get password
roombanet set-password --password "new-password"
roombanet configure-wifi --ssid "YourNetwork" --password "YourWiFiPassword" \
  --robot-name "My Roomba" --timezone "Europe/Ljubljana" --country "SI"
```

## Status

```bash
roombanet status
```

`status` waits a few seconds for the robot state and prints a summary: current activity, battery,
bin, mission errors, position, Wi-Fi signal, map count, firmware versions, and Braava pad and mop
settings when they apply.

```bash
roombanet subscribe
```

`subscribe` prints every MQTT message with its topic and payload. It is useful to see what the
robot reports while it works. See [Mission observations](MISSION_OBSERVATIONS.md) for an example.

## Commands

```bash
roombanet command start
roombanet command dock
```

| Command | Alias | What it does |
|---|---|---|
| `find` | `locate` | Play a sound. |
| `start` | `clean` | Start a cleaning mission. |
| `stop` | `halt` | Stop the mission. |
| `pause` | | Pause the mission. |
| `resume` | `continue` | Resume a paused mission. |
| `dock` | `home` | Return to the dock. |
| `cancel` | | Stop and dock, so the mission does not resume later. |
| `spot` | | Spot clean. |
| `tidy` | | Tidy-up clean. |
| `skip` | | Skip the current room. |
| `train` | `map` | Start a mapping run. |
| `evac` | `empty` | Empty the bin into the Clean Base. |
| `stopevac` | | Stop emptying the bin. |
| `wake` / `sleep` / `off` | | Power state. |
| `reset` | `reboot` | Reset the robot. |
| `wifi-on` / `wifi-off` | `wlapon` / `wlapoff` | Turn the robot's Wi-Fi radio on or off. |

Braava commands: `washpad`, `stoppadwash`, `drypad`, `stoppaddry`, `flushsluice`, `flrefill`,
`stopflrefill`, `spray`, `startvibrate`, `stopvibrate`.

Any other command string can be sent as is:

```bash
roombanet command raw <command>
```

## Settings

```bash
roombanet setting childlock enable
roombanet setting cleaningpasses twopass
roombanet setting cleaningpower performance
roombanet setting name "Dusty"
roombanet setting timezone Europe/Ljubljana
```

| Setting | Alias | Value |
|---|---|---|
| `childlock` | | `enable` / `disable` (also `on` / `off`, `1` / `0`) |
| `binpause` | | `enable` / `disable` |
| `edge` | `edgeclean` | `enable` / `disable` |
| `cleaningpasses` | `pass` | `onepass` (1), `twopass` (2), `roomsizeclean` (3, auto) |
| `cleaningpower` | `power` | `eco` (0), `auto` (1, carpet boost), `performance` (2) |
| `ecocharge` | | `enable` / `disable`. Slower charging, longer battery life. |
| `perimeter` | `perimeterpass` | `enable` / `disable` |
| `maplearning` | | `enable` / `disable` |
| `cl` | `continuouslearning` | `enable` / `disable` |
| `mapupload` | | `enable` / `disable`. Allows map upload to the cloud. |
| `bintype` | `bintypedetect` | Raw integer. |
| `volume` | | `0` to `100` |
| `timezone` | `tz` | IANA name, for example `Europe/Ljubljana` |
| `lang` | `language` | Voice language code, for example `en-US_1` |
| `name` | | New robot name |

The Braava settings (mopping overlap, liquid amount, charging light) are available in the
[REST API](api.md#settings).

## Weekly schedule

The schedule is stored on the robot and runs on the robot. It works without the cloud and
without RoombaNet running.

```bash
roombanet schedule show
roombanet schedule set weekdays 09:30
roombanet schedule set mon,wed,fri 14:00
roombanet schedule clear sat,sun
roombanet schedule clear            # clears all days
```

Days are a comma list (`mon,tue,...`) or `daily`, `weekdays`, `weekends`. The time is `HH:MM`
in 24-hour format, in the robot's timezone. The robot needs the correct time for the schedule to
run on time. Without internet access, give it a local time server, see
[OpenWrt network setup](openwrt.md#4-time-ntp).

## Maps and room cleaning

```bash
roombanet maps list
roombanet maps clear          # deletes ALL maps; the robot remaps on its next run

roombanet clean-regions --pmap <pmap-id> --regions 1 3 5
roombanet clean-regions --pmap <pmap-id> --regions 1 3 5 --unordered
```

`maps list` shows the map IDs and the room (region) IDs. `clean-regions` cleans rooms in the
given order. With `--unordered`, the robot picks the order. `--version <user_pmapv_id>` selects a
map version.

## Proxy

```bash
roombanet proxy
```

Runs the MQTT proxy in the foreground for every robot in `Robots:Profiles`. See
[MQTT proxy](proxy.md).
