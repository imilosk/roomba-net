# MCP server

`RoombaNet.Mcp` is a [Model Context Protocol](https://modelcontextprotocol.io) server. It lets an
AI assistant, such as Claude Code or Claude Desktop, control the robots in plain language.

The MCP server does not talk to the robots itself. It calls the [REST API](api.md), so the API
must be running, and the robots must be in the API registry.

```
AI assistant  --stdio-->  RoombaNet.Mcp  --HTTP-->  RoombaNet.Api  --MQTT-->  robots
```

## Setup

1. Start the API:

   ```bash
   dotnet run --project src/RoombaNet.Api
   ```

2. Register the MCP server in your AI client. For Claude Code, put this in `.mcp.json`. For
   Claude Desktop, put it in `claude_desktop_config.json`:

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

   `ROOMBANET_API_URL` defaults to `http://localhost:5264`. To start faster, publish the project
   once and set `command` to the built binary.

3. Ask, for example: "Clean the kitchen and the living room, then dock."

## Tools

| Area | Tools |
|---|---|
| Robots | `list_robots`, `discover_robots` |
| Status | `get_status`, `get_status_summary`, `get_firmware` |
| Missions | `start_cleaning`, `stop_cleaning`, `pause_cleaning`, `resume_cleaning`, `cancel_cleaning`, `dock`, `find`, `evacuate_bin`, `send_command` |
| Maps | `list_maps`, `clear_maps`, `clean_regions` |
| Schedule | `get_schedule`, `set_schedule` |
| Settings | `set_cleaning_power`, `set_cleaning_passes`, `set_child_lock`, `set_eco_charge`, `set_perimeter_pass`, `set_map_learning`, `set_continuous_map_learning`, `set_map_upload`, `set_bin_type_detect`, `set_volume`, `set_timezone`, `set_language` |

`clear_maps` deletes every map on the robot. Most clients ask you to confirm a tool call; check
this one before you allow it.
