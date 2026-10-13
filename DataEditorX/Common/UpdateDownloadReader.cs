using System.IO;

namespace DataEditorX.Common
{
    /// <summary>
    /// Copies update downloads with an inactivity limit rather than a total transfer limit.
    /// </summary>
    internal static class UpdateDownloadReader
    {
        internal static async Task<long> CopyAsync(
            Stream source,
            Stream destination,
            long? totalBytes,
            TimeSpan inactivityTimeout,
            IProgress<CheckUpdate.DownloadProgress>? progress,
            CancellationToken cancellationToken)
        {
            if (inactivityTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(inactivityTimeout));
            if (totalBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(totalBytes));

            long received = 0;
            byte[] buffer = new byte[128 * 1024];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int count;
                // A fresh deadline for each read allows large downloads to keep making
                // progress. Local disk writes do not consume the network inactivity limit.
                using (CancellationTokenSource readDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    readDeadline.CancelAfter(inactivityTimeout);
                    try
                    {
                        count = await source.ReadAsync(buffer.AsMemory(), readDeadline.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException("Update download canceled.", cancellationToken);
                    }
                    catch (OperationCanceledException) when (readDeadline.IsCancellationRequested)
                    {
                        throw new TimeoutException("The update download stalled while waiting for more data. Please try again.");
                    }
                }

                if (count == 0)
                    break;
                received += count;
                if (totalBytes.HasValue && received > totalBytes.Value)
                    throw new InvalidDataException("The update download is larger than the size reported by the server. Please try again.");

                await destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                progress?.Report(new CheckUpdate.DownloadProgress
                {
                    BytesReceived = received,
                    TotalBytes = totalBytes
                });
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (totalBytes.HasValue && received != totalBytes.Value)
                throw new InvalidDataException($"The update download ended early: received {received} of {totalBytes.Value} bytes. Please try again.");
            return received;
        }
    }
}
