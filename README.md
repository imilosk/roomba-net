# RoombaNet

Local control for iRobot robots, written in .NET.

RoombaNet talks to the robot directly over its local MQTT interface. It does not use the iRobot
cloud, and it keeps working when the robot has no internet access at all.

It has four parts:

| Part | Project | What it does |
|---|---|---|
| CLI | `src/RoombaNet.Cli` | Control a robot from the terminal. AOT-compiled to a single binary. |
| REST API | `src/RoombaNet.Api` | HTTP API with OpenAPI docs, a robot registry, and live status over SSE. |
| MCP server | `src/RoombaNet.Mcp` | Lets an AI assistant control the robots through the API. |
| MQTT proxy | `src/RoombaNet.Proxy` | Shares one robot connection between many clients. |

## Features

- Discovery on the local network, also across subnets.
- Password retrieval and Wi-Fi setup, so you do not need the iRobot app.
- All mission commands: start, stop, pause, resume, dock, spot, evac, and more.
- Status snapshot, human-readable summary, firmware versions, and a live event stream.
- Persistent maps: list maps, clear all maps, and clean selected rooms.
- Weekly schedule: read and write. The schedule is stored and run on the robot.
- Settings: cleaning passes, suction power, edge clean, child lock, bin pause, volume,
  timezone, language, and more.
- Braava settings: mopping overlap, liquid amount, and charging light pattern.
- Several robots at once: CLI profiles, an API registry, and a multi-robot proxy.

## Compatibility

| Robot | Firmware | Status |
|---|---|---|
| Roomba i3+ | daredevil 2.6.0 | Tested. All features work. |
| Braava jet m6 | sanmarino 22.29.6 | Discovery and status tested. Commands and mop settings are only partly tested. |

Other Wi-Fi robots that use the same local MQTT protocol should also work, but are not tested:
i-series, j-series, s-series, the 960 and 980, and the Wi-Fi 600 series. Robots without
Wi-Fi are not supported. If you test another model, please open an issue with the results.

### The one-connection limit

A robot accepts only one local MQTT connection at a time. When a second client connects, the
robot drops the first one. This includes the iRobot app. Two RoombaNet tools pointed at the same
robot will keep disconnecting each other.

To use the CLI, API, and MCP server at the same time, run the [MQTT proxy](docs/proxy.md). It
keeps the single connection to each robot and lets many clients share it.

## Quick start

You need the .NET 10 SDK (10.0.403 or newer), and a robot on your network.

```bash
git clone https://github.com/imilosk/roomba-net.git
cd roomba-net
dotnet build
```

1. Find the robot and note its IP and BLID:

   ```bash
   dotnet run --project src/RoombaNet.Cli discover
   ```

2. Get the robot password. Put the robot in pairing mode and connect your computer to the
   robot's own Wi-Fi network first, see
   [Getting started](docs/getting-started.md#2-get-the-robot-password).

   ```bash
   dotnet run --project src/RoombaNet.Cli get password
   ```

3. Create `src/RoombaNet.Settings/secrets.Development.json`:

   ```json
   {
     "RoombaSettings": {
       "Ip": "192.168.1.100",
       "Blid": "1234567890ABCDEF",
       "Password": "your-robot-password"
     }
   }
   ```

4. Check that it works:

   ```bash
   dotnet run --project src/RoombaNet.Cli status
   dotnet run --project src/RoombaNet.Cli command find
   ```

Secrets files (`secrets.*.json`) are in `.gitignore`. Do not commit them.

## Documentation

| Document | Content |
|---|---|
| [Getting started](docs/getting-started.md) | Discovery, passwords, configuration, Wi-Fi setup, several robots. |
| [CLI](docs/cli.md) | All CLI commands with examples. |
| [REST API](docs/api.md) | Endpoints, robot registry, Docker. |
| [MCP server](docs/mcp.md) | Connecting an AI assistant. |
| [MQTT proxy](docs/proxy.md) | Sharing one robot connection between many clients. |
| [OpenWrt network setup](docs/openwrt.md) | Isolating the robots from the cloud, and giving them correct time. |
| [Mission observations](docs/MISSION_OBSERVATIONS.md) | What the robot reports during a real mission. |
| [Protocol notes and roadmap](docs/ROOMBA_NOTES_AND_ROADMAP.md) | Verified protocol details and plans. |

## Project layout

```
src/
  RoombaNet.Core              Robot services: commands, settings, maps, discovery, Wi-Fi
  RoombaNet.Transport.Mqtt    MQTT connection handling
  RoombaNet.Transport.Tls     TLS connection used for password retrieval
  RoombaNet.Settings          Shared configuration (robot connection settings, profiles)
  RoombaNet.Cli               Command-line tool
  RoombaNet.Api               REST API
  RoombaNet.Mcp               MCP server (talks to the REST API)
  RoombaNet.Proxy             Multi-robot MQTT proxy
  RoombaNet.Proxy.Tests       Proxy tests
```

Build and test:

```bash
dotnet build RoombaNet.slnx
dotnet test RoombaNet.slnx
```

## Roadmap

Done:

- Local MQTT control, CLI, REST API, SSE status stream
- Settings, maps, room cleaning, weekly schedule, firmware info
- MCP server
- Docker image for the API
- Several robots, robot registry, MQTT proxy

In progress:

- Web UI: [roomba-net-ui](https://github.com/imilosk/roomba-net-ui)
- More unit and integration tests

Planned:

- Keep-out zones
- Cleaning history
- Home Assistant MQTT discovery

## Contributing

Pull requests are welcome. The most useful help is testing on robot models that are not in the
compatibility table. Run `status` and a few basic commands (`find`, `start`, `stop`, `dock`), and
report the model, firmware, and results in an issue.

## Acknowledgments

This project builds on the protocol work of:

- [dorita980](https://github.com/koalazak/dorita980)
- [roombapy](https://github.com/pschmitt/roombapy)
- [Roomba980-Python](https://github.com/v6ak/Roomba980-Python-WiFi-only)

## License

MIT. See [LICENSE](LICENSE).

## Disclaimer

This project is not affiliated with, endorsed by, or sponsored by iRobot Corporation. It is
provided as is, without warranty of any kind. Use it at your own risk.
