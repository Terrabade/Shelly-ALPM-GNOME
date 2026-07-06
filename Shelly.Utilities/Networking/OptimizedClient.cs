using System.Net;
using System.Net.Sockets;

namespace Shelly.Utilities.Networking;

public static class OptimizedClient
{
    private static SocketsHttpHandler CreateHandler(
        long connectionLifetimeMinutes = 5,
        long connectionIdleTimeoutMinutes = 2,
        int maxConnectionsPerServer = 10,
        DecompressionMethods decompressionMethods = DecompressionMethods.All,
        bool allowAutoRedirect = true,
        int maxAutomaticRedirections = 20,
        long connectionTimeoutSeconds = 30,
        bool enableMultipleHttp2Connections = true,
        bool enableMultipleHttp3Connections = true,
        CancellationToken cancellationToken = default
    ) => new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(connectionLifetimeMinutes),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(connectionIdleTimeoutMinutes),
        MaxConnectionsPerServer = maxConnectionsPerServer,
        AutomaticDecompression = decompressionMethods,
        AllowAutoRedirect = allowAutoRedirect,
        MaxAutomaticRedirections = maxAutomaticRedirections,
        ConnectTimeout = TimeSpan.FromSeconds(connectionTimeoutSeconds),
        EnableMultipleHttp2Connections = enableMultipleHttp2Connections,
        EnableMultipleHttp3Connections = enableMultipleHttp3Connections,
        ConnectCallback = async (context, connectToken) =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(connectToken, cancellationToken);
            var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, linkedCts.Token);

            // Prefer IPv4 first so a missing IPv6 route never blocks a successful connection.
            var addresses = entry.AddressList
                .OrderBy(a => a.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                .ToList();

            var pending = addresses.Select(async addr =>
            {
                var socket = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(3)); // fast per-address fallback
                    await socket.ConnectAsync(addr, context.DnsEndPoint.Port, timeoutCts.Token);
                    return socket;
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }).ToList();

            // Wait for the first task to SUCCEED, not just the first to complete.
            // A fast IPv6 "Network is unreachable" failure must not abort the whole
            // request when a slower IPv4 path would have succeeded.
            while (pending.Count > 0)
            {
                var finished = await Task.WhenAny(pending);
                pending.Remove(finished);

                if (finished.IsCompletedSuccessfully)
                {
                    // Dispose any other sockets that complete successfully later.
                    if (pending.Count > 0)
                    {
                        _ = Task.WhenAll(pending).ContinueWith(_ =>
                        {
                            foreach (var p in pending)
                                if (p.IsCompletedSuccessfully)
                                    p.Result.Dispose();
                        }, TaskScheduler.Default);
                    }

                    return new NetworkStream(finished.Result, ownsSocket: true);
                }

                // Observe the faulted task's exception and keep trying remaining addresses.
                _ = finished.Exception;
            }

            throw new HttpRequestException(
                $"Unable to connect to any address for {context.DnsEndPoint.Host}:{context.DnsEndPoint.Port}");
        },
    };

    public static HttpClient CreateClient(long httpTimeoutSeconds = 30, long connectionLifetimeMinutes = 5,
        long connectionIdleTimeoutMinutes = 2, CancellationToken cancellationToken = default) =>
        new(
            CreateHandler(connectionLifetimeMinutes, connectionIdleTimeoutMinutes,
                cancellationToken: cancellationToken), true)
        {
            Timeout = TimeSpan.FromSeconds(httpTimeoutSeconds),
            DefaultRequestHeaders = { UserAgent = { Http.UserAgent } },
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };
}