using DataEditorX;
using DataEditorX.Common;
using System.Reflection;
using System.Windows.Forms;

internal static class UpdateDialogTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        VerifyCancellation("Checking");
        VerifyCancellation("Downloading");
        VerifyActionPrompts();
        VerifyDownloadFailure();
        VerifyInstallerHandoff();
        VerifyInstallerFailure(false);
        VerifyInstallerFailure(true);
        VerifyQuietCompletion();
        Console.WriteLine("PASS: update dialog cancellation, stale progress, prompts, installer handoff/failure and quiet completion.");
    }

    private static void VerifyCancellation(string stage)
    {
        using var form = Open();
        SetRunning(form, stage);
        Assert(form.AttemptClose(CloseReason.UserClosing), "Closing active update work must wait for cancellation to settle");
        Assert(Field<CancellationTokenSource>(form, "cancellation").IsCancellationRequested, "Closing active update work must request cancellation");
        Assert(Text(form, "statusLabel") == "Cancelling..." && !Field<Button>(form, "cancelButton").Enabled,
            "A real cancellation must disable the Cancel button and explain the wait");
        Assert(form.AttemptClose(CloseReason.UserClosing), "Repeated close attempts must not dispose controls still used by asynchronous work");
        Progress(form);
        Assert(Text(form, "statusLabel") == "Cancelling...", "Queued download progress must not overwrite cancellation status");
        Call(form, "CompleteCancelled");
        Assert(Text(form, "titleLabel") == "Update cancelled" && !form.AttemptClose(CloseReason.UserClosing),
            "Once work settles the cancellation result must be closable");
        Progress(form);
        Assert(Text(form, "titleLabel") == "Update cancelled" && Text(form, "statusLabel") == "Finished.",
            "Queued progress must not overwrite the completed result");
    }

    private static void VerifyActionPrompts()
    {
        using (var form = Open())
        {
            var download = WaitForAction(form, "Download");
            Call(form, "PrimaryButtonClick", form, EventArgs.Empty);
            Assert(download.IsCompletedSuccessfully && download.Result, "Download must resolve the pending action exactly once");
            var install = WaitForAction(form, "Install and Restart");
            Progress(form);
            Assert(Text(form, "statusLabel") == "Ready", "Queued download progress must not replace the installation prompt");
            Assert(!form.AttemptClose(CloseReason.UserClosing), "Closing an action prompt must close immediately");
            Assert(install.IsCompletedSuccessfully && !install.Result, "Closing the install prompt must not start installation");
        }
        using (var form = Open())
        {
            var download = WaitForAction(form, "Download");
            Call(form, "CancelButtonClick", form, EventArgs.Empty);
            Assert(download.IsCompletedSuccessfully && !download.Result && form.IsDisposed,
                "The Close button must decline the pending download and dispose the dialog");
            Progress(form);
        }
    }

    private static void VerifyDownloadFailure()
    {
        using var form = Open();
        SetRunning(form, "Downloading");
        Progress(form);
        Assert(Text(form, "statusLabel") == "Downloading... 50%", "Live progress must remain visible while downloading");
        Call(form, "Complete", "Download failed", "The archive was interrupted.", true);
        Progress(form);
        Assert(Text(form, "titleLabel") == "Download failed" && Text(form, "statusLabel") == "The update could not continue.",
            "Queued progress must not hide a failed download");
        Assert(!form.AttemptClose(CloseReason.UserClosing) && Field<Button>(form, "cancelButton").Enabled,
            "A failed download must leave a closable dialog");
    }

    private static void VerifyInstallerHandoff()
    {
        using var form = Open();
        bool launched = false;
        bool exited = false;
        Call(form, "StartInstaller", "test-update.zip", new Func<string, bool>(path =>
        {
            launched = path == "test-update.zip";
            Assert(!Field<Button>(form, "cancelButton").Enabled, "Starting an installer is not a cancellable download");
            Assert(form.AttemptClose(CloseReason.UserClosing), "The dialog must not close halfway through starting the installer");
            Assert(!Field<CancellationTokenSource>(form, "cancellation").IsCancellationRequested,
                "Closing while starting the installer must not masquerade as download cancellation");
            return true;
        }), new Action(() =>
        {
            exited = true;
            Assert(!form.AttemptClose(CloseReason.ApplicationExitCall), "Successful installer handoff must allow Application.Exit");
            Assert(!Field<CancellationTokenSource>(form, "cancellation").IsCancellationRequested,
                "Successful application shutdown must not cancel the update");
            Assert(Text(form, "statusLabel").StartsWith("Installer started."), "Successful handoff must retain installer status");
            // Simulate another form vetoing exit. No real process is launched or closed.
        }));
        Assert(launched && exited, "A successful installer launch must request application exit");
        Assert(Text(form, "titleLabel") == "Installer is waiting" && Text(form, "detailLabel").Contains("close DataEditorX"),
            "If another form keeps the application open, the dialog must explain what the installer is waiting for");
        Assert(Field<Button>(form, "cancelButton").Enabled && Field<Button>(form, "cancelButton").Text == "Close",
            "A vetoed exit must leave a usable dialog");

        using var exiting = Open();
        Call(exiting, "StartInstaller", "test-update.zip", new Func<string, bool>(_ => true), new Action(exiting.Close));
        Assert(exiting.IsDisposed, "A successful close must not render a result into disposed controls");
    }

    private static void VerifyInstallerFailure(bool throws)
    {
        using var form = Open();
        bool exited = false;
        Call(form, "StartInstaller", "test-update.zip", new Func<string, bool>(_ =>
        {
            if (throws) throw new IOException("Installer launch test failure");
            return false;
        }), new Action(() => exited = true));
        Assert(!exited && Text(form, "titleLabel") == "Installer could not start",
            "Failed installer launch must report failure without exiting");
        Assert(Field<Button>(form, "cancelButton").Enabled && !form.AttemptClose(CloseReason.UserClosing),
            "Installer failures must leave the dialog closable");
        Assert(Text(form, "detailLabel").Contains("manually"), "Installer failure must retain the manual installation fallback");
    }

    private static void VerifyQuietCompletion()
    {
        using var form = Open();
        SetRunning(form, "Checking");
        string closingStatus = "";
        form.FormClosing += (_, _) => closingStatus = Text(form, "statusLabel");
        Call(form, "CloseIfOpen");
        Assert(form.IsDisposed && closingStatus == "Active", "A quiet completed check must close without pretending the user cancelled");
    }

    private static TestUpdateForm Open()
    {
        var form = new TestUpdateForm { Opacity = 0, ShowInTaskbar = false };
        form.Show();
        return form;
    }

    private static void SetRunning(UpdateProgressForm form, string stage)
    {
        var stageType = typeof(UpdateProgressForm).GetNestedType("UpdateStage", BindingFlags.NonPublic)!;
        Call(form, "SetRunning", Enum.Parse(stageType, stage), "Active update", "Active", "");
    }

    private static Task<bool> WaitForAction(UpdateProgressForm form, string text)
        => (Task<bool>)Call(form, "WaitForActionAsync", "Update available", "Ready", "", text)!;

    private static void Progress(UpdateProgressForm form)
        => Call(form, "UpdateDownloadProgress", new CheckUpdate.DownloadProgress { BytesReceived = 50, TotalBytes = 100 });

    private static object Call(UpdateProgressForm form, string method, params object[] arguments)
        => typeof(UpdateProgressForm).GetMethod(method, PrivateInstance)!.Invoke(form, arguments);

    private static T Field<T>(UpdateProgressForm form, string field)
        => (T)typeof(UpdateProgressForm).GetField(field, PrivateInstance)!.GetValue(form)!;

    private static string Text(UpdateProgressForm form, string field) => Field<Label>(form, field).Text;

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class TestUpdateForm : UpdateProgressForm
    {
        // Suppress the real network check; exercise the actual form lifecycle with
        // simulated updater boundaries instead of installing or closing the app.
        protected override void OnShown(EventArgs e) { }

        public bool AttemptClose(CloseReason reason)
        {
            var args = new FormClosingEventArgs(reason, false);
            base.OnFormClosing(args);
            return args.Cancel;
        }
    }
}
