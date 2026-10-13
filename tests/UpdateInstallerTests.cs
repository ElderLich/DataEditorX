using DataEditorX.Common;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;

internal static class UpdateInstallerTests
{
    internal static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "dataeditor-installer-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string installation = Path.Combine(root, "Editor's Ω folder");
            Directory.CreateDirectory(installation);
            string executable = Path.Combine(installation, "DataEditorX.exe");
            File.WriteAllText(executable, "old executable");
            File.WriteAllText(Path.Combine(installation, "pack.db"), "old database");
            File.WriteAllText(Path.Combine(installation, "DataEditorX.dll"), "old loose assembly");
            File.WriteAllText(Path.Combine(installation, "my-project.cdb"), "user project");
            Directory.CreateDirectory(Path.Combine(installation, "data"));
            File.WriteAllText(Path.Combine(installation, "data", "reference.txt"), "old reference");

            string invalid = Path.Combine(root, "invalid.zip");
            File.WriteAllText(invalid, "a downloaded error page, not an archive");
            Assert(!CheckUpdate.InstallUpdate(invalid) && !string.IsNullOrWhiteSpace(CheckUpdate.LastInstallError),
                "Invalid archives must report preparation failure without starting an installer");
            AssertOriginal(installation);

            foreach (string missing in new[] { "DataEditorX.exe", "pack.db", "data/reference.txt" })
            {
                string incomplete = CreateArchive(root, "missing-" + Path.GetFileName(missing) + ".zip", missing);
                ExpectPreparationFailure(incomplete, installation);
                AssertOriginal(installation);
            }
            string traversal = Path.Combine(root, "traversal.zip");
            using (var zip = ZipFile.Open(traversal, ZipArchiveMode.Create))
            using (var writer = new StreamWriter(zip.CreateEntry("../escaped.txt").Open()))
                writer.Write("unexpected");
            ExpectPreparationFailure(traversal, installation);
            AssertOriginal(installation);

            VerifyHelper(root, installation, failRestart: true);
            VerifyHelper(root, installation, failRestart: false);
            Console.WriteLine("PASS: invalid/incomplete update archives leave installation untouched; staged installer replaces files, preserves projects, rolls back failures and reports diagnostics.");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void VerifyHelper(string root, string installation, bool failRestart)
    {
        string archive = CreateArchive(root, failRestart ? "rollback.zip" : "valid.zip");
        string script = Prepare(archive, installation);
        string preparation = Path.GetDirectoryName(script)!;
        try
        {
            AssertOriginal(installation);
            Assert(File.Exists(Path.Combine(preparation, "files", "DataEditorX.exe")),
                "The archive must be extracted before the installer can be launched");
            string source = File.ReadAllText(script);
            const string restart = "Start-Process -FilePath $exePath -WorkingDirectory $appDir -ErrorAction Stop";
            const string report = "[System.Windows.Forms.MessageBox]::Show($message, 'DataEditorX update failed') | Out-Null";
            Assert(source.Contains(restart) && source.Contains(report), "Test must intercept exactly the real process/dialog boundaries");
            // Exercise the generated PowerShell against disposable fixture files only.
            // Never start the real app or show a message box during a regression run.
            source = source.Replace(restart, failRestart ? "throw 'Simulated restart failure'"
                : "Set-Content -LiteralPath (Join-Path $appDir 'restart-requested') -Value 'yes'")
                .Replace(report, "Write-Output $message");
            File.WriteAllText(script, source, new UTF8Encoding(true));
            using var helper = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"",
                WorkingDirectory = root,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            })!;
            var output = helper.StandardOutput.ReadToEndAsync();
            var error = helper.StandardError.ReadToEndAsync();
            if (!helper.WaitForExit(15000))
            {
                helper.Kill();
                throw new Exception("Fixture installer did not finish within 15 seconds");
            }
            string diagnostics = output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult();
            if (failRestart)
            {
                Assert(helper.ExitCode == 1, "Restart failure must fail explicitly: " + diagnostics);
                AssertOriginal(installation);
                Assert(!File.Exists(Path.Combine(installation, "data", "new-reference.txt")),
                    "Rollback must remove newly added files as well as restore replaced files");
                Assert(File.Exists(archive) && File.ReadAllText(Path.Combine(preparation, "install.log")).Contains("Simulated restart failure"),
                    "Failures must retain the archive and readable error/recovery details");
            }
            else
            {
                Assert(helper.ExitCode == 0, "Staged installation must complete: " + diagnostics);
                Assert(File.ReadAllText(Path.Combine(installation, "DataEditorX.exe")) == "new executable"
                    && File.ReadAllText(Path.Combine(installation, "pack.db")) == "new database"
                    && File.ReadAllText(Path.Combine(installation, "data", "reference.txt")) == "new reference",
                    "Successful installation must copy all staged files");
                Assert(!File.Exists(Path.Combine(installation, "DataEditorX.dll")), "Obsolete loose assemblies must be removed after the copy");
                Assert(File.Exists(Path.Combine(installation, "restart-requested")), "The helper must request restart after copying");
                Assert(!File.Exists(archive) && !Directory.Exists(preparation), "Successful installation must clean up its own temporary files");
            }
            Assert(File.ReadAllText(Path.Combine(installation, "my-project.cdb")) == "user project", "Updates and rollback must preserve unrelated user files");
        }
        finally
        {
            if (Directory.Exists(preparation)) Directory.Delete(preparation, recursive: true);
        }
    }

    private static string CreateArchive(string root, string name, string missing = "")
    {
        string path = Path.Combine(root, name);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (entry, text) in new[] { ("DataEditorX.exe", "new executable"), ("pack.db", "new database"),
            ("data/reference.txt", "new reference"), ("data/new-reference.txt", "new file") })
        {
            if (entry == missing || (missing.StartsWith("data/") && entry.StartsWith("data/"))) continue;
            using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
            writer.Write(text);
        }
        return path;
    }

    private static string Prepare(string archive, string installation)
    {
        var type = typeof(CheckUpdate).Assembly.GetType("DataEditorX.Common.UpdateInstaller", true)!;
        var method = type.GetMethod("Prepare", BindingFlags.NonPublic | BindingFlags.Static)!;
        // An impossible Windows process ID makes the wait a no-op for the fixture.
        return (string)method.Invoke(null, new object[] { archive, installation,
            Path.Combine(installation, "DataEditorX.exe"), int.MaxValue })!;
    }

    private static void ExpectPreparationFailure(string archive, string installation)
    {
        try { Prepare(archive, installation); }
        catch (TargetInvocationException ex) when (ex.InnerException is IOException or InvalidDataException) { return; }
        throw new Exception("Invalid update archive should have failed before touching the installation");
    }

    private static void AssertOriginal(string installation)
    {
        Assert(File.ReadAllText(Path.Combine(installation, "DataEditorX.exe")) == "old executable"
            && File.ReadAllText(Path.Combine(installation, "pack.db")) == "old database"
            && File.ReadAllText(Path.Combine(installation, "DataEditorX.dll")) == "old loose assembly"
            && File.ReadAllText(Path.Combine(installation, "data", "reference.txt")) == "old reference",
            "Existing application files must remain intact after preparation or recovery");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}