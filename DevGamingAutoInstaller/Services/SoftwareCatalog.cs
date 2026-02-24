using System.Text.Json;
using DevGamingAutoInstaller.Models;

namespace DevGamingAutoInstaller.Services;

public sealed class SoftwareCatalog
{
    private readonly Logger _logger;

    public SoftwareCatalog(Logger logger)
    {
        _logger = logger;
    }

    public async Task<List<SoftwareItem>> LoadAsync(string configPath)
    {
        if (File.Exists(configPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var definitions = JsonSerializer.Deserialize<List<SoftwareDefinition>>(json, options) ?? new();
                return definitions.Select(ToItem).ToList();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Nem sikerült betölteni a JSON konfigurációt: {ex.Message}. Alapértelmezett lista kerül használatra.");
            }
        }

        return GetDefaultDefinitions().Select(ToItem).ToList();
    }

    public Dictionary<string, SoftwareDefinition> GetDefinitionDictionary()
    {
        return GetDefaultDefinitions().ToDictionary(def => def.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static SoftwareItem ToItem(SoftwareDefinition definition)
    {
        return new SoftwareItem
        {
            Name = definition.Name,
            Category = definition.Category,
            DownloadUrl = definition.DownloadUrl,
            SilentArgs = definition.SilentArgs,
            RequiresAdmin = definition.RequiresAdmin,
            InstallMethod = definition.InstallMethod,
            Command = definition.Command,
            CommandArgs = definition.CommandArgs,
            DetectDisplayNameContains = definition.DetectDisplayNameContains,
            DetectFilePath = definition.DetectFilePath,
            PostInstallCommands = definition.PostInstallCommands
        };
    }

    private static List<SoftwareDefinition> GetDefaultDefinitions()
    {
        const string dev = "Fejlesztői eszközök";
        const string games = "Játék platformok";
        const string general = "Általános programok";

        return new List<SoftwareDefinition>
        {
            new()
            {
                Name = "WSL",
                Category = dev,
                InstallMethod = InstallMethod.SystemCommand,
                Command = "wsl",
                CommandArgs = "--install",
                RequiresAdmin = true,
                DetectFilePath = @"C:\\Windows\\System32\\wsl.exe"
            },
            new()
            {
                Name = "Visual Studio Code",
                Category = dev,
                DownloadUrl = "https://code.visualstudio.com/sha/download?build=stable&os=win32-x64",
                SilentArgs = "/verysilent /suppressmsgboxes /norestart",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Microsoft Visual Studio Code"
            },
            new()
            {
                Name = "Visual Studio Community",
                Category = dev,
                DownloadUrl = "https://aka.ms/vs/17/release/vs_Community.exe",
                SilentArgs = "--quiet --wait --norestart --nocache",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Visual Studio Community"
            },
            new()
            {
                Name = "Git",
                Category = dev,
                DownloadUrl = "https://github.com/git-for-windows/git/releases/latest/download/Git-64-bit.exe",
                SilentArgs = "/VERYSILENT /NORESTART",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Git"
            },
            new()
            {
                Name = "GitHub Desktop",
                Category = dev,
                DownloadUrl = "https://central.github.com/deployments/desktop/desktop/latest/win64",
                SilentArgs = "--silent",
                RequiresAdmin = true,
                DetectDisplayNameContains = "GitHub Desktop"
            },
            new()
            {
                Name = "Python",
                Category = dev,
                DownloadUrl = "https://www.python.org/ftp/python/3.12.2/python-3.12.2-amd64.exe",
                SilentArgs = "/quiet InstallAllUsers=1 PrependPath=1 Include_test=0",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Python 3.",
                PostInstallCommands = new List<string>
                {
                    "py -m pip install --upgrade pip",
                    "py -m pip install numpy requests flask"
                }
            },
            new()
            {
                Name = "Node.js (LTS)",
                Category = dev,
                DownloadUrl = "https://nodejs.org/dist/v20.11.1/node-v20.11.1-x64.msi",
                SilentArgs = "/quiet /norestart",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Node.js"
            },
            new()
            {
                Name = "XAMPP",
                Category = dev,
                DownloadUrl = "https://downloadsapachefriends.global.ssl.fastly.net/xampp-files/8.2.12/xampp-windows-x64-8.2.12-0-VS16-installer.exe",
                SilentArgs = "--mode unattended",
                RequiresAdmin = true,
                DetectDisplayNameContains = "XAMPP"
            },
            new()
            {
                Name = "Notepad++",
                Category = dev,
                DownloadUrl = "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/latest/download/npp.8.6.2.Installer.x64.exe",
                SilentArgs = "/S",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Notepad++"
            },
            new()
            {
                Name = "PowerShell 7",
                Category = dev,
                DownloadUrl = "https://github.com/PowerShell/PowerShell/releases/latest/download/PowerShell-7.4.2-win-x64.msi",
                SilentArgs = "/quiet /norestart",
                RequiresAdmin = true,
                DetectDisplayNameContains = "PowerShell 7"
            },
            new()
            {
                Name = "FFmpeg",
                Category = dev,
                DownloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
                SilentArgs = "",
                RequiresAdmin = true,
                DetectFilePath = @"C:\\Program Files\\FFmpeg\\bin\\ffmpeg.exe"
            },
            new()
            {
                Name = "Steam",
                Category = games,
                DownloadUrl = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe",
                SilentArgs = "/S",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Steam"
            },
            new()
            {
                Name = "Epic Games Launcher",
                Category = games,
                DownloadUrl = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi",
                SilentArgs = "/quiet /norestart",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Epic Games Launcher"
            },
            new()
            {
                Name = "GOG Galaxy",
                Category = games,
                DownloadUrl = "https://webinstallers.gog-statics.com/download/GOG_Galaxy_2.0.exe",
                SilentArgs = "/S",
                RequiresAdmin = true,
                DetectDisplayNameContains = "GOG GALAXY"
            },
            new()
            {
                Name = "Ubisoft Connect",
                Category = games,
                DownloadUrl = "https://static3.cdn.ubi.com/orbit/launcher_installer/UbisoftConnectInstaller.exe",
                SilentArgs = "/S",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Ubisoft Connect"
            },
            new()
            {
                Name = "Discord",
                Category = general,
                DownloadUrl = "https://discord.com/api/download?platform=win",
                SilentArgs = "-s",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Discord"
            },
            new()
            {
                Name = "Brave Browser",
                Category = general,
                DownloadUrl = "https://laptop-updates.brave.com/latest/winx64",
                SilentArgs = "/silent /install",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Brave"
            },
            new()
            {
                Name = "VLC Media Player",
                Category = general,
                DownloadUrl = "https://get.videolan.org/vlc/3.0.20/win64/vlc-3.0.20-win64.exe",
                SilentArgs = "/S",
                RequiresAdmin = true,
                DetectDisplayNameContains = "VLC media player"
            },
            new()
            {
                Name = "WinRAR",
                Category = general,
                DownloadUrl = "https://www.rarlab.com/rar/winrar-x64-700.exe",
                SilentArgs = "/S",
                RequiresAdmin = true,
                DetectDisplayNameContains = "WinRAR"
            },
            new()
            {
                Name = "Total Commander",
                Category = general,
                DownloadUrl = "https://download.ghisler.com/tcmd1110x64.exe",
                SilentArgs = "/S",
                RequiresAdmin = true,
                DetectDisplayNameContains = "Total Commander"
            }
        };
    }
}
