using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ArgusUnity.Bridge
{
    public sealed class UnityBridgeClient : IDisposable
    {
        private readonly ConcurrentQueue<BridgeEnvelope> inboundMessages = new ConcurrentQueue<BridgeEnvelope>();
        private readonly ConcurrentQueue<StructuredError> inboundErrors = new ConcurrentQueue<StructuredError>();
        private readonly Func<ClientWebSocket> socketFactory;
        private ClientWebSocket socket;
        private CancellationTokenSource cancellationTokenSource;
        private Task receiveTask;
        private string sessionId = "unity-session";

        public UnityBridgeClient() : this(() => new ClientWebSocket())
        {
        }

        public UnityBridgeClient(Func<ClientWebSocket> socketFactory)
        {
            this.socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));
        }

        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

        public int QueuedMessageCount => inboundMessages.Count;

        public int QueuedErrorCount => inboundErrors.Count;

        public async Task ConnectAsync(Uri uri, string sessionId, CancellationToken cancellationToken = default)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            this.sessionId = string.IsNullOrWhiteSpace(sessionId) ? "unity-session" : sessionId;
            socket = socketFactory();
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await socket.ConnectAsync(uri, cancellationTokenSource.Token).ConfigureAwait(false);
            await SendReadyAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            receiveTask = Task.Run(() => ReceiveLoopAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);
        }

        public bool TryDequeueMessage(out BridgeEnvelope envelope)
        {
            return inboundMessages.TryDequeue(out envelope);
        }

        public bool TryDequeueError(out StructuredError error)
        {
            return inboundErrors.TryDequeue(out error);
        }

        public Task SendReadyAsync(CancellationToken cancellationToken = default)
        {
            return SendEnvelopeAsync(BridgeEnvelope.UnityReady(sessionId), cancellationToken);
        }

        public Task SendAckAsync(
            string acknowledgedMessageId,
            long acknowledgedSequence,
            bool applied,
            CancellationToken cancellationToken = default)
        {
            return SendEnvelopeAsync(
                BridgeEnvelope.UnityAck(sessionId, acknowledgedMessageId, acknowledgedSequence, applied),
                cancellationToken);
        }

        public Task SendErrorAsync(
            StructuredError error,
            string correlationId = null,
            CancellationToken cancellationToken = default)
        {
            return SendEnvelopeAsync(
                BridgeEnvelope.UnityError(sessionId, error, correlationId),
                cancellationToken);
        }

        public Task SendBridgeEnvelopeAsync(BridgeEnvelope envelope, CancellationToken cancellationToken = default)
        {
            return SendEnvelopeAsync(envelope, cancellationToken);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (socket == null)
            {
                return;
            }

            cancellationTokenSource?.Cancel();
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Unity client disconnect", cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            cancellationTokenSource?.Cancel();
            if (receiveTask != null && receiveTask.IsCompleted)
            {
                receiveTask.Dispose();
            }
            socket?.Dispose();
            cancellationTokenSource?.Dispose();
        }

        private async Task SendEnvelopeAsync(BridgeEnvelope envelope, CancellationToken cancellationToken)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Unity bridge client is not connected.");
            }

            var bytes = Encoding.UTF8.GetBytes(envelope.ToJson());
            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                try
                {
                    var json = await ReceiveTextFrameAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (json == null)
                    {
                        return;
                    }

                    if (BridgeEnvelope.TryParse(json, out var envelope, out var error))
                    {
                        inboundMessages.Enqueue(envelope);
                    }
                    else
                    {
                        inboundErrors.Enqueue(error);
                        if (IsConnected)
                        {
                            await SendErrorAsync(error, error.CorrelationId, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    inboundErrors.Enqueue(StructuredError.BridgeError(
                        "Unity bridge receive loop failed.",
                        null,
                        new JObject { ["exception"] = exception.Message }));
                    return;
                }
            }
        }

        private async Task<string> ReceiveTextFrameAsync(byte[] buffer, CancellationToken cancellationToken)
        {
            using (var memory = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken)
                        .ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return null;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        throw new InvalidOperationException("Unity bridge received a non-text WebSocket frame.");
                    }

                    memory.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                return Encoding.UTF8.GetString(memory.ToArray());
            }
        }
    }
}
