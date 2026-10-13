using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace DataEditorX.Common
{
    internal static class UpdateInstaller
    {
        internal static bool Start(string archive, string appDirectory, string executable, int processId)
        {
            string script = Prepare(archive, appDirectory, executable, processId);
            try
            {
                using Process? process = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                if (process == null)
                    throw new IOException("Windows could not start the update installer.");
                return true;
            }
            catch
            {
                // Preparation only creates a uniquely named temporary directory.
                // The installed application has not been changed at this point.
                TryRemovePreparation(Path.GetDirectoryName(script)!);
                throw;
            }
        }

        internal static string Prepare(string archive, string appDirectory, string executable, int processId)
        {
            string preparation = Path.Combine(Path.GetTempPath(), "DataEditorX_Update_" + Guid.NewGuid().ToString("N"));
            string files = Path.Combine(preparation, "files");
            try
            {
                // Fully extract before closing the application. ExtractToDirectory
                // rejects archive entries that would escape this staging directory.
                ZipFile.ExtractToDirectory(archive, files);
                foreach (string required in new[] { "DataEditorX.exe", "pack.db" })
                {
                    string path = Path.Combine(files, required);
                    if (!File.Exists(path) || new FileInfo(path).Length == 0)
                        throw new InvalidDataException($"The update archive is missing a valid {required} at its root. Please download it again.");
                }
                string data = Path.Combine(files, "data");
                if (!Directory.Exists(data) || !Directory.EnumerateFiles(data, "*", SearchOption.AllDirectories).Any())
                    throw new InvalidDataException("The update archive is missing its data files. Please download it again.");

                string script = Path.Combine(preparation, "install.ps1");
                File.WriteAllText(script, BuildScript(Path.GetFullPath(archive), Path.GetFullPath(appDirectory),
                    Path.GetFullPath(executable), preparation, processId), new UTF8Encoding(true));
                return script;
            }
            catch
            {
                TryRemovePreparation(preparation);
                throw;
            }
        }

        private static string BuildScript(string archive, string appDirectory, string executable, string preparation, int processId)
        {
            return $$"""
$ErrorActionPreference = 'Stop'
$appDir = '{{Escape(appDirectory)}}'
$exePath = '{{Escape(executable)}}'
$archive = '{{Escape(archive)}}'
$preparation = '{{Escape(preparation)}}'
$files = Join-Path $preparation 'files'
$backup = Join-Path $preparation 'backup'
$log = Join-Path $preparation 'install.log'
$changes = New-Object 'System.Collections.Generic.List[object]'
$appPrefix = [IO.Path]::GetFullPath($appDir).TrimEnd('\') + '\'

function Get-Target([string] $relative) {
    $target = [IO.Path]::GetFullPath((Join-Path $appDir $relative))
    if (-not $target.StartsWith($appPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'An update path is outside the application directory.'
    }
    return $target
}

function Backup-Target([string] $relative) {
    $target = Get-Target $relative
    if ([IO.Directory]::Exists($target)) { throw "A directory blocks the update file: $relative" }
    $saved = Join-Path $backup $relative
    $existed = [IO.File]::Exists($target)
    if ($existed) {
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($saved)) | Out-Null
        Copy-Item -LiteralPath $target -Destination $saved -Force
    }
    $changes.Add([pscustomobject]@{ Target = $target; Saved = $saved; Existed = $existed })
    return $target
}

try {
    Wait-Process -Id {{processId}} -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
    $filePrefix = [IO.Path]::GetFullPath($files).TrimEnd('\') + '\'
    foreach ($file in Get-ChildItem -LiteralPath $files -File -Recurse) {
        $relative = $file.FullName.Substring($filePrefix.Length)
        $target = Backup-Target $relative
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
    # Remove obsolete loose assemblies only after all staged files are installed.
    # Any replacement shipped in the new archive is retained.
    $obsolete = @(
        'DataEditorX.deps.json', 'DataEditorX.dll', 'DataEditorX.pdb', 'DataEditorX.runtimeconfig.json',
        'e_sqlite3.dll', 'FastColoredTextBox.dll', 'Microsoft.Data.Sqlite.dll', 'Neo.Lua.dll', 'Newtonsoft.Json.dll',
        'SQLitePCLRaw.batteries_v2.dll', 'SQLitePCLRaw.core.dll', 'SQLitePCLRaw.provider.e_sqlite3.dll',
        'WeifenLuo.WinFormsUI.Docking.dll', 'WeifenLuo.WinFormsUI.Docking.ThemeVS2015.dll'
    )
    foreach ($relative in $obsolete) {
        $target = Get-Target $relative
        if ([IO.File]::Exists($target) -and -not [IO.File]::Exists((Join-Path $files $relative))) {
            $target = Backup-Target $relative
            Remove-Item -LiteralPath $target -Force
        }
    }
    Start-Process -FilePath $exePath -WorkingDirectory $appDir -ErrorAction Stop
}
catch {
    $failure = $_.Exception.Message
    $restoreErrors = New-Object 'System.Collections.Generic.List[string]'
    for ($i = $changes.Count - 1; $i -ge 0; $i--) {
        $change = $changes[$i]
        try {
            if ($change.Existed) {
                Copy-Item -LiteralPath $change.Saved -Destination $change.Target -Force
            }
            elseif ([IO.File]::Exists($change.Target)) {
                Remove-Item -LiteralPath $change.Target -Force
            }
        }
        catch { $restoreErrors.Add($_.Exception.Message) }
    }
    $recovery = if ($restoreErrors.Count -eq 0) { 'Previous application files were restored.' }
        else { 'Some files could not be restored. Recovery copies are in: ' + $backup }
    $message = "DataEditorX could not finish updating.`r`n`r`n$failure`r`n`r`n$recovery`r`nThe downloaded archive was kept. You can restart DataEditorX or install the update manually.`r`n`r`nDetails: $log"
    ($message + "`r`n" + ($restoreErrors -join "`r`n")) | Set-Content -LiteralPath $log -Encoding UTF8
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show($message, 'DataEditorX update failed') | Out-Null
    exit 1
}
# Cleanup cannot roll back a successful restart if an antivirus still holds a file.
Remove-Item -LiteralPath $archive -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $preparation -Recurse -Force -ErrorAction SilentlyContinue
""";
        }

        private static string Escape(string value) => value.Replace("'", "''");

        private static void TryRemovePreparation(string directory)
        {
            try
            {
                string path = Path.GetFullPath(directory);
                string temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
                if (string.Equals(Path.GetDirectoryName(path), temporaryRoot, StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(path).StartsWith("DataEditorX_Update_", StringComparison.Ordinal)
                    && Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch { }
        }
    }
}