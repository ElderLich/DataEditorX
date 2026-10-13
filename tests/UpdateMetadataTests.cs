using DataEditorX.Common;
using System.Net;
using System.Reflection;

internal static class UpdateMetadataTests
{
    internal static void Run()
    {
        Task.Run(VerifyAsync).GetAwaiter().GetResult();
        Console.WriteLine("PASS: fresh update metadata, preserved custom URLs, bounded body reads and explicit failures.");
    }

    private static async Task VerifyAsync()
    {
        const string metadata = "[DataEditorX]1.2.4[DataEditorX]\n[URL]https://example.test/DataEditorX_win64.zip[URL]";
        var requests = new List<Uri>();
        using var successClient = new HttpClient(new Handler((request, _) =>
        {
            requests.Add(request.RequestUri!);
            Assert(request.Headers.CacheControl?.NoCache == true
                && request.Headers.CacheControl.NoStore && request.Headers.CacheControl.MaxAge == TimeSpan.Zero,
                "Metadata requests must request a fresh response.");
            Assert(request.Headers.Pragma.Any(x => x.Name == "no-cache"), "Legacy proxies must also receive a revalidation hint.");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(metadata) });
        }));
        const string githubUrl = "https://raw.githubusercontent.com/Owner/Repo/main/version?token=a%2Bb&mode=release#manifest";
        var first = await ReadAsync(successClient, githubUrl, TimeSpan.FromSeconds(2));
        var second = await ReadAsync(successClient, githubUrl, TimeSpan.FromSeconds(2));
        Assert(first.Success && first.Content == metadata && second.Success, "Fresh metadata must reach the version parser unchanged.");
        Assert(requests[0] != requests[1] && requests[0].Query.Contains("_dataeditorx_check="), "Repeated GitHub checks must use distinct cache keys.");
        Assert(requests[0].Query.StartsWith("?token=a%2Bb&mode=release&", StringComparison.Ordinal)
            && requests[0].Fragment == "#manifest", "Cache busting must preserve existing parameters and fragments.");
        const string customUrl = "https://updates.example.test/version?signature=a%2Bb&mode=release#manifest";
        await ReadAsync(successClient, customUrl, TimeSpan.FromSeconds(2));
        Assert(requests[2].OriginalString == customUrl, "Custom metadata endpoints, including signed URLs, must remain unchanged.");

        using var failedClient = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
        var failed = await ReadAsync(failedClient, githubUrl, TimeSpan.FromSeconds(2));
        Assert(!failed.Success && failed.Content == "" && failed.Error.Contains("503"), "HTTP failure must not become successful empty metadata.");
        var invalid = await ReadAsync(successClient, "file:///tmp/version", TimeSpan.FromSeconds(2));
        Assert(!invalid.Success && invalid.Error.Contains("HTTP or HTTPS"), "Unsupported URLs must fail explicitly.");

        using var stalled = new StalledStream();
        using var stalledClient = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stalled)
        })));
        var read = ReadAsync(stalledClient, githubUrl, TimeSpan.FromMilliseconds(150));
        var completed = await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(3)));
        Assert(completed == read, "Timeout must also cover a body stalled after successful headers.");
        var timeout = await read;
        Assert(stalled.ReadStarted && !timeout.Success && timeout.Error.Contains("timed out"), "A stalled body must report timeout instead of up to date.");

        using var canceled = new CancellationTokenSource();
        using var cancellationStream = new StalledStream();
        using var cancellationClient = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(cancellationStream)
        })));
        var cancellationRead = ReadAsync(cancellationClient, githubUrl, TimeSpan.FromSeconds(10), canceled.Token);
        canceled.Cancel();
        var cancellationResult = await cancellationRead;
        Assert(!cancellationResult.Success && cancellationResult.Error == "Update check canceled.", "User cancellation must remain distinct from timeout.");

        CheckUpdate.URL = "https://example.test/old-release.zip";
        string version = await CheckUpdate.GetNewVersionAsync("not an absolute URL");
        Assert(version == CheckUpdate.DEFAULT && CheckUpdate.LastInfoStatus == CheckUpdate.UpdateInfoStatus.Unavailable
            && CheckUpdate.URL == "" && CheckUpdate.LastInfoError.Length > 0,
            "A failed check must clear stale release data and return failure, not a latest version.");
    }

    private static Task<(bool Success, string Content, string Error)> ReadAsync(HttpClient client, string url, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var type = typeof(CheckUpdate).Assembly.GetType("DataEditorX.Common.UpdateMetadataReader", true)!;
        var method = type.GetMethod("ReadAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (Task<(bool, string, string)>)method.Invoke(null, new object[] { client, url, timeout, cancellationToken })!;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }

    private sealed class StalledStream : Stream
    {
        internal bool ReadStarted { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadStarted = true;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
