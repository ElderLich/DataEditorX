/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: June 10, Tuesday
 * Time: 9:58
 * 
 */
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace DataEditorX.Common
{
    /// <summary>
    /// Update checker and installer.
    /// </summary>
    public static class CheckUpdate
    {
        public enum UpdateInfoStatus
        {
            Ok,
            EmptyUrl,
            Unavailable,
            InvalidFormat,
        }

        /// <summary>
        /// Selected release download URL.
        /// </summary>
        public static string URL = "";
        public static UpdateInfoStatus LastInfoStatus { get; private set; } = UpdateInfoStatus.Ok;
        public static string LastInfoError { get; private set; } = "";
        public static string LastDownloadError { get; private set; } = "";
        /// <summary>
        /// Default version when metadata cannot be read.
        /// </summary>
        public const string DEFAULT = "0.0.0.0";
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public sealed class DownloadProgress
        {
            public long BytesReceived { get; init; }
            public long? TotalBytes { get; init; }

            public int Percent
            {
                get
                {
                    if (!TotalBytes.HasValue || TotalBytes.Value <= 0)
                    {
                        return 0;
                    }

                    long percent = BytesReceived * 100 / TotalBytes.Value;
                    return (int)Math.Clamp(percent, 0, 100);
                }
            }
        }

        #region Version check
        /// <summary>
        /// Reads the latest available version from update metadata.
        /// </summary>
        /// <param name="VERURL">Metadata URL.</param>
        /// <returns>Version number.</returns>
        public static string GetNewVersion(string VERURL)
        {
            return GetNewVersionAsync(VERURL).GetAwaiter().GetResult();
        }

        public static async Task<string> GetNewVersionAsync(string VERURL, CancellationToken cancellationToken = default)
        {
            URL = "";
            LastInfoStatus = UpdateInfoStatus.Ok;
            LastInfoError = "";

            string urlver = DEFAULT;
            if (string.IsNullOrWhiteSpace(VERURL))
            {
                LastInfoStatus = UpdateInfoStatus.EmptyUrl;
                LastInfoError = "Update URL is empty.";
                return urlver;
            }

            (bool success, string html, string error) = await TryGetHtmlContentByUrlAsync(VERURL, cancellationToken).ConfigureAwait(false);
            if (!success)
            {
                LastInfoStatus = UpdateInfoStatus.Unavailable;
                LastInfoError = error;
                return urlver;
            }

            if (!string.IsNullOrEmpty(html))
            {
                Regex ver = new(@"\[DataEditorX\]([0-9]+(?:\.[0-9]+){2,3})\[DataEditorX\]");
                Regex url = new(@"\[URL\]([^\[]+?) ?\[URL\]");
                if (ver.IsMatch(html) && url.IsMatch(html))
                {
                    Match mVer = ver.Match(html);
                    MatchCollection mUrl = url.Matches(html);
                    try
                    {
                        URL = mUrl.First(Environment.Is64BitOperatingSystem ? (m) =>
                            m.Groups[1].Value.EndsWith("64.zip") : (m) => m.Groups[1].Value.EndsWith("32.zip")).Groups[1].Value;
                    }
                    catch
                    {
                        URL = mUrl.First().Groups[1].Value;
                    }
                    return $"{mVer.Groups[1].Value}";
                }
            }

            LastInfoStatus = UpdateInfoStatus.InvalidFormat;
            LastInfoError = "Update metadata was found, but it did not contain a DataEditorX version and release URL.";
            return urlver;
        }
        /// <summary>
        /// Compares version numbers in 0.0.0.0 format.
        /// </summary>
        /// <param name="ver">0.0.0.0</param>
        /// <param name="oldver">0.0.0.0</param>
        /// <returns>Whether a newer version is available.</returns>
        public static bool CheckVersion(string ver, string oldver)
        {
            bool hasNew = false;
            string[] vers = Regex.Replace(ver, "\\+.*", "").Split('.');
            string[] oldvers = Regex.Replace(oldver, "\\+.*", "").Split('.');

            int count = Math.Max(vers.Length, oldvers.Length);
            if (count > 0)
            {
                // Compare numeric components from left to right.
                for (int i = 0; i < count; i++)
                {
                    int.TryParse(i < vers.Length ? vers[i] : "0", out int j);
                    int.TryParse(i < oldvers.Length ? oldvers[i] : "0", out int k);
                    if (j > k)// New version is greater than the old version.
                    {
                        hasNew = true;
                        break;
                    }
                    else if (j < k)
                    {
                        hasNew = false;
                        break;
                    }
                }
            }
            return hasNew;
        }
        #endregion

        #region URL content
        /// <summary>
        /// Reads text content from a URL.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>Response content.</returns>
        public static string GetHtmlContentByUrl(string url)
        {
            return GetHtmlContentByUrlAsync(url).GetAwaiter().GetResult();
        }

        public static async Task<string> GetHtmlContentByUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            (bool success, string content, _) = await TryGetHtmlContentByUrlAsync(url, cancellationToken).ConfigureAwait(false);
            return success ? content : "";
        }

        private static async Task<(bool Success, string Content, string Error)> TryGetHtmlContentByUrlAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response = await Http
                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return (false, "", $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                }

                await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using StreamReader streamReader = new(stream, Encoding.UTF8);
                string htmlContent = await streamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                return (true, htmlContent, "");
            }
            catch (Exception ex)
            {
                return (false, "", ex.GetBaseException().Message);
            }
        }
        #endregion

        #region Download
        /// <summary>
        /// Downloads the selected update archive.
        /// </summary>
        /// <param name="filename">Destination file path.</param>
        /// <returns>Whether the download succeeded.</returns>
        public static bool DownLoad(string filename)
        {
            return DownloadAsync(filename).GetAwaiter().GetResult();
        }

        public static async Task<bool> DownloadAsync(string filename, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                LastDownloadError = "";

                if (string.IsNullOrWhiteSpace(URL))
                {
                    LastDownloadError = "The update download URL is empty.";
                    return false;
                }

                string? directory = Path.GetDirectoryName(filename);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempFile = filename + ".tmp";
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                using HttpResponseMessage response = await Http
                    .GetAsync(URL, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    LastDownloadError = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                long? totalBytes = response.Content.Headers.ContentLength;
                await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using FileStream destination = new(
                    tempFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    1024 * 128,
                    useAsync: true);

                long totalDownloadedByte = 0;
                byte[] buffer = new byte[1024 * 128];
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                    totalDownloadedByte += bytesRead;
                    progress?.Report(new DownloadProgress
                    {
                        BytesReceived = totalDownloadedByte,
                        TotalBytes = totalBytes
                    });
                }

                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }

                File.Move(tempFile, filename);
            }
            catch (Exception ex)
            {
                LastDownloadError = ex.GetBaseException().Message;
                TryDelete(filename + ".tmp");
                return false;
            }
            return true;
        }

        public static bool InstallUpdate(string zipFile)
        {
            try
            {
                if (!File.Exists(zipFile))
                {
                    return false;
                }

                string appDir = Application.StartupPath;
                string exe = Application.ExecutablePath;
                string script = Path.Combine(Path.GetTempPath(), $"DataEditorX_Update_{Guid.NewGuid():N}.ps1");
                string content = $$"""
$ErrorActionPreference = 'Stop'
Wait-Process -Id {{Environment.ProcessId}} -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
$cleanupFiles = @(
    'DataEditorX.deps.json',
    'DataEditorX.dll',
    'DataEditorX.pdb',
    'DataEditorX.runtimeconfig.json',
    'e_sqlite3.dll',
    'FastColoredTextBox.dll',
    'Microsoft.Data.Sqlite.dll',
    'Neo.Lua.dll',
    'Newtonsoft.Json.dll',
    'SQLitePCLRaw.batteries_v2.dll',
    'SQLitePCLRaw.core.dll',
    'SQLitePCLRaw.provider.e_sqlite3.dll',
    'WeifenLuo.WinFormsUI.Docking.dll',
    'WeifenLuo.WinFormsUI.Docking.ThemeVS2015.dll'
)
foreach ($file in $cleanupFiles) {
    Remove-Item -LiteralPath (Join-Path '{{EscapePowerShell(appDir)}}' $file) -Force -ErrorAction SilentlyContinue
}
Remove-Item -LiteralPath (Join-Path '{{EscapePowerShell(appDir)}}' 'de') -Recurse -Force -ErrorAction SilentlyContinue
Expand-Archive -LiteralPath '{{EscapePowerShell(zipFile)}}' -DestinationPath '{{EscapePowerShell(appDir)}}' -Force
Remove-Item -LiteralPath '{{EscapePowerShell(zipFile)}}' -Force -ErrorAction SilentlyContinue
Start-Process -FilePath '{{EscapePowerShell(exe)}}' -WorkingDirectory '{{EscapePowerShell(appDir)}}'
Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue
""";
                File.WriteAllText(script, content, new UTF8Encoding(false));

                ProcessStartInfo info = new()
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                return Process.Start(info) != null;
            }
            catch
            {
                return false;
            }
        }

        private static string EscapePowerShell(string text)
        {
            return (text ?? "").Replace("'", "''");
        }

        private static void TryDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }
        }
        #endregion
    }
}
