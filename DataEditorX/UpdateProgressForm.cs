using DataEditorX.Common;
using DataEditorX.Config;
using DataEditorX.Language;
using System.Text.RegularExpressions;

namespace DataEditorX
{
    public class UpdateProgressForm : Form
    {
        private readonly bool showNew;
        private readonly CancellationTokenSource cancellation = new();
        private readonly Label titleLabel;
        private readonly Label statusLabel;
        private readonly Label detailLabel;
        private readonly ProgressBar progressBar;
        private readonly Button primaryButton;
        private readonly Button cancelButton;
        private TaskCompletionSource<bool>? actionCompletion;
        private UpdateStage stage;

        private enum UpdateStage
        {
            Idle,
            Checking,
            Downloading,
            AwaitingAction,
            StartingInstaller,
            InstallerStarted,
            Cancelling,
            Complete
        }

        public UpdateProgressForm(bool showNew = true)
        {
            this.showNew = showNew;
            titleLabel = new Label();
            statusLabel = new Label();
            detailLabel = new Label();
            progressBar = new ProgressBar();
            primaryButton = new Button();
            cancelButton = new Button();
            InitializeComponent();
            ApplyTheme();
        }

        public static void CheckForUpdates(IWin32Window owner, bool showNew = true)
        {
            using UpdateProgressForm form = new(showNew);
            _ = form.ShowDialog(owner);
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await RunUpdateAsync();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Keep the controls alive until asynchronous network work settles,
            // including repeated close attempts after cancellation was requested.
            if (stage is UpdateStage.Checking or UpdateStage.Downloading or UpdateStage.Cancelling)
            {
                e.Cancel = true;
                RequestCancel();
                return;
            }
            if (stage == UpdateStage.StartingInstaller)
            {
                e.Cancel = true;
                return;
            }

            TaskCompletionSource<bool>? pendingAction = actionCompletion;
            actionCompletion = null;
            pendingAction?.TrySetResult(false);
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                cancellation.Cancel();
                cancellation.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            Text = "Check for Updates";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = true;
            Icon = SystemIcons.Information;
            ClientSize = new Size(500, 230);

            TableLayoutPanel root = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(18)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            titleLabel.AutoSize = true;
            titleLabel.Font = new Font(Font.FontFamily, 15, FontStyle.Bold);
            titleLabel.Margin = new Padding(0, 0, 0, 8);
            titleLabel.Text = "Checking for updates";

            statusLabel.AutoSize = true;
            statusLabel.Margin = new Padding(0, 0, 0, 12);
            statusLabel.Text = "Reading update metadata...";

            progressBar.Dock = DockStyle.Top;
            progressBar.Height = 18;
            progressBar.Margin = new Padding(0, 0, 0, 12);
            progressBar.Style = ProgressBarStyle.Marquee;

            detailLabel.Dock = DockStyle.Fill;
            detailLabel.Margin = new Padding(0);
            detailLabel.Text = "";

            FlowLayoutPanel buttons = new()
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 16, 0, 0)
            };

            cancelButton.Text = "Cancel";
            cancelButton.Size = new Size(96, 28);
            cancelButton.Click += CancelButtonClick;

            primaryButton.Text = "Download";
            primaryButton.Size = new Size(128, 28);
            primaryButton.Margin = new Padding(8, 0, 0, 0);
            primaryButton.Visible = false;
            primaryButton.Click += PrimaryButtonClick;

            buttons.Controls.Add(cancelButton);
            buttons.Controls.Add(primaryButton);

