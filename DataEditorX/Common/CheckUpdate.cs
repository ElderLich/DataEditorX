/*
 * 由SharpDevelop创建。
 * 用户： Acer
 * 日期: 6月10 星期二
 * 时间: 9:58
 * 
 */
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace DataEditorX.Common
{
    /// <summary>
    /// 检查更新
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
        /// 下载URL
        /// </summary>
        public static string URL = "";
        public static UpdateInfoStatus LastInfoStatus { get; private set; } = UpdateInfoStatus.Ok;
        public static string LastInfoError { get; private set; } = "";
        /// <summary>
        /// 从HEAD获取版本号
        /// </summary>
        public const string DEFAULT = "0.0.0.0";

        #region 检查版本
        /// <summary>
        /// 获取新版本
        /// </summary>
        /// <param name="VERURL">链接</param>
        /// <returns>版本号</returns>
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
        /// 检查版本号，格式0.0.0.0
        /// </summary>
        /// <param name="ver">0.0.0.0</param>
        /// <param name="oldver">0.0.0.0</param>
        /// <returns>是否有新版本</returns>
        public static bool CheckVersion(string ver, string oldver)
        {
            bool hasNew = false;
            string[] vers = Regex.Replace(ver, "\\+.*", "").Split('.');
            string[] oldvers = Regex.Replace(oldver, "\\+.*", "").Split('.');

            int count = Math.Max(vers.Length, oldvers.Length);
            if (count > 0)
            {
                //从左到右比较数字
                for (int i = 0; i < count; i++)
                {
                    int.TryParse(i < vers.Length ? vers[i] : "0", out int j);
                    int.TryParse(i < oldvers.Length ? oldvers[i] : "0", out int k);
                    if (j > k)//新的版本号大于旧的
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

        #region 获取网址内容
        /// <summary>
        /// 获取网址内容
        /// </summary>
        /// <param name="url">网址</param>
        /// <returns>内容</returns>
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

        #region 下载文件
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="filename">保存文件路径</param>
        /// <returns>是否下载成功</returns>
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
