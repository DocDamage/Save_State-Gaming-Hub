using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.ValueObjects;

namespace SaveState.Application.RomManagement.Commands;

public record RegisterEmulatorsCommand : IRequest<Result>;

public class RegisterEmulatorsCommandHandler : IRequestHandler<RegisterEmulatorsCommand, Result>
{
    private readonly IPlatformRepository _platformRepository;
    private readonly IEmulatorRepository _emulatorRepository;

    public RegisterEmulatorsCommandHandler(
        IPlatformRepository platformRepository,
        IEmulatorRepository emulatorRepository)
    {
        _platformRepository = platformRepository;
        _emulatorRepository = emulatorRepository;
    }

    public async Task<Result> Handle(RegisterEmulatorsCommand request, CancellationToken cancellationToken)
    {
        var retroArchPath = Path.Combine(Environment.CurrentDirectory, "engines", "RetroArch-Win64", "retroarch.exe");
        var coresPath = Path.Combine(Environment.CurrentDirectory, "engines", "RetroArch-Win64", "cores");

        if (!File.Exists(retroArchPath))
        {
             return Result.Failure("RetroArch executable not found.");
        }

        try
        {
            var executablePath = new FilePath(retroArchPath);

            var mappings = new Dictionary<string, string[]>
            {
                { "mgba_libretro.dll", new[] { "Game Boy Advance" } },
                { "mesen_libretro.dll", new[] { "NES" } },
                { "genesis_plus_gx_libretro.dll", new[] { "Genesis", "Master System", "Game Gear" } },
                { "fbneo_libretro.dll", new[] { "Arcade", "Neo Geo" } },
                { "stella_libretro.dll", new[] { "Atari 2600" } }
            };

            foreach (var mapping in mappings)
            {
                var coreDll = Path.Combine(coresPath, mapping.Key);
                if (File.Exists(coreDll))
                {
                    foreach (var platformName in mapping.Value)
                    {
                        var platform = await _platformRepository.GetByNameAsync(platformName, cancellationToken);
                        if (platform != null)
                        {
                            var existing = await _emulatorRepository.GetByPlatformIdAsync(platform.Id, cancellationToken);
                            if (existing == null)
                            {
                                var args = $"-L \"{coreDll}\" \"{{ROM}}\"";
                                var emu = new SaveState.Core.RomManagement.Entities.Emulator($"RetroArch ({mapping.Key})", executablePath, platform.Id);
                                emu.SetCommandLineArgs(args);
                                await _emulatorRepository.AddAsync(emu, cancellationToken);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to setup emulator paths: {ex.Message}");
        }

        return Result.Success();
    }
}
