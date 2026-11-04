# Getting started

This guide takes you from a new robot to a working CLI setup. For the REST API, robots are
stored in a registry instead of a config file. See [REST API](api.md#robot-registry).

You need:

- The .NET 10 SDK, version 10.0.403 or newer (`dotnet --version`).
- A robot that is already on your Wi-Fi. If it is not, see [Set up Wi-Fi](#set-up-wi-fi-without-the-app).

All commands below run from the repository root. `dotnet run --project src/RoombaNet.Cli` can be
replaced by the published binary, see [CLI](cli.md#build-a-single-binary).

## 1. Find the robot

```bash
dotnet run --project src/RoombaNet.Cli discover
```

Example output:

```
Found 1 Roomba(s):

Name:          My Roomba
BLID:          1234567890ABCDEF
IP Address:    192.168.1.100
Hostname:      Roomba-1234567890ABCDEF
MAC Address:   AA:BB:CC:DD:EE:FF
SKU/Model:     i355020
```

Discovery uses UDP broadcast on port 5678. Broadcast does not cross subnets or VLANs. If the
robot is on another subnet, query its IP or the whole range directly:

```bash
dotnet run --project src/RoombaNet.Cli discover --target 192.168.20.0/24
```

Note the **IP** and the **BLID**. The BLID is the robot ID. It is also the MQTT username.

## 2. Get the robot password

The robot gives out its password only in pairing mode, over its own Wi-Fi access point.
RoombaNet always asks for the password at `192.168.10.1`, which is the robot's address on that
access point.

1. Put the robot in pairing mode. On the i3 and i3+, hold **HOME** and **SPOT CLEAN** for about
   2 seconds, until the robot plays a sound and the light turns blue. Other models use a
   different button combination; check the model's Wi-Fi setup instructions.
2. The robot starts its own Wi-Fi network, named like `Roomba-XXXXXXXX`. Connect your computer
   to it.
3. Get the password:

   ```bash
   dotnet run --project src/RoombaNet.Cli get password
   ```

4. Connect your computer back to your normal network.

With the API, `POST /api/roomba/robots/<blid>/pair` does the same and stores the password in the
registry. The API host must be connected to the robot's access point while you call it.

Some robots also give the password on the normal network (hold **HOME** for 2 seconds on the
dock). RoombaNet does not support that method yet.

## 3. Configure the CLI

The CLI reads its configuration from `src/RoombaNet.Settings/`:

- `appsettings.json` holds defaults and is committed.
- `secrets.Development.json` holds your robot credentials. It is in `.gitignore`.

Create `src/RoombaNet.Settings/secrets.Development.json`:

```json
{
  "RoombaSettings": {
    "Ip": "192.168.1.100",
    "Port": 8883,
    "Blid": "1234567890ABCDEF",
    "Password": "your-robot-password"
  }
}
```

| Key | Default | Meaning |
|---|---|---|
| `Ip` | | Robot IP, or proxy IP when you use the [MQTT proxy](proxy.md). |
| `Port` | `8883` | MQTT port. |
| `Blid` | | Robot ID from discovery. |
| `Password` | | Robot password from step 2. |
| `UseTls` | `true` | Set to `false` only when you connect to the proxy's plain listener. The robot itself always uses TLS. |

The environment name comes from `DOTNET_ENVIRONMENT` and defaults to `Development`. For
another environment, name the file `secrets.<Environment>.json`.

Test the connection:

```bash
dotnet run --project src/RoombaNet.Cli status
dotnet run --project src/RoombaNet.Cli command find
```

The robot should beep.

## Several robots

Give each robot its own config section, and map short profile names to those sections under
`Robots:Profiles`:

```json
{
  "Robots": {
    "DefaultRobot": "roomba",
    "Profiles": {
      "roomba": "RoombaSettings",
      "braava": "BraavaSettings"
    }
  },
  "RoombaSettings": {
    "Ip": "192.168.20.65",
    "Blid": "<roomba-blid>",
    "Password": "<roomba-password>"
  },
  "BraavaSettings": {
    "Ip": "192.168.20.66",
    "Blid": "<braava-blid>",
    "Password": "<braava-password>"
  }
}
```

Pick a robot with `--robot` (or `-r`). Without it, the CLI uses `DefaultRobot`. If that is empty
too, it uses the `RoombaSettings` section.

```bash
dotnet run --project src/RoombaNet.Cli -r braava status
dotnet run --project src/RoombaNet.Cli -r roomba command dock
```

The MQTT proxy uses the same profile list, so `proxy` serves every robot listed here.

## Set up Wi-Fi without the app

You can connect a new or reset robot to your Wi-Fi without the iRobot app.

1. Put the robot in pairing mode, as in step 2. It starts its own access point.
2. Connect your computer to the robot's access point.
3. Send the Wi-Fi settings:

   ```bash
   dotnet run --project src/RoombaNet.Cli configure-wifi \
     --ssid "YourNetwork" \
     --password "YourWiFiPassword"
   ```

   Optional: `--robot-name "My Roomba"`, `--timezone "Europe/Ljubljana"` (IANA name),
   `--country "SI"`.

4. Do not press any buttons while the robot connects. It joins the network within about a
   minute and beeps when done.
5. Connect your computer back to your normal network and run `discover` to find the robot's new IP.

Most robots support 2.4 GHz Wi-Fi only.

## Next steps

- Run the robots without internet access, but with correct time: [OpenWrt network setup](openwrt.md).
- Use the CLI, API, and MCP server at the same time: [MQTT proxy](proxy.md).
- All CLI commands: [CLI](cli.md).