            CancelButton = cancelButton;
            root.Controls.Add(titleLabel, 0, 0);
            root.Controls.Add(statusLabel, 0, 1);
            root.Controls.Add(progressBar, 0, 2);
            root.Controls.Add(detailLabel, 0, 3);
            root.Controls.Add(buttons, 0, 4);
            Controls.Add(root);
        }

        private async Task RunUpdateAsync()
        {
            string updateUrl = DEXConfig.ReadString(DEXConfig.TAG_UPDATE_URL);
            try
            {
                SetRunning(UpdateStage.Checking, "Checking for updates", "Reading update metadata...", "");
                string latestVersion = await CheckUpdate.GetNewVersionAsync(updateUrl, cancellation.Token);
                if (cancellation.IsCancellationRequested)
                {
                    CompleteCancelled();
                    return;
                }

                if (latestVersion == CheckUpdate.DEFAULT)
                {
                    if (!showNew)
                    {
                        CloseIfOpen();
                        return;
                    }

                    Complete("Update check failed", GetUpdateInfoFailureMessage(updateUrl), true);
                    return;
                }

                string currentVersion = CleanVersion(Application.ProductVersion);
                if (!CheckUpdate.CheckVersion(latestVersion, currentVersion))
                {
                    if (!showNew)
                    {
                        CloseIfOpen();
                        return;
                    }

                    Complete("DataEditorX is up to date", $"Current version: {currentVersion}\nLatest version: {latestVersion}");
                    return;
                }

                bool download = await WaitForActionAsync(
                    "Update available",
                    $"DataEditorX {latestVersion} is available.",
                    $"Current version: {currentVersion}\nDownload URL:\n{CheckUpdate.URL}",
                    "Download");
                if (!download || IsDisposed || Disposing)
                {
                    CloseIfOpen();
                    return;
                }

                string zipFile = BuildDownloadPath(latestVersion);
                Progress<CheckUpdate.DownloadProgress> progress = new(UpdateDownloadProgress);
                SetRunning(UpdateStage.Downloading, $"Downloading DataEditorX {latestVersion}", "Starting download...", "");
                bool downloaded = await CheckUpdate.DownloadAsync(zipFile, progress, cancellation.Token);
                if (cancellation.IsCancellationRequested)
                {
                    CompleteCancelled();
                    return;
                }

                if (!downloaded)
                {
                    Complete("Download failed", GetDownloadFailureMessage(), true);
                    return;
                }

                bool install = await WaitForActionAsync(
                    "Update downloaded",
                    "The update archive is ready.",
                    $"Saved to:\n{zipFile}",
                    "Install and Restart");
                if (!install || IsDisposed || Disposing)
                {
                    CloseIfOpen();
                    return;
                }

                StartInstaller(zipFile, CheckUpdate.InstallUpdate, Application.Exit);
            }
            catch (Exception ex)
            {
                if (cancellation.IsCancellationRequested)
                {
                    CompleteCancelled();
                    return;
                }

                Complete("Update failed", ex.GetBaseException().Message, true);
            }
        }

        private void StartInstaller(string zipFile, Func<string, bool> launchInstaller, Action exitApplication)
        {
            SetRunning(UpdateStage.StartingInstaller, "Starting installer", "DataEditorX will close and restart.", "");
            try
            {
                if (!launchInstaller(zipFile))
                {
                    Complete("Installer could not start", GetInstallFailureMessage(), true);
                    return;
                }

                // Application.Exit raises FormClosing on this dialog too. The
                // installer is already launched, so this is a successful handoff,
                // not cancellation of an update check or download.
                stage = UpdateStage.InstallerStarted;
                statusLabel.Text = "Installer started. Waiting for DataEditorX to close...";
                exitApplication();
                if (!IsDisposed && !Disposing && Visible)
                {
                    Complete("Installer is waiting", "Finish any active tasks, then close DataEditorX. The installer will continue when the application closes.");
                    statusLabel.Text = "DataEditorX is still open.";
                }
            }
            catch (Exception ex)
            {
                Complete("Installer could not start", GetInstallFailureMessage() + "\n\nDetails: " + ex.GetBaseException().Message, true);
            }
        }

        private Task<bool> WaitForActionAsync(string title, string status, string detail, string actionText)
        {
            if (IsDisposed || Disposing)
            {
                return Task.FromResult(false);
            }
            stage = UpdateStage.AwaitingAction;
            titleLabel.Text = title;
            statusLabel.Text = status;
            detailLabel.Text = detail;
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = 100;
            primaryButton.Text = actionText;
            primaryButton.Visible = true;
            primaryButton.Enabled = true;
            cancelButton.Text = "Close";
            cancelButton.Enabled = true;
            AcceptButton = primaryButton;
            actionCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return actionCompletion.Task;
        }

        private void SetRunning(UpdateStage stage, string title, string status, string detail)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }
            this.stage = stage;
            titleLabel.Text = title;
            statusLabel.Text = status;
            detailLabel.Text = detail;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Value = 0;
            primaryButton.Visible = false;
            cancelButton.Text = "Cancel";
            cancelButton.Enabled = stage is UpdateStage.Checking or UpdateStage.Downloading;
            AcceptButton = null;
        }

        private void Complete(string title, string detail, bool isError = false)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }
            stage = UpdateStage.Complete;
            actionCompletion = null;
            titleLabel.Text = title;
            statusLabel.Text = isError ? "The update could not continue." : "Finished.";
            detailLabel.Text = detail;
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = isError ? 0 : 100;
            primaryButton.Visible = false;
            cancelButton.Text = "Close";
            cancelButton.Enabled = true;
            AcceptButton = cancelButton;
        }

        private void CompleteCancelled()
        {
            Complete("Update cancelled", "No changes were made.");
        }

        private void UpdateDownloadProgress(CheckUpdate.DownloadProgress progress)
        {
            // Progress<T> posts to the UI queue; an old notification can arrive
            // after cancellation, completion or the install prompt.
            if (IsDisposed || Disposing || stage != UpdateStage.Downloading || cancellation.IsCancellationRequested)
            {
                return;
            }
            if (progress.TotalBytes.HasValue && progress.TotalBytes.Value > 0)
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Value = progress.Percent;
                statusLabel.Text = $"Downloading... {progress.Percent}%";
                detailLabel.Text = $"{FormatBytes(progress.BytesReceived)} of {FormatBytes(progress.TotalBytes.Value)}";
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                statusLabel.Text = "Downloading...";
                detailLabel.Text = $"{FormatBytes(progress.BytesReceived)} downloaded";
            }
        }

        private void PrimaryButtonClick(object sender, EventArgs e)
        {
            primaryButton.Visible = false;
            TaskCompletionSource<bool>? pendingAction = actionCompletion;
            actionCompletion = null;
            pendingAction?.TrySetResult(true);
        }

        private void CancelButtonClick(object sender, EventArgs e)
        {
            if (stage is UpdateStage.Checking or UpdateStage.Downloading or UpdateStage.Cancelling)
            {
                RequestCancel();
                return;
            }
            if (stage is UpdateStage.StartingInstaller or UpdateStage.InstallerStarted)
            {
                return;
            }

            TaskCompletionSource<bool>? pendingAction = actionCompletion;
            actionCompletion = null;
            pendingAction?.TrySetResult(false);
            CloseIfOpen();
        }

        private void RequestCancel()
        {
            if (stage is not (UpdateStage.Checking or UpdateStage.Downloading) || cancellation.IsCancellationRequested)
            {
                return;
            }

            stage = UpdateStage.Cancelling;
            cancelButton.Enabled = false;
            statusLabel.Text = "Cancelling...";
            cancellation.Cancel();
        }

        private void CloseIfOpen()
        {
            if (!IsDisposed && !Disposing)
            {
                // Internal completion (including a silent up-to-date check) must
                // not be interpreted by FormClosing as a user's cancellation.
                stage = UpdateStage.Complete;
                Close();
            }
        }

        private void ApplyTheme()
        {
            ThemeManager.ApplyControlTree(this);
        }

        private static string BuildDownloadPath(string version)
        {
            string downloadDir = MyPath.Combine(Path.GetTempPath(), "DataEditorX");
            MyPath.CreateDir(downloadDir);
            return MyPath.Combine(downloadDir, "DataEditorX_" + version + ".zip");
        }

        private static string GetUpdateInfoFailureMessage(string updateUrl)
        {
            string reason = CheckUpdate.LastInfoStatus switch
            {
                CheckUpdate.UpdateInfoStatus.EmptyUrl => "The update metadata URL is empty.",
                CheckUpdate.UpdateInfoStatus.InvalidFormat => "The update metadata file exists, but it does not contain a valid DataEditorX version and release URL.",
                CheckUpdate.UpdateInfoStatus.Unavailable => "The update metadata file could not be reached.",
                _ => "The update metadata could not be checked."
            };
            string detail = string.IsNullOrWhiteSpace(CheckUpdate.LastInfoError)
                ? ""
                : "\n\nDetails: " + CheckUpdate.LastInfoError;

            return reason
                + "\n\nDataEditorX checks this file before looking for release zips:"
                + "\n" + updateUrl
                + "\n\nIf you have not pushed the repo/update metadata yet, or the GitHub repo is private, this is expected."
                + detail;
        }

        private static string GetDownloadFailureMessage()
        {
            string message = LanguageHelper.GetMsg(LMSG.DownloadFail);
            return string.IsNullOrWhiteSpace(CheckUpdate.LastDownloadError)
                ? message
                : message + "\n\nDetails: " + CheckUpdate.LastDownloadError;
        }

        private static string GetInstallFailureMessage()
        {
            return "Update downloaded, but the installer could not start."
                + "\n\nYou can still install manually from the downloaded archive.";
        }

        private static string CleanVersion(string version)
        {
            return Regex.Replace(version ?? "", "\\+.*", "");
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = ["B", "KB", "MB", "GB"];
            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.0} {units[unit]}";
        }
    }
}
