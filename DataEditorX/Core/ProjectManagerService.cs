using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace DataEditorX.Core
{
    internal enum ProjectManagerLogLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    internal sealed record ProjectInstallPaths(
        string Root,
        string Expansions,
        string Scripts,
        string Closeups,
        string Art2,
        string OverFrame,
        string MonsterCutin2);

    internal sealed record ProjectPackageResult(string PackageFile, int FileCount);

    internal sealed class ProjectManagerService
    {
        private readonly Action<string, ProjectManagerLogLevel> log;

        public ProjectManagerService(Action<string, ProjectManagerLogLevel> log)
        {
            this.log = log;
        }

        public static ProjectInstallPaths BuildPaths(string root)
        {
            string expansions = Path.Combine(root, "Expansions");
            string picture = Path.Combine(root, "Picture");
            string mdpro3Data = Path.Combine(root, "MDPro3_Data");

            return new ProjectInstallPaths(
                root,
                expansions,
                Path.Combine(expansions, "script"),
                Path.Combine(picture, "Closeup"),
                Path.Combine(picture, "Art2"),
                Path.Combine(picture, "OverFrame"),
                Path.Combine(mdpro3Data, "StandaloneWindows64", "MonsterCutin2"));
        }

        public static string GetDefaultPackageName(string customProjectDirectory)
        {
            string name = string.IsNullOrWhiteSpace(customProjectDirectory)
                ? "CustomProject"
                : new DirectoryInfo(customProjectDirectory).Name;

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(name) ? "CustomProject.ypk" : name + ".ypk";
        }

        public void InstallProject(string mdpro3Directory, string customProjectDirectory)
        {
            ValidateProjectRoots(mdpro3Directory, customProjectDirectory);

            ProjectInstallPaths source = BuildPaths(customProjectDirectory);
            ProjectInstallPaths destination = BuildPaths(mdpro3Directory);

            log("Installing custom card project...", ProjectManagerLogLevel.Info);
            int copied = 0;
            copied += CopyMatching(source.Expansions, destination.Expansions, file => HasExtension(file, ".cdb"));
            copied += CopyMatching(source.Scripts, destination.Scripts, file => HasExtension(file, ".lua"));
            copied += CopyMatching(source.Art2, destination.Art2, IsImage);
            copied += CopyMatching(source.Closeups, destination.Closeups, IsImage, allowMissingSource: true);
            copied += CopyMatching(source.OverFrame, destination.OverFrame, IsImage, allowMissingSource: true);
            copied += CopyMatching(source.Expansions, destination.Expansions, file => HasExtension(file, ".conf"));
            copied += CopyMatching(source.MonsterCutin2, destination.MonsterCutin2, _ => true, allowMissingSource: true);
            log($"Project install finished. {copied} file(s) copied.", ProjectManagerLogLevel.Success);
        }

        public void UninstallProject(string mdpro3Directory, string customProjectDirectory)
        {
            ValidateProjectRoots(mdpro3Directory, customProjectDirectory);

            ProjectInstallPaths source = BuildPaths(customProjectDirectory);
            ProjectInstallPaths destination = BuildPaths(mdpro3Directory);

            log("Uninstalling custom card project...", ProjectManagerLogLevel.Info);
            int deleted = 0;
            deleted += DeleteMatching(source.Expansions, destination.Expansions, file => HasExtension(file, ".cdb"));
            deleted += DeleteMatching(source.Scripts, destination.Scripts, file => HasExtension(file, ".lua"));
            deleted += DeleteMatching(source.Art2, destination.Art2, IsImage);
            deleted += DeleteMatching(source.Closeups, destination.Closeups, IsImage, allowMissingSource: true);
            deleted += DeleteMatching(source.OverFrame, destination.OverFrame, IsImage, allowMissingSource: true);
            deleted += DeleteMatching(source.Expansions, destination.Expansions, file => HasExtension(file, ".conf"));
            deleted += DeleteMatching(source.MonsterCutin2, destination.MonsterCutin2, _ => true, allowMissingSource: true);
            log($"Project uninstall finished. {deleted} file(s) deleted.", ProjectManagerLogLevel.Success);
        }

        public ProjectPackageResult PackageProject(string customProjectDirectory, string packageFile)
        {
            ValidateDirectory(customProjectDirectory, "custom project directory");
            if (string.IsNullOrWhiteSpace(packageFile))
            {
                throw new InvalidOperationException("Package file path is empty.");
            }

            string packageDirectory = Path.GetDirectoryName(packageFile);
            if (!string.IsNullOrWhiteSpace(packageDirectory))
            {
                Directory.CreateDirectory(packageDirectory);
            }

            string tempPackageFile = packageFile + ".tmp";
            if (File.Exists(tempPackageFile))
            {
                File.Delete(tempPackageFile);
            }

            log("Packaging custom card project...", ProjectManagerLogLevel.Info);
            ProjectInstallPaths paths = BuildPaths(customProjectDirectory);
            HashSet<string> entryNames = new(StringComparer.OrdinalIgnoreCase);
            int count = 0;

            using (ZipArchive archive = ZipFile.Open(tempPackageFile, ZipArchiveMode.Create))
            {
                count += AddFiles(archive, entryNames, paths.Expansions, "*.cdb", "", SearchOption.TopDirectoryOnly);
                count += AddFiles(archive, entryNames, paths.Expansions, "*.conf", "", SearchOption.TopDirectoryOnly, ExcludePackageConfig);
                count += AddFiles(archive, entryNames, paths.Scripts, "*", "script", SearchOption.AllDirectories, file => HasExtension(file, ".lua"));
                count += AddFiles(archive, entryNames, paths.Closeups, "*", "pics", SearchOption.AllDirectories, IsImage, allowMissingSource: true);
                count += AddFiles(archive, entryNames, paths.Art2, "*", "art", SearchOption.AllDirectories, IsImage, allowMissingSource: true);
                count += AddFiles(archive, entryNames, paths.OverFrame, "*", "Picture/OverFrame", SearchOption.AllDirectories, IsImage, allowMissingSource: true);
                count += AddFiles(archive, entryNames, paths.OverFrame, "*", "pics/overframe", SearchOption.AllDirectories, IsImage, allowMissingSource: true);
                count += AddFiles(archive, entryNames, Path.Combine(paths.Root, "Deck"), "*.ydk", "pack", SearchOption.TopDirectoryOnly, allowMissingSource: true);
                count += AddFiles(archive, entryNames, paths.MonsterCutin2, "*", "MDPro3_Data/StandaloneWindows64/MonsterCutin2", SearchOption.AllDirectories, _ => true, allowMissingSource: true);
            }

            if (count == 0)
            {
                File.Delete(tempPackageFile);
                throw new InvalidOperationException("No packageable project files were found.");
            }

            if (File.Exists(packageFile))
            {
                File.Delete(packageFile);
            }

            File.Move(tempPackageFile, packageFile);
            log($"Package created: {packageFile}", ProjectManagerLogLevel.Success);
            log($"Packed {count} file(s).", ProjectManagerLogLevel.Success);
            return new ProjectPackageResult(packageFile, count);
        }

        public void InstallVoicePack(string voicePackDirectory, string mdpro3Directory)
        {
            ValidateDirectory(mdpro3Directory, "MDPro3 directory");
            ValidateDirectory(voicePackDirectory, "custom voice pack directory");

            log("Installing custom voice pack...", ProjectManagerLogLevel.Info);
            int copied = 0;
            foreach (string sourceFile in Directory.EnumerateFiles(voicePackDirectory, "*", SearchOption.AllDirectories))
            {
                string relativeFile = Path.GetRelativePath(voicePackDirectory, sourceFile);
                string relativeDirectory = Path.GetDirectoryName(relativeFile) ?? string.Empty;
                if (string.IsNullOrEmpty(relativeDirectory) || relativeDirectory == ".")
                {
                    continue;
                }

                string destinationFile = Path.Combine(mdpro3Directory, relativeFile);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

                if (File.Exists(destinationFile))
                {
                    if (FilesEqual(sourceFile, destinationFile))
                    {
                        log($"Skipping identical file: {destinationFile}", ProjectManagerLogLevel.Info);
                        continue;
                    }

                    string backupFile = destinationFile + ".backup";
                    if (File.Exists(backupFile))
                    {
                        File.Delete(backupFile);
                    }

                    File.Move(destinationFile, backupFile);
                    log($"Backed up existing file: {backupFile}", ProjectManagerLogLevel.Warning);
                }

                File.Copy(sourceFile, destinationFile, overwrite: true);
                copied++;
                log($"Copied {sourceFile} -> {destinationFile}", ProjectManagerLogLevel.Success);
            }

            log($"Voice pack install finished. {copied} file(s) copied.", ProjectManagerLogLevel.Success);
        }

        public void UninstallVoicePack(string voicePackDirectory, string mdpro3Directory)
        {
            ValidateDirectory(mdpro3Directory, "MDPro3 directory");
            ValidateDirectory(voicePackDirectory, "custom voice pack directory");

            log("Uninstalling custom voice pack...", ProjectManagerLogLevel.Info);
            int removed = 0;
            int restored = 0;
            foreach (string sourceFile in Directory.EnumerateFiles(voicePackDirectory, "*", SearchOption.AllDirectories))
            {
                string relativeFile = Path.GetRelativePath(voicePackDirectory, sourceFile);
                string relativeDirectory = Path.GetDirectoryName(relativeFile) ?? string.Empty;
                if (string.IsNullOrEmpty(relativeDirectory) || relativeDirectory == ".")
                {
                    continue;
                }

                string destinationFile = Path.Combine(mdpro3Directory, relativeFile);
                string backupFile = destinationFile + ".backup";

                if (File.Exists(destinationFile))
                {
                    if (FilesEqual(sourceFile, destinationFile))
                    {
                        File.Delete(destinationFile);
                        removed++;
                        log($"Removed installed voice file: {destinationFile}", ProjectManagerLogLevel.Success);
                    }
                    else
                    {
                        log($"Leaving modified file in place: {destinationFile}", ProjectManagerLogLevel.Warning);
                    }
                }

                if (File.Exists(backupFile))
                {
                    if (File.Exists(destinationFile))
                    {
                        log($"Backup not restored because a file still exists: {destinationFile}", ProjectManagerLogLevel.Warning);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                    File.Move(backupFile, destinationFile);
                    restored++;
                    log($"Restored backup: {destinationFile}", ProjectManagerLogLevel.Success);
                }
            }

            log($"Voice pack uninstall finished. {removed} file(s) removed, {restored} backup(s) restored.", ProjectManagerLogLevel.Success);
        }

        public void RestartMdPro3(string mdpro3Directory)
        {
            ValidateDirectory(mdpro3Directory, "MDPro3 directory");

            string executablePath = Path.Combine(mdpro3Directory, "MDPro3.exe");
            if (!File.Exists(executablePath))
            {
                throw new InvalidOperationException($"MDPro3.exe was not found at '{executablePath}'.");
            }

            foreach (Process process in Process.GetProcessesByName("MDPro3"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                    log($"Closed running MDPro3 process: {process.Id}", ProjectManagerLogLevel.Success);
                }
                catch (Exception ex)
                {
                    log($"Failed to close MDPro3 process {process.Id}: {ex.Message}", ProjectManagerLogLevel.Warning);
                }
            }

            Process.Start(new ProcessStartInfo(executablePath)
            {
                WorkingDirectory = mdpro3Directory,
                UseShellExecute = true
            });
            log($"Started MDPro3 from {executablePath}", ProjectManagerLogLevel.Success);
        }

        private void ValidateProjectRoots(string mdpro3Directory, string customProjectDirectory)
        {
            ValidateDirectory(mdpro3Directory, "MDPro3 directory");
            ValidateDirectory(customProjectDirectory, "custom project directory");

            if (SamePath(mdpro3Directory, customProjectDirectory))
            {
                throw new InvalidOperationException("The MDPro3 directory and custom project directory cannot be the same.");
            }
        }

        private static void ValidateDirectory(string path, string label)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The {label} does not exist: '{path}'.");
            }
        }

        private int CopyMatching(string sourceDirectory, string destinationDirectory, Func<string, bool> predicate, bool allowMissingSource = false)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                if (!allowMissingSource)
                {
                    log($"Source folder not found: {sourceDirectory}", ProjectManagerLogLevel.Warning);
                }

                return 0;
            }

            Directory.CreateDirectory(destinationDirectory);
            int count = 0;
            foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory))
            {
                if (!predicate(sourceFile))
                {
                    continue;
                }

                string destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFile));
                File.Copy(sourceFile, destinationFile, overwrite: true);
                count++;
                log($"Copied {sourceFile} -> {destinationFile}", ProjectManagerLogLevel.Success);
            }

            return count;
        }

        private int AddFiles(
            ZipArchive archive,
            HashSet<string> entryNames,
            string sourceDirectory,
            string searchPattern,
            string packageDirectory,
            SearchOption searchOption,
            Func<string, bool> predicate = null,
            bool allowMissingSource = false)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                if (!allowMissingSource)
                {
                    log($"Package source folder not found: {sourceDirectory}", ProjectManagerLogLevel.Warning);
                }

                return 0;
            }

            int count = 0;
            foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory, searchPattern, searchOption))
            {
                if (predicate != null && !predicate(sourceFile))
                {
                    continue;
                }

                string relativeFile = Path.GetRelativePath(sourceDirectory, sourceFile);
                string entryName = CombineEntryName(packageDirectory, relativeFile);
                if (!entryNames.Add(entryName))
                {
                    log($"Skipping duplicate package entry: {entryName}", ProjectManagerLogLevel.Warning);
                    continue;
                }

                archive.CreateEntryFromFile(sourceFile, entryName, CompressionLevel.Optimal);
                count++;
            }

            if (count > 0)
            {
                log($"Packed {count} file(s) from {sourceDirectory}", ProjectManagerLogLevel.Success);
            }

            return count;
        }

        private int DeleteMatching(string sourceDirectory, string destinationDirectory, Func<string, bool> predicate, bool allowMissingSource = false)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                if (!allowMissingSource)
                {
                    log($"Source folder not found: {sourceDirectory}", ProjectManagerLogLevel.Warning);
                }

                return 0;
            }

            if (SamePath(sourceDirectory, destinationDirectory))
            {
                log($"Skipping deletion because source and destination match: {sourceDirectory}", ProjectManagerLogLevel.Warning);
                return 0;
            }

            int count = 0;
            foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory))
            {
                if (!predicate(sourceFile))
                {
                    continue;
                }

                string destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFile));
                if (!File.Exists(destinationFile))
                {
                    continue;
                }

                File.Delete(destinationFile);
                count++;
                log($"Deleted {destinationFile}", ProjectManagerLogLevel.Success);
            }

            return count;
        }

        private static bool HasExtension(string file, string extension)
        {
            return Path.GetExtension(file).Equals(extension, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsImage(string file)
        {
            string extension = Path.GetExtension(file);
            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ExcludePackageConfig(string file)
        {
            return !Path.GetFileName(file).Equals("corres_srv.ini", StringComparison.OrdinalIgnoreCase);
        }

        private static string CombineEntryName(string packageDirectory, string relativeFile)
        {
            string entryName = string.IsNullOrWhiteSpace(packageDirectory)
                ? relativeFile
                : Path.Combine(packageDirectory, relativeFile);

            return entryName.Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }

        internal static bool SamePath(string left, string right)
        {
            return NormalizePath(left).Equals(NormalizePath(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool FilesEqual(string left, string right)
        {
            FileInfo leftInfo = new(left);
            FileInfo rightInfo = new(right);
            if (leftInfo.Length != rightInfo.Length)
            {
                return false;
            }

            const int bufferSize = 81920;
            using FileStream leftStream = File.OpenRead(left);
            using FileStream rightStream = File.OpenRead(right);
            byte[] leftBuffer = new byte[bufferSize];
            byte[] rightBuffer = new byte[bufferSize];

            while (true)
            {
                int leftRead = leftStream.Read(leftBuffer, 0, leftBuffer.Length);
                int rightRead = rightStream.Read(rightBuffer, 0, rightBuffer.Length);
                if (leftRead != rightRead)
                {
                    return false;
                }

                if (leftRead == 0)
                {
                    return true;
                }

                for (int i = 0; i < leftRead; i++)
                {
                    if (leftBuffer[i] != rightBuffer[i])
                    {
                        return false;
                    }
                }
            }
        }
    }

    internal sealed class ProjectFolderSynchronizer : IDisposable
    {
        private static readonly Regex FileNamePattern =
            new(@"(^.*\.(png|jpg|lua|cdb|conf)$|^\d+$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly string sourceDirectory;
        private readonly string destinationDirectory;
        private readonly Action<string, ProjectManagerLogLevel> log;
        private readonly object syncLock = new();
        private FileSystemWatcher watcher;

        public ProjectFolderSynchronizer(string sourceDirectory, string destinationDirectory, Action<string, ProjectManagerLogLevel> log)
        {
            this.sourceDirectory = sourceDirectory;
            this.destinationDirectory = destinationDirectory;
            this.log = log;
        }

        public bool Start()
        {
            if (!Directory.Exists(sourceDirectory))
            {
                log($"Synchronization source folder does not exist: {sourceDirectory}", ProjectManagerLogLevel.Error);
                return false;
            }

            if (!Directory.Exists(destinationDirectory))
            {
                log($"Synchronization destination folder does not exist: {destinationDirectory}", ProjectManagerLogLevel.Error);
                return false;
            }

            if (ProjectManagerService.SamePath(sourceDirectory, destinationDirectory))
            {
                log("Synchronization source and destination cannot be the same folder.", ProjectManagerLogLevel.Error);
                return false;
            }

            watcher = new FileSystemWatcher(sourceDirectory)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Created += OnCreatedOrChanged;
            watcher.Changed += OnCreatedOrChanged;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;
            watcher.EnableRaisingEvents = true;

            log("Project folder synchronization started.", ProjectManagerLogLevel.Success);
            return true;
        }

        public void Stop()
        {
            if (watcher == null)
            {
                return;
            }

            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnCreatedOrChanged;
            watcher.Changed -= OnCreatedOrChanged;
            watcher.Deleted -= OnDeleted;
            watcher.Renamed -= OnRenamed;
            watcher.Error -= OnError;
            watcher.Dispose();
            watcher = null;
            log("Project folder synchronization stopped.", ProjectManagerLogLevel.Success);
        }

        private void OnCreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            if (!ShouldSync(e.FullPath) || Directory.Exists(e.FullPath))
            {
                return;
            }

            lock (syncLock)
            {
                CopyWithRetry(e.FullPath, DestinationFor(e.FullPath));
            }
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (!ShouldSync(e.FullPath))
            {
                return;
            }

            lock (syncLock)
            {
                string destination = DestinationFor(e.FullPath);
                if (File.Exists(destination))
                {
                    File.Delete(destination);
                    log($"Synchronized delete: {destination}", ProjectManagerLogLevel.Success);
                }
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            lock (syncLock)
            {
                string oldDestination = DestinationFor(e.OldFullPath);
                string newDestination = DestinationFor(e.FullPath);

                if (ShouldSync(e.OldFullPath) && File.Exists(oldDestination))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newDestination));
                    File.Move(oldDestination, newDestination, overwrite: true);
                    log($"Synchronized rename: {oldDestination} -> {newDestination}", ProjectManagerLogLevel.Success);
                    return;
                }

                if (ShouldSync(e.FullPath) && File.Exists(e.FullPath))
                {
                    CopyWithRetry(e.FullPath, newDestination);
                }
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            log($"Synchronization error: {e.GetException().Message}", ProjectManagerLogLevel.Error);
        }

        private bool ShouldSync(string path)
        {
            return FileNamePattern.IsMatch(Path.GetFileName(path));
        }

        private string DestinationFor(string sourcePath)
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
            return Path.Combine(destinationDirectory, relativePath);
        }

        private void CopyWithRetry(string sourceFile, string destinationFile)
        {
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                    File.Copy(sourceFile, destinationFile, overwrite: true);
                    log($"Synchronized copy: {sourceFile} -> {destinationFile}", ProjectManagerLogLevel.Success);
                    return;
                }
                catch (IOException) when (attempt < 5)
                {
                    Thread.Sleep(200);
                }
                catch (UnauthorizedAccessException) when (attempt < 5)
                {
                    Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    log($"Failed to synchronize '{sourceFile}': {ex.Message}", ProjectManagerLogLevel.Error);
                    return;
                }
            }

            log($"Failed to synchronize locked file: {sourceFile}", ProjectManagerLogLevel.Error);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
