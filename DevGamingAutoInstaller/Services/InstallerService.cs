using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using DevGamingAutoInstaller.Models;
using Microsoft.Win32;

namespace DevGamingAutoInstaller.Services;

public sealed class InstallerService
{
    private readonly HttpClient _httpClient;
    private readonly Logger _logger;

    public InstallerService(HttpClient httpClient, Logger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsInstalled(SoftwareItem item)
    {
        // Fast-path file detection for apps without registry entries.
        if (!string.IsNullOrWhiteSpace(item.DetectFilePath) && File.Exists(item.DetectFilePath))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(item.DetectDisplayNameContains))
        {
            return IsInstalledByDisplayName(item.DetectDisplayNameContains);
        }

        return false;
    }

    public async Task<List<InstallResult>> InstallAsync(
        IReadOnlyList<SoftwareItem> items,
        IProgress<InstallProgress> progress,
        Action<SoftwareItem, InstallStatus, string> statusCallback,
        CancellationToken cancellationToken)
    {
        var results = new List<InstallResult>();
        var total = items.Count;
        var completed = 0;
        var overallPercent = 0d;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            overallPercent = total == 0 ? 0 : (completed / (double)total) * 100;
            progress.Report(new InstallProgress
            {
                OverallPercent = overallPercent,
                CurrentItem = item.Name,
                Message = "Előkészítés"
            });

            try
            {
                if (IsInstalled(item))
                {
                    statusCallback(item, InstallStatus.AlreadyInstalled, "Már telepítve");
                    results.Add(new InstallResult { Name = item.Name, Status = InstallStatus.AlreadyInstalled, Message = "Már telepítve" });
                    _logger.Info($"{item.Name}: már telepítve.");
                    completed++;
                    continue;
                }

                if (item.InstallMethod == InstallMethod.SystemCommand)
                {
                    statusCallback(item, InstallStatus.Installing, "Rendszerparancs futtatása");
                    await RunProcessAsync(item.Command ?? string.Empty, item.CommandArgs ?? string.Empty, item.RequiresAdmin, cancellationToken).ConfigureAwait(false);
                    statusCallback(item, InstallStatus.Success, "Sikeres telepítés");
                    results.Add(new InstallResult { Name = item.Name, Status = InstallStatus.Success, Message = "Sikeres telepítés" });
                    _logger.Info($"{item.Name}: rendszerparancs sikeres.");
                    completed++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.DownloadUrl))
                {
                    throw new InvalidOperationException("Nincs letöltési URL megadva.");
                }

                // Download installer to a temp location so it can be executed silently.
                statusCallback(item, InstallStatus.Downloading, "Letöltés folyamatban");
                var installerPath = await DownloadInstallerAsync(item, overallPercent, progress, cancellationToken).ConfigureAwait(false);

                if (installerPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    statusCallback(item, InstallStatus.Installing, "Kicsomagolás folyamatban");
                    await ExtractZipAsync(item, installerPath, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    statusCallback(item, InstallStatus.Installing, "Telepítés folyamatban");
                    await RunInstallerAsync(installerPath, item.SilentArgs, item.RequiresAdmin, cancellationToken).ConfigureAwait(false);
                }

                // Optional post-install steps (e.g., Python pip upgrades).
                if (item.PostInstallCommands.Count > 0)
                {
                    statusCallback(item, InstallStatus.PostInstall, "Utótelepítési lépések");
                    foreach (var command in item.PostInstallCommands)
                    {
                        await RunProcessAsync("cmd.exe", $"/c {command}", item.RequiresAdmin, cancellationToken).ConfigureAwait(false);
                    }
                }

                statusCallback(item, InstallStatus.Success, "Sikeres telepítés");
                results.Add(new InstallResult { Name = item.Name, Status = InstallStatus.Success, Message = "Sikeres telepítés" });
                _logger.Info($"{item.Name}: telepítés sikeres.");
            }
            catch (Exception ex)
            {
                statusCallback(item, InstallStatus.Failed, ex.Message);
                results.Add(new InstallResult { Name = item.Name, Status = InstallStatus.Failed, Message = ex.Message });
                _logger.Error($"{item.Name}: hiba - {ex.Message}");
            }
            finally
            {
                completed++;
                var finalPercent = total == 0 ? 100 : (completed / (double)total) * 100;
                progress.Report(new InstallProgress
                {
                    OverallPercent = finalPercent,
                    CurrentItem = item.Name,
                    Message = "Kész"
                });
            }
        }

        return results;
    }

    private async Task<string> DownloadInstallerAsync(SoftwareItem item, double overallPercent, IProgress<InstallProgress> progress, CancellationToken cancellationToken)
    {
        var fileName = GetFileNameFromUrl(item.DownloadUrl!) ?? $"{item.Name}.exe";
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);

        using var response = await _httpClient.GetAsync(item.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength ?? -1L;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = File.Create(tempPath);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;

        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            totalRead += read;

            if (contentLength > 0)
            {
                var percent = totalRead / (double)contentLength * 100;
                progress.Report(new InstallProgress
                {
                    OverallPercent = overallPercent,
                    CurrentItem = item.Name,
                    Message = $"Letöltés {percent:0}%"
                });
            }
        }

        _logger.Info($"{item.Name}: letöltve ide: {tempPath}");
        return tempPath;
    }

    private async Task RunInstallerAsync(string installerPath, string silentArgs, bool requiresAdmin, CancellationToken cancellationToken)
    {
        if (installerPath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
        {
            var args = $"/i \"{installerPath}\" {silentArgs}".Trim();
            await RunProcessAsync("msiexec.exe", args, requiresAdmin, cancellationToken).ConfigureAwait(false);
            return;
        }

        await RunProcessAsync(installerPath, silentArgs, requiresAdmin, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RunProcessAsync(string fileName, string arguments, bool requiresAdmin, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = requiresAdmin ? "runas" : string.Empty,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Nem sikerült elindítani a folyamatot.");
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"A folyamat hibával állt le. ExitCode: {process.ExitCode}");
        }
    }

    private async Task ExtractZipAsync(SoftwareItem item, string zipPath, CancellationToken cancellationToken)
    {
        var targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "FFmpeg");
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, targetDir, true), cancellationToken).ConfigureAwait(false);
        _logger.Info($"{item.Name}: kicsomagolva ide: {targetDir}");
    }

    private static string? GetFileNameFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
        {
            return null;
        }

        return fileName;
    }

    private static bool IsInstalledByDisplayName(string displayNameContains)
    {
        foreach (var root in new[]
        {
            Registry.LocalMachine,
            Registry.CurrentUser
        })
        {
            if (IsInstalledInKey(root, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", displayNameContains))
            {
                return true;
            }

            if (IsInstalledInKey(root, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", displayNameContains))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInstalledInKey(RegistryKey root, string path, string displayNameContains)
    {
        using var key = root.OpenSubKey(path);
        if (key == null)
        {
            return false;
        }

        foreach (var subKeyName in key.GetSubKeyNames())
        {
            using var subKey = key.OpenSubKey(subKeyName);
            var displayName = subKey?.GetValue("DisplayName") as string;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            if (displayName.Contains(displayNameContains, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
