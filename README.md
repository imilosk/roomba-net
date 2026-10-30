# RoombaNet

A .NET application for local control of iRobot devices via MQTT, without relying on cloud services.

## 🤖 Compatibility

### Tested Devices
- **Roomba i3** ✅ Fully tested and confirmed working
- **Braava jet m6** 🟡 Discovery and status confirmed (firmware sanmarino 22.29.6); commands and mop settings not yet exercised

### Potentially Compatible Models
Based on MQTT protocol support, the following models should work but are untested:

**i-series** (entry to mid-tier with smart features)
- i1, i2, i3, i4, i5, i6, i7, i8

**j-series** (advanced with object recognition)
- j7, j7+, j9, j9+

**s-series** (premium with D-shaped design)
- s9, s9+

**900 series** (older premium models)
- 960, 980

**600 series** (budget models with Wi-Fi)
- 690, 695, 698 (if equipped with Wi-Fi)

**Braava** (mopping robots)
- Jet m6 (see Tested Devices)

**Note**: Models without Wi-Fi connectivity or those using only Bluetooth are not supported.

### Connection Requirements
- Local network connectivity required
- MQTT protocol support (most Wi-Fi enabled Roombas from 2016+)
- No cloud services required - all communication stays on your local network
- **⚠️ Single connection limit**: Roombas only allow one active MQTT connection at a time. If you start a second instance (CLI, API, or mobile app), the previous connection will be disconnected. This is a hardware limitation of the Roomba, not this application.

## 🌟 Features

### Core Capabilities
- **CLI Tool** — full local control from the terminal (AOT-compiled, single binary)
- **RESTful API** — OpenAPI/Scalar documented
- **MCP Server** — control the Roomba from any AI assistant (Claude, etc.) over the Model Context Protocol
- **Real-time Status Streaming**: Server-Sent Events (SSE) for live status updates
- **Map Management**: list and clear persistent maps, clean specific rooms/zones
- **Local Communication**: Direct MQTT connection to your Roomba
- **Privacy-Focused**: All communication stays on your local network — **no cloud, ever**

### Supported Commands
- `find` - Make the Roomba beep to locate it
- `start` - Start a cleaning mission
- `stop` - Stop the current cleaning mission
- `pause` - Pause the cleaning mission
- `resume` - Resume a paused mission
- `dock` - Return to charging dock
- `evac` / `stopevac` - Empty bin into Clean Base / stop evacuation (auto-evacuation models only)
- `reset` - Reset Roomba to factory settings
- `train` - Start a training run for mapping
- `spot` - Spot clean
- `wake` / `sleep` / `off` - Power management
- `tidy` - Tidy-up clean
- `skip` - Skip the current area/room
- `wlapon` / `wlapoff` - Turn the Wi-Fi radio on/off
- `raw <command>` - Send any other device-specific command (e.g. `washpad`, `drypad`, `flushsluice`)

### Maps & Region Cleaning
- List persistent maps (pmaps) stored on the robot
- **Clear all maps** to force a fresh remap — fully local, no cloud
- Clean specific rooms/zones within a map

### Settings
- Child lock, bin pause, edge cleaning
- Cleaning passes (one / two / auto)
- Cleaning power (eco / auto carpet-boost / performance)
- Rename the robot
- Braava: mopping overlap, liquid amount, charging light pattern

## 🚀 Getting Started

### Prerequisites

- .NET 10.0 or later
- Roomba connected to your local network
- Roomba BLID and password (see configuration section)

### Installation

```bash
git clone https://github.com/imilosk/RoombaNet.git
cd RoombaNet
dotnet build
```

### Configuration

#### Step 1: Discover Your Roomba

First, find your Roomba on the network to get its IP address and BLID:

```bash
dotnet run --project src/RoombaNet.Cli discover
```

This will display information like:
```
Found 1 Roomba(s):

Name:          My Roomba
BLID:          1234567890ABCDEF
IP Address:    192.168.1.100
Hostname:      Roomba-1234567890ABCDEF
MAC Address:   AA:BB:CC:DD:EE:FF
SKU/Model:     i355020
```

#### Step 2: Get Your Roomba Password

The process varies by model:

**For i3, i3+, and similar models:**
1. Press and hold both the **HOME** and **SPOT CLEAN** buttons for about 2 seconds until you hear a sound and the CLEAN button turns blue
2. The Roomba will create its own Wi-Fi network (e.g., "Roomba-XXXXXXXX")
3. Connect your computer to the Roomba's Wi-Fi network
4. Run discovery again to get the Roomba's IP on its own network:
   ```bash
   dotnet run --project src/RoombaNet.Cli discover
   ```
