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

Create a `secrets.Development.json` file in the `src/RoombaNet.Settings/` directory:

```json
{
  "RoombaSettings": {
    "Ip": "192.168.1.100",
    "Port": 8883,
    "Blid": "your_roomba_blid",
    "Password": "your_roomba_password"
  }
}
```

## 📖 Usage Examples

### CLI Commands

Run the CLI tool with different commands:

```bash
# Make the Roomba beep (find command)
dotnet run --project src/RoombaNet.Cli -- execute find

# Start cleaning
dotnet run --project src/RoombaNet.Cli -- execute start

# Return to dock
dotnet run --project src/RoombaNet.Cli -- execute dock

# Subscribe to all Roomba events and messages
dotnet run --project src/RoombaNet.Cli -- subscribe

# Get current settings
dotnet run --project src/RoombaNet.Cli -- get

# Change settings (e.g., enable two-pass cleaning)
dotnet run --project src/RoombaNet.Cli -- setting twoPass true

# Get help and see all available commands
dotnet run --project src/RoombaNet.Cli -- --help
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
  -e RoombaSettings__Ip=192.168.1.100 \
  -e RoombaSettings__Port=8883 \
  -e RoombaSettings__Blid=your_roomba_blid \
  -e RoombaSettings__Password=your_roomba_password \
  roombanet-api

# View logs
docker logs -f roombanet

# Stop the container
docker stop roombanet
```

#### Single Commands
```bash
# Start cleaning
curl -X POST http://localhost:8080/api/roomba/commands/start

# Find Roomba (make it beep)
curl -X POST http://localhost:8080/api/roomba/commands/find

# Return to dock
curl -X POST http://localhost:8080/api/roomba/commands/dock

# Evacuate bin (Clean Base models only)
curl -X POST http://localhost:8080/api/roomba/evac

# Get available commands
curl http://localhost:8080/api/roomba/commands
```

#### Batch Commands
```bash
# Execute multiple commands sequentially
curl -X POST http://localhost:8080/api/roomba/commands/batch \
  -H "Content-Type: application/json" \
  -d '{
    "commands": ["start", "pause", "resume"],
    "executeInParallel": false
  }'

# Execute commands in parallel
curl -X POST http://localhost:8080/api/roomba/commands/batch \
  -H "Content-Type: application/json" \
  -d '{
    "commands": ["find", "start"],
    "executeInParallel": true
  }'
```

#### Settings Management
```bash
# Get current settings
curl http://localhost:8080/api/roomba/settings

# Update settings
curl -X PUT http://localhost:8080/api/roomba/settings \
  -H "Content-Type: application/json" \
  -d '{
    "twoPass": true,
    "carpetBoost": true,
    "vacHigh": false,
    "noAutoPasses": false,
    "openOnly": false
  }'
```

#### Real-time Status Streaming
```bash
# Stream live status updates (Server-Sent Events)
curl http://localhost:8080/api/roomba/status/stream

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
- [ ] Multi-robot support

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
    - https://github.com/imilosk/roomba-net-ui
- All contributors and testers

## ⚠️ Disclaimer

This project is not affiliated with, endorsed by, or sponsored by iRobot Corporation. Use at your own risk. The software is provided "as is" without warranty of any kind, express or implied.

**Important Notes:**
- Some operations may void your warranty