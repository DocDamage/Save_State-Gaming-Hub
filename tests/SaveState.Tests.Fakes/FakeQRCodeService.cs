using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of IQRCodeService for integration testing.
/// </summary>
public class FakeQRCodeService : IQRCodeService
{
    public Task<Result<byte[]>> GeneratePairingQRCodeAsync(PairingInfo info, int size = 256)
    {
        // Return a fake PNG image (just some bytes)
        var fakePng = new byte[size * size * 4];
        new Random().NextBytes(fakePng);
        return Task.FromResult(Result<byte[]>.Success(fakePng));
    }

    public Task<Result<PairingInfo?>> ReadQRCodeAsync(Stream imageStream)
    {
        return Task.FromResult(Result<PairingInfo?>.Success(null));
    }

    public string GeneratePairingUrl(PairingInfo info)
    {
        return $"savestate://pair?hub={info.HubId}&ip={info.IpAddress}&port={info.Port}";
    }

    public Result<PairingInfo> ParsePairingUrl(string url)
    {
        var info = new PairingInfo
        {
            HubId = "test-hub",
            HubName = "Test Hub",
            IpAddress = "127.0.0.1",
            Port = 8080,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        return Result<PairingInfo>.Success(info);
    }

    public string GenerateManualPairingCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    public bool ValidateManualPairingCode(string code)
    {
        return code.Length == 6 && code.All(char.IsDigit);
    }
}