5. Get the password using the new IP address:
   ```bash
   dotnet run --project src/RoombaNet.Cli get password --ip 192.168.10.1
   ```
   (The IP is typically `192.168.10.1` on the Roomba's network)
6. Reconnect to your regular Wi-Fi network

**For other models:**
1. Place Roomba on the dock
2. Hold the HOME button for about 2 seconds until it plays a series of tones (about 6 beeps)
3. Run the command immediately:
   ```bash
   dotnet run --project src/RoombaNet.Cli get password --ip 192.168.1.100
   ```

Replace `192.168.1.100` with your Roomba's IP address from step 1.

#### Step 3: Create Configuration File

Create a `secrets.Development.json` file in the `src/RoombaNet.Settings/` directory with the information from steps 1 and 2:

```json
{
  "RoombaSettings": {
    "Ip": "192.168.1.100",
    "Port": 8883,
    "Blid": "1234567890ABCDEF",
    "Password": "your_roomba_password_from_step_2"
  }
}
```

### Configure Wi-Fi

If you don't want to use the iRobot app, you can configure your Roomba's Wi-Fi settings directly using this CLI:

1. **Put Roomba in configuration mode** (same as Step 2 above for password)
2. **Connect your computer to the Roomba's Wi-Fi network**
3. **Run the configure-wifi command:**
   ```bash
   dotnet run --project src/RoombaNet.Cli configure-wifi \
     --ssid "YourWiFiNetwork" \
     --password "YourWiFiPassword"
   ```

Optional parameters:
- `--robot-name "My Roomba"` - Set a custom name
- `--timezone "America/New_York"` - Set IANA timezone
- `--country "US"` - Set country code

After configuration, the Roomba will connect to your Wi-Fi network within a minute. **Important:** Do not press any buttons on the Roomba while it's connecting to Wi-Fi. Once the configuration is complete, the Roomba will exit configuration mode and you'll hear a confirmation beep. You can then reconnect your computer to your regular network.

## 📖 Usage Examples

### CLI Commands

Run the CLI tool with different commands:

```bash
# Discover Roombas on your network
dotnet run --project src/RoombaNet.Cli discover

# Robots on another subnet/VLAN (broadcast can't reach them): query an IP or range directly
dotnet run --project src/RoombaNet.Cli discover --target 192.168.20.0/24

# Configure Wi-Fi settings (connect to Roomba's network first)
dotnet run --project src/RoombaNet.Cli configure-wifi --ssid "YourNetwork" --password "YourPassword"

# Get Roomba password (hold HOME button for 2 seconds first)
dotnet run --project src/RoombaNet.Cli get password

# Make the Roomba beep (find command)
dotnet run --project src/RoombaNet.Cli execute find

# Start cleaning
dotnet run --project src/RoombaNet.Cli execute start

# Return to dock
dotnet run --project src/RoombaNet.Cli execute dock

# Subscribe to all Roomba events and messages
dotnet run --project src/RoombaNet.Cli subscribe

# Get current settings
dotnet run --project src/RoombaNet.Cli get

# One-shot status snapshot (battery, phase, bin, position, map count)
dotnet run --project src/RoombaNet.Cli status

# Commands: spot clean, tidy, skip a room, power off, toggle Wi-Fi radio
dotnet run --project src/RoombaNet.Cli command spot
dotnet run --project src/RoombaNet.Cli command skip
dotnet run --project src/RoombaNet.Cli command wifi-off
# Send any raw command
dotnet run --project src/RoombaNet.Cli command raw washpad

# Maps: list, and clear all (local-only — forces a fresh remap)
dotnet run --project src/RoombaNet.Cli maps list
dotnet run --project src/RoombaNet.Cli maps clear

# Clean specific rooms/zones in a map
dotnet run --project src/RoombaNet.Cli clean-regions --pmap <pmap_id> --regions 1 3 5

# Settings: cleaning power (0 eco / 1 auto / 2 performance), edge clean, rename
dotnet run --project src/RoombaNet.Cli setting cleaningpower 2
dotnet run --project src/RoombaNet.Cli setting edgeclean on
dotnet run --project src/RoombaNet.Cli setting name "Dusty"

# Get help and see all available commands
dotnet run --project src/RoombaNet.Cli --help
```

### MQTT Proxy (share one robot connection across API, MCP, and CLI)

iRobot robots accept only **one** local MQTT connection at a time — a second client kicks the
first. The proxy holds that single upstream connection per robot and fans it out to many clients.
A client connects to the proxy exactly as it would to the robot (TLS on 8883, username =
client-id = BLID, password = the robot's local password); the only thing that changes is the host IP.

```bash
# Run the proxy standalone from the CLI (proxies every robot in Robots:Profiles)
dotnet run --project src/RoombaNet.Cli proxy

# Watch every robot at once via the plain localhost listener (aggregated, no auth)
mosquitto_sub -h 127.0.0.1 -p 1883 -t 'roomba/#' -v
```

Or enable it inside the API by setting `Proxy:Enabled` to `true` (robots are taken from the API
robot registry). See [docs/proxy.md](docs/proxy.md) for the architecture, config keys, and the
two listeners.

Example output when using the `execute` command:
```
🔍 Sending 'find' command to Roomba...
✅ Command 'find' sent successfully
🤖 Roomba should now be beeping!
```

Example output when using the `subscribe` command:
```
👂 Listening for Roomba messages... (Press Ctrl+C to exit)
📥 Message received:
   Topic: wifistat
   Payload: {"wifi_state":{"connected":true,"signal":-45}}
   Timestamp: 2025-11-23 14:23:15
   --------------------------------------------------
```

### API Usage

#### Option 1: Run with .NET
```bash
dotnet run --project src/RoombaNet.Api
```

#### Option 2: Run with Docker
```bash
# Build the image
docker build -t roombanet-api .

# Run the container
docker run -d \
  --name roombanet \
  -p 8080:8080 \
  -e RobotRegistry__DatabasePath=/data/roombanet.db \
  -e RobotRegistry__EncryptionKey=base64_32_byte_key \
  roombanet-api

# View logs
docker logs -f roombanet

# Stop the container
docker stop roombanet
```

Notes:
- `RobotRegistry__DatabasePath` is optional; defaults to `roombanet.db` in the app directory.
- `RobotRegistry__EncryptionKey` is optional but recommended; use a base64-encoded 32-byte key to encrypt stored passwords.

#### Robot Registry (Multi-Robot)
```bash
# Discover robots and save them to the registry
curl "http://localhost:8080/api/roomba/discovery?save=true"

# Include robots on another subnet/VLAN (target is repeatable: IP or CIDR range)
curl "http://localhost:8080/api/roomba/discovery?save=true&target=192.168.20.0/24"

# List registered robots
curl http://localhost:8080/api/roomba/robots

# Pair a robot to store its password (put robot in pairing mode first)
curl -X POST http://localhost:8080/api/roomba/robots/{blid}/pair

# Configure Wi-Fi (connect to Roomba's AP first)
curl -X POST "http://localhost:8080/api/roomba/wifi/configure?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{"ssid":"YourNetwork","password":"YourPassword"}'
```

#### Single Commands
```bash
# Start cleaning
curl -X POST "http://localhost:8080/api/roomba/commands/start?robotId={blid}"

# Find Roomba (make it beep)
curl -X POST "http://localhost:8080/api/roomba/commands/find?robotId={blid}"

# Return to dock
curl -X POST "http://localhost:8080/api/roomba/commands/dock?robotId={blid}"

# Evacuate bin (Clean Base models only)
curl -X POST "http://localhost:8080/api/roomba/evac?robotId={blid}"

# Get available commands
curl http://localhost:8080/api/roomba/commands
```

#### Batch Commands
```bash
# Execute multiple commands sequentially
curl -X POST "http://localhost:8080/api/roomba/commands/batch?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{
    "commands": ["start", "pause", "resume"],
    "sequential": true
  }'

# Execute commands in parallel
curl -X POST "http://localhost:8080/api/roomba/commands/batch?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{
    "commands": ["find", "start"],
    "sequential": false
  }'
```

#### Settings Management
```bash
# Enable child lock
curl -X POST "http://localhost:8080/api/roomba/settings/child-lock?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{"enable": true}'

# Enable bin pause
curl -X POST "http://localhost:8080/api/roomba/settings/bin-pause?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{"enable": true}'

# Set cleaning passes (1, 2, or 3)
curl -X POST "http://localhost:8080/api/roomba/settings/cleaning-passes?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{"passes": 2}'

# Braava: set mopping overlap (rankOverlap)
curl -X POST "http://localhost:8080/api/roomba/settings/rank-overlap?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{"value": 1}'
# 0-100 percentage

# Braava: set liquid amount (padWetness)
curl -X POST "http://localhost:8080/api/roomba/settings/liquid-amount?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{"value": 1}'
# 1 = Eco, 2 = Standard, 3 = Ultra

# Braava: set charging light pattern (chrgLrPtrn)
curl -X POST "http://localhost:8080/api/roomba/settings/charging-light-pattern?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{"value": 1}'
# 0 = Docking & charging, 1 = Docking only, 2 = No status lights
```

#### Wi-Fi Configuration
```bash
# Configure Wi-Fi (connect to Roomba's AP first)
curl -X POST "http://localhost:8080/api/roomba/wifi/configure?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{"ssid":"YourNetwork","password":"YourPassword"}'
```

#### Real-time Status Streaming
```bash
# Current status snapshot (battery, mission, bin, position, map count)
curl "http://localhost:8080/api/roomba/status?robotId={blid}"

# Stream live status updates (Server-Sent Events)
curl "http://localhost:8080/api/roomba/status/stream?robotId={blid}"

# Get health check
curl http://localhost:8080/api/roomba/health
```

#### Maps & Region Cleaning
```bash
# List persistent maps
curl "http://localhost:8080/api/roomba/maps?robotId={blid}"

# Delete ALL maps (local-only; the robot remaps on its next run)
curl -X DELETE "http://localhost:8080/api/roomba/maps?robotId={blid}"

# Clean specific rooms/zones within a map
curl -X POST "http://localhost:8080/api/roomba/clean-regions?robotId={blid}" \
  -H "Content-Type: application/json" \
  -d '{"pmapId":"<pmap_id>","regions":[{"regionId":"1","type":"rid"},{"regionId":"3","type":"rid"}]}'
```

## 🧠 AI Control (MCP Server)

`RoombaNet.Mcp` is a [Model Context Protocol](https://modelcontextprotocol.io) server that lets an
AI assistant (Claude Desktop, Claude Code, etc.) control your Roomba in natural language. It bridges
to the REST API over your local network — **no cloud involved**.

### Run it

1. Start the API (it must be reachable from the MCP server):
   ```bash
   dotnet run --project src/RoombaNet.Api   # serves on http://localhost:5264 by default
   ```
2. Register the MCP server with your AI client. Example for Claude Desktop / Claude Code
   (`claude_desktop_config.json` or `.mcp.json`):
   ```json
   {
     "mcpServers": {
       "roomba": {
         "command": "dotnet",
         "args": ["run", "--project", "/absolute/path/to/RoombaNet/src/RoombaNet.Mcp"],
         "env": { "ROOMBANET_API_URL": "http://localhost:5264" }
       }
     }
   }
   ```
   (Or build once and point `command` at the published `RoombaNet.Mcp` binary.)

### Available tools
`list_robots`, `discover_robots`, `get_status`, `start_cleaning`, `stop_cleaning`, `pause_cleaning`,
`resume_cleaning`, `dock`, `find`, `evacuate_bin`, `send_command`, `list_maps`, `clear_maps`,
`clean_regions`, `set_cleaning_power`, `set_cleaning_passes`, `set_child_lock`.

Then just ask: *"Send the Roomba to clean the kitchen and living room, then dock."*

## 📋 Roadmap

### Completed ✅
- [x] MQTT communication with Roomba
- [x] CLI tool for all basic commands
- [x] Message subscription and monitoring
- [x] RESTful API
- [x] Real-time status streaming (SSE)
- [x] One-shot status snapshot (CLI `status` / `GET /status`)
- [x] Batch command execution
- [x] Settings management (child lock, bin pause, passes, power, edge, name, Braava)
- [x] Map management: list + clear persistent maps (local, no cloud)
- [x] Room-specific / zone cleaning
- [x] MCP server for AI assistant control
- [x] Docker container support
- [x] Multi-robot support

### In Progress 🚧
- [ ] Web UI for browser-based control (https://github.com/imilosk/roomba-net-ui)
- [ ] Unit and integration tests

### Planned 📝
- [ ] Keep-out zone management
- [ ] Cleaning history and analytics
- [ ] Schedule management UI
- [ ] Home Assistant MQTT-discovery bridge

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Ways to Contribute
- **Test on your Roomba model**: Help expand the compatibility list
- **Report bugs**: Open an issue with details about your setup
- **Feature requests**: Suggest new features or improvements
- **Code contributions**: Submit PRs with bug fixes or new features
- **Documentation**: Improve README, add examples, or write guides

### Testing Other Models
If you have a Roomba model not listed as tested, we'd love your help! The easiest way to contribute compatibility data:
1. Run the API or CLI tool with your Roomba
2. Check if basic commands work (find, start, stop, dock)
3. Report your results in a GitHub issue with your model number

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- iRobot for creating the robots
- The reverse engineering community for MQTT protocol documentation
    - https://github.com/koalazak/dorita980
    - https://github.com/pschmitt/roombapy
    - https://github.com/v6ak/Roomba980-Python-WiFi-only
- All contributors and testers

## ⚠️ Disclaimer

This project is not affiliated with, endorsed by, or sponsored by iRobot Corporation. Use at your own risk. The software is provided "as is" without warranty of any kind, express or implied.

**Important Notes:**
- Some operations may void your warranty
