using System.Threading.Channels;
using RoombaNet.Api.Models;

namespace RoombaNet.Api.Services.RoombaStatus;

public record RoombaStatusSubscription(
    RoombaStatusUpdate LastStatus,
    ChannelReader<RoombaStatusUpdate> Updates
);
