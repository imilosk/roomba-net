using System.CommandLine;
using System.Security.Cryptography;
using RoombaNet.Core;
using RoombaNet.Settings.Settings;

namespace RoombaNet.Cli.Commands;

public class SetPasswordCommand : Command
{
    private readonly IRoombaPasswordService _roombaPasswordService;
    private readonly RoombaSettings _roombaSettings;
    private readonly CancellationToken _cancellationToken;

    public SetPasswordCommand(
        IRoombaPasswordService roombaPasswordService,
        RoombaSettings roombaSettings,
        CancellationToken cancellationToken = default
    ) : base("set-password", "Set Roomba password via SoftAP (192.168.10.1)")
    {
        _roombaPasswordService = roombaPasswordService;
        _roombaSettings = roombaSettings;
        _cancellationToken = cancellationToken;

        var passwordOption = new Option<string>("--password")
        {
            Description = "Password to set on the Roomba (ASCII).",
        };

        Add(passwordOption);

        SetAction(async parseResult =>
        {
            var password = parseResult.GetValue(passwordOption);
            if (string.IsNullOrWhiteSpace(password))
            {
                password = GeneratePassword();
                Console.WriteLine($"Generated password: {password}");
            }

            var blid = _roombaSettings.Blid;
            if (string.IsNullOrWhiteSpace(blid))
            {
                Console.WriteLine("Error: RoombaSettings:Blid is required");
                return;
            }

            var success = await _roombaPasswordService.SetPassword(
                Core.Constants.RoombaApDefaults.DefaultApAddress,
                password,
                blid,
                blid,
                Core.Constants.RoombaApDefaults.DefaultApPort,
                _cancellationToken);

            Console.WriteLine(success
                ? "Password set request sent."
                : "Failed to send password set request.");
        });
    }

    private static string GeneratePassword()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var suffix = new char[16];
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = new byte[suffix.Length];

        RandomNumberGenerator.Fill(bytes);
        for (var i = 0; i < suffix.Length; i++)
        {
            suffix[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return $":1:{timestamp}:{new string(suffix)}";
    }
}
