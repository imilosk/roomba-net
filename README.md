# RoombaNet

A .NET application for local control of iRobot Roomba devices via MQTT, without relying on cloud services.

## 🤖 Compatibility

- **Tested on**: Roomba i3
- **Potentially compatible**: Other Roomba models with MQTT support (untested)
- **Connection**: Local network only - no cloud services required

## 🌟 Features

- **CLI Tool**: Command-line interface for direct Roomba control
- **API**: RESTful API for programmatic access (planned)
- **Web UI**: Browser-based interface (planned)
- **Local Communication**: Direct MQTT connection to your Roomba
- **Privacy-Focused**: All communication stays on your local network

## 🚀 Getting Started

### Prerequisites

- .NET 9.0 or later
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
dotnet run --project src/RoombaNet.Cli -- find

# Subscribe to all Roomba events and messages
dotnet run --project src/RoombaNet.Cli -- listen

# Get help and see all available commands
dotnet run --project src/RoombaNet.Cli -- --help
```

Example output when using the `find` command:
```
🔍 Sending 'find' command to Roomba...
✅ Command 'find' sent successfully
🤖 Roomba should now be beeping!
```

Example output when using the `listen` command:
```
👂 Listening for Roomba messages... (Press Ctrl+C to exit)
📥 Message received:
   Topic: wifistat
   Payload: {"wifi_state":{"connected":true,"signal":-45}}
   Timestamp: 2025-07-31 14:23:15
   --------------------------------------------------
```

### API Calls (Planned)

```bash
# Start cleaning
curl -X POST http://localhost:5000/api/roomba/start

# Find Roomba (make it beep)
curl -X POST http://localhost:5000/api/roomba/find

# Get Roomba status
curl -X GET http://localhost:5000/api/roomba/status

# Return to dock
curl -X POST http://localhost:5000/api/roomba/dock
```

### Web UI (Planned)

Access the web interface at `http://localhost:5000` for:
- Real-time status monitoring
- Remote control buttons
- Cleaning history
- Schedule management

## 📋 Roadmap

- [x] MQTT communication with Roomba
- [ ] CLI tool for basic commands
- [x] Message subscription and monitoring
- [ ] RESTful API
- [ ] Web UI
- [ ] Additional Roomba commands (start, stop, dock, etc.)
- [ ] Real-time status monitoring
- [ ] Cleaning history and analytics

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ⚠️ Disclaimer

This project is not affiliated with iRobot Corporation. Use at your own risk. The software is provided "as is" without warranty of any kind.