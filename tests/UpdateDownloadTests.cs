#nullable enable
using DataEditorX.Common;
using System.Diagnostics;
using System.Reflection;

internal static class UpdateDownloadTests
{
    internal static void Run()
    {
        Task.Run(VerifyAsync).GetAwaiter().GetResult();
        Console.WriteLine("PASS: progressing update downloads, inactivity timeout, prompt cancellation and truncated-body rejection.");
    }

    private static async Task VerifyAsync()
    {
        byte[] payload = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        var updates = new List<CheckUpdate.DownloadProgress>();
        using (var source = new DelayedStream(payload, TimeSpan.FromMilliseconds(125), 2))
        using (var destination = new MemoryStream())
        {
            var elapsed = Stopwatch.StartNew();
            long copied = await CopyAsync(source, destination, payload.Length, TimeSpan.FromMilliseconds(500), new InlineProgress(updates.Add));
            Assert(elapsed.Elapsed > TimeSpan.FromMilliseconds(500), "The fixture must exceed a single inactivity deadline overall.");
            Assert(copied == payload.Length && destination.ToArray().SequenceEqual(payload), "A slow but progressing download must finish without corrupting its bytes.");
            Assert(updates.Count == 8 && updates[^1].BytesReceived == payload.Length && updates[^1].Percent == 100,
                "Successful download progress must report actual bytes and reach the declared total.");
        }

        using (var source = new StalledStream())
        using (var destination = new MemoryStream())
        {
            var copy = CopyAsync(source, destination, null, TimeSpan.FromMilliseconds(150));
            Assert(await Task.WhenAny(copy, Task.Delay(TimeSpan.FromSeconds(3))) == copy,
                "A stalled body must stop even when no user cancellation arrives.");
            await ExpectAsync<TimeoutException>(copy, ex => ex.Message.Contains("stalled"));
        }

        using (var source = new StalledStream())
        using (var destination = new MemoryStream())
        using (var cancellation = new CancellationTokenSource())
        {
            var copy = CopyAsync(source, destination, null, TimeSpan.FromSeconds(30), cancellationToken: cancellation.Token);
            await source.Started.Task;
            cancellation.Cancel();
            Assert(await Task.WhenAny(copy, Task.Delay(TimeSpan.FromSeconds(2))) == copy,
                "User cancellation must abort a pending read without waiting for the inactivity deadline.");
            await ExpectAsync<OperationCanceledException>(copy, ex => ex.CancellationToken == cancellation.Token);
        }

        using (var source = new MemoryStream(payload))
        using (var destination = new MemoryStream())
        {
            await ExpectAsync<InvalidDataException>(CopyAsync(source, destination, payload.Length + 3, TimeSpan.FromSeconds(1)),
                ex => ex.Message.Contains("ended early") && ex.Message.Contains("16 of 19"));
        }
        using (var source = new MemoryStream(payload))
        using (var destination = new MemoryStream())
        {
            await ExpectAsync<InvalidDataException>(CopyAsync(source, destination, payload.Length - 1, TimeSpan.FromSeconds(1)),
                ex => ex.Message.Contains("larger"));
        }
        using (var source = new MemoryStream(payload))
        using (var destination = new MemoryStream())
        {
            long copied = await CopyAsync(source, destination, null, TimeSpan.FromSeconds(1));
            Assert(copied == payload.Length && destination.ToArray().SequenceEqual(payload),
                "Chunked downloads without Content-Length must remain supported.");
        }
    }

    private static Task<long> CopyAsync(Stream source, Stream destination, long? totalBytes, TimeSpan timeout,
        IProgress<CheckUpdate.DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var type = typeof(CheckUpdate).Assembly.GetType("DataEditorX.Common.UpdateDownloadReader", true)!;
        var method = type.GetMethod("CopyAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (Task<long>)method.Invoke(null, new object?[] { source, destination, totalBytes, timeout, progress, cancellationToken })!;
    }

    private static async Task ExpectAsync<T>(Task task, Func<T, bool> matches) where T : Exception
    {
        try { await task; }
        catch (T ex)
        {
            Assert(matches(ex), $"{typeof(T).Name} must explain the expected failure.");
            return;
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name} from the download.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class InlineProgress(Action<CheckUpdate.DownloadProgress> report) : IProgress<CheckUpdate.DownloadProgress>
    {
        public void Report(CheckUpdate.DownloadProgress value) => report(value);
    }

    private abstract class ReadOnlyStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class DelayedStream(byte[] payload, TimeSpan delay, int chunkSize) : ReadOnlyStream
    {
        private int offset;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (offset >= payload.Length) return 0;
            await Task.Delay(delay, cancellationToken);
            int count = Math.Min(buffer.Length, Math.Min(chunkSize, payload.Length - offset));
            payload.AsMemory(offset, count).CopyTo(buffer);
            offset += count;
            return count;
        }
    }

    private sealed class StalledStream : ReadOnlyStream
    {
        internal TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }
}
