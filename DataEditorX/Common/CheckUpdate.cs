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
        /// <summary>
        /// Default version when metadata cannot be read.
        /// </summary>
        public const string DEFAULT = "0.0.0.0";

        #region Version check
        /// <summary>
        /// Reads the latest available version from update metadata.
        /// </summary>
        /// <param name="VERURL">Metadata URL.</param>
        /// <returns>Version number.</returns>
        public static string GetNewVersion(string VERURL)
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

            if (!TryGetHtmlContentByUrl(VERURL, out string html, out string error))
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
            return TryGetHtmlContentByUrl(url, out string htmlContent, out _) ? htmlContent : "";
        }

        private static bool TryGetHtmlContentByUrl(string url, out string htmlContent, out string error)
        {
            htmlContent = "";
            error = "";
            try
            {
                using HttpClient httpClient = new()
                {
                    Timeout = TimeSpan.FromMilliseconds(15000)
                };
                using HttpResponseMessage response = httpClient.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                using Stream stream = response.Content.ReadAsStreamAsync().Result;
                using StreamReader streamReader = new(stream, Encoding.UTF8);
                htmlContent = streamReader.ReadToEnd();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetBaseException().Message;
                return false;
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
            try
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }

                Stream st = new HttpClient().GetStreamAsync(URL).Result;
                Stream so = new FileStream(filename + ".tmp", FileMode.Create);
                long totalDownloadedByte = 0;
                byte[] by = new byte[1024 * 512];
                int osize = st.Read(by, 0, by.Length);
                while (osize > 0)
                {
                    totalDownloadedByte = osize + totalDownloadedByte;
                    Application.DoEvents();
                    so.Write(by, 0, osize);
                    osize = st.Read(by, 0, by.Length);
                }
                so.Close();
                st.Close();
                File.Move(filename + ".tmp", filename);
            }
            catch (Exception)
            {
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
        #endregion
    }
}
