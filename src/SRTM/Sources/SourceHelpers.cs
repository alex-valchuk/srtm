using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using SRTM.Logging;

namespace SRTM.Sources
{
    public class SourceHelpers
    {
        /// <summary>
        /// Downloads a remote file and stores the data in the local one.
        /// </summary>
        public static bool Download(string local, string remote, bool logErrors = false)
        {
            var client = new HttpClient();
            return PerformDownload(client, local, remote, logErrors);
        }

        /// <summary>
        /// Downloads a remote file and stores the data in the local one. The given credentials are used for authorization.
        /// </summary>
        public static bool DownloadWithCredentials(NetworkCredential credentials, string local, string remote,
            bool logErrors = false)
        {
            HttpClientHandler handler = new HttpClientHandler { Credentials = credentials };
            var client = new HttpClient(handler);
            return PerformDownload(client, local, remote, logErrors);
        }

        /// <summary>
        /// Downloads a remote file and stores the data in the local one.
        /// </summary>
        public static Task<bool> DownloadAsync(string local, string remote, CancellationToken cancellationToken, bool logErrors = false)
        {
            var client = new HttpClient();
            return PerformDownloadAsync(client, local, remote, cancellationToken, logErrors);
        }

        /// <summary>
        /// Downloads a remote file and stores the data in the local one. The given credentials are used for authorization.
        /// </summary>
        public static Task<bool> DownloadWithCredentials(NetworkCredential credentials, string local, string remote, CancellationToken cancellationToken,
            bool logErrors = false)
        {
            HttpClientHandler handler = new HttpClientHandler { Credentials = credentials };
            var client = new HttpClient(handler);
            return PerformDownloadAsync(client, local, remote, cancellationToken, logErrors);
        }

        private static bool PerformDownload(HttpClient client, string local, string remote, bool logErrors = false)
        {
            var Logger = LogProvider.For<SourceHelpers>();

            try
            {
                if (File.Exists(local))
                {
                    File.Delete(local);
                }

                using (var stream = client.GetStreamAsync(remote).Result)
                using (var outputStream = File.OpenWrite(local))
                {
                    stream.CopyTo(outputStream);
                }
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (logErrors)
                {
                    Logger.ErrorException("Download failed.", ex);
                }
            }
            return false;
        }

        private const int _maxBufferSize = 81920;
        private static async Task<bool> PerformDownloadAsync(HttpClient client, string local, string remote, CancellationToken cancellationToken, bool logErrors = false)
        {
            var Logger = LogProvider.For<SourceHelpers>();

            try
            {
                if (File.Exists(local))
                {
                    File.Delete(local);
                }

                using (var stream = await client.GetStreamAsync(remote))
                using (var outputStream = File.OpenWrite(local))
                {
                    await stream.CopyToAsync(outputStream, _maxBufferSize, cancellationToken);
                }
                return true;
            }
            catch (Exception ex)
            {
                if (logErrors)
                {
                    Logger.ErrorException("Download failed.", ex);
                }
            }
            return false;
        }
    }
}
