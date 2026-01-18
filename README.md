# RoombaNet

A .NET application for local control of iRobot devices via MQTT, without relying on cloud services.

## 🤖 Compatibility

### Tested Devices
- **Roomba i3** ✅ Fully tested and confirmed working

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
- Jet m6

**Note**: Models without Wi-Fi connectivity or those using only Bluetooth are not supported.

### Connection Requirements
- Local network connectivity required
- MQTT protocol support (most Wi-Fi enabled Roombas from 2016+)
- No cloud services required - all communication stays on your local network
- **⚠️ Single connection limit**: Roombas only allow one active MQTT connection at a time. If you start a second instance (CLI, API, or mobile app), the previous connection will be disconnected. This is a hardware limitation of the Roomba, not this application.

## 🌟 Features

### Core Capabilities
- **CLI Tool**
- **RESTful API**
- **Real-time Status Streaming**: Server-Sent Events (SSE) for live status updates
- **Local Communication**: Direct MQTT connection to your Roomba
- **Privacy-Focused**: All communication stays on your local network

### Supported Commands
- `find` - Make the Roomba beep to locate it
- `start` - Start a cleaning mission
- `stop` - Stop the current cleaning mission
- `pause` - Pause the cleaning mission
- `resume` - Resume a paused mission
- `dock` - Return to charging dock
- `evac` - Empty bin into Clean Base (models with auto-evacuation only)
- `reset` - Reset Roomba to factory settings
- `train` - Start a training run for mapping

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

# Change settings (e.g., enable two-pass cleaning)
dotnet run --project src/RoombaNet.Cli setting twoPass true

# Get help and see all available commands
dotnet run --project src/RoombaNet.Cli --help
```

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
# Stream live status updates (Server-Sent Events)
curl "http://localhost:8080/api/roomba/status/stream?robotId={blid}"

# Get health check
curl http://localhost:8080/api/roomba/health
```

## 📋 Roadmap

### Completed ✅
- [x] MQTT communication with Roomba
- [x] CLI tool for all basic commands
- [x] Message subscription and monitoring
- [x] RESTful API
- [x] Real-time status streaming (SSE)
- [x] Batch command execution
- [x] Settings management
- [x] Docker container support
- [x] Multi-robot support

### In Progress 🚧
- [ ] Web UI for browser-based control (https://github.com/imilosk/roomba-net-ui)
- [ ] API documentation (Swagger/OpenAPI)
- [ ] Unit and integration tests

### Planned 📝
- [ ] Room-specific cleaning
- [ ] Zone cleaning support
- [ ] Keep-out zone management
- [ ] Cleaning history and analytics
- [ ] Schedule management UI

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
