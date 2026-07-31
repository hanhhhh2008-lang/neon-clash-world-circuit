using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NeonClash
{
    public enum NetworkRole { Host, Guest, Spectator }

    [Serializable]
    public sealed class ProtocolHelloV2
    {
        public int protocol = RealtimeProtocolV2.Version;
        public string buildHash;
        public string contentHash;
        public string requestedRole;
        public string region;
    }

    [Serializable]
    public sealed class ProtocolMatchConfigV2
    {
        public string firstFighterId;
        public string secondFighterId;
        public string firstCostumeId;
        public string secondCostumeId;
        public string stageId;
        public int randomSeed;
    }

    [Serializable]
    public sealed class ProtocolAssignmentV2
    {
        public string sessionId;
        public string joinCode;
        public string token;
        public string role;
        public int playerIndex;
        public int tickRate;
        public int inputDelay;
        public int maxRollbackTicks;
        public long expiresAt;
        public ProtocolMatchConfigV2 config;
    }

    [Serializable]
    public sealed class ProtocolInputV2
    {
        public int playerIndex;
        public int tick;
        public int move;
        public int buttons;
        public int ackRemoteTick;

        public NetworkInputFrame ToFrame()
        {
            return new NetworkInputFrame { Tick = tick, AckRemoteTick = ackRemoteTick, Command = new FighterCommand { Move = move, Buttons = (FighterButtons)buttons } };
        }

        public static ProtocolInputV2 FromFrame(NetworkInputFrame value, int playerIndex)
        {
            return new ProtocolInputV2 { playerIndex = playerIndex, tick = value.Tick, move = value.Command.Move, buttons = (int)value.Command.Buttons, ackRemoteTick = value.AckRemoteTick };
        }
    }

    [Serializable]
    public sealed class ProtocolChecksumV2 { public int tick; public string checksum; }

    [Serializable]
    public sealed class ProtocolKeyframeV2
    {
        public int tick;
        public string checksum;
        public DeterministicMatchState state;
    }

    [Serializable]
    public sealed class ProtocolDesyncV2
    {
        public int tick;
        public string hostChecksum;
        public string guestChecksum;
        public bool requestKeyframe;
    }

    [Serializable]
    public sealed class ProtocolEnvelopeV2
    {
        public int protocol = RealtimeProtocolV2.Version;
        public string kind;
        public string sessionId;
        public int sequence;
        public ProtocolHelloV2 hello;
        public ProtocolAssignmentV2 assignment;
        public ProtocolInputV2 input;
        public ProtocolChecksumV2 checksum;
        public ProtocolKeyframeV2 keyframe;
        public ProtocolDesyncV2 desync;
        public string error;
    }

    public static class RealtimeProtocolV2
    {
        public const int Version = 2;
        public const string BuildRevision = "neon-clash-unity-sim-r2";
        public const int MaximumPacketBytes = 32768;

        public static string Encode(ProtocolEnvelopeV2 message)
        {
            Validate(message);
            string json = JsonUtility.ToJson(message);
            if (Encoding.UTF8.GetByteCount(json) > MaximumPacketBytes) throw new ArgumentException("Protocol message exceeds the packet limit.");
            return json;
        }

        public static ProtocolEnvelopeV2 Decode(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json) > MaximumPacketBytes)
                throw new ArgumentException("Protocol message is empty or oversized.");
            ProtocolEnvelopeV2 value = JsonUtility.FromJson<ProtocolEnvelopeV2>(json);
            Validate(value);
            return value;
        }

        public static string ChecksumText(uint checksum) { return checksum.ToString("x8"); }

        public static uint ParseChecksum(string value)
        {
            uint result;
            if (string.IsNullOrWhiteSpace(value) || value.Length != 8 || !uint.TryParse(value, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out result)) throw new ArgumentException("Checksum must be eight hexadecimal characters.");
            return result;
        }

        public static string ComputeContentHash(GameContentCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException("catalog");
            StringBuilder source = new StringBuilder(BuildRevision).Append('|').Append(catalog.SourceRevision);
            foreach (FighterDefinition fighter in catalog.Fighters)
                source.Append('|').Append(fighter.FighterId).Append(':').Append(fighter.Speed).Append(':').Append(fighter.Power).Append(':').Append(fighter.Reach)
                    .Append(':').Append(fighter.ComboSequence).Append(':').Append(fighter.UsesProjectileSpecial ? '1' : '0');
            foreach (StageDefinition stage in catalog.Stages) source.Append('|').Append(stage.StageId).Append(':').Append(stage.Motif);
            foreach (CostumeDefinition costume in catalog.Costumes) source.Append('|').Append(costume.CostumeId).Append(':').Append(costume.Cut);
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source.ToString()));
                StringBuilder text = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) text.Append(hash[i].ToString("x2"));
                return text.ToString();
            }
        }

        public static Uri NormalizeSocketUri(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A relay WebSocket URL is required.");
            Uri uri;
            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out uri)) throw new ArgumentException("Relay WebSocket URL must be absolute.");
            bool local = uri.Host == "localhost" || uri.Host == "127.0.0.1";
            if (uri.Scheme != "wss" && !(local && uri.Scheme == "ws")) throw new ArgumentException("Relay must use WSS except for localhost development.");
            return uri;
        }

        private static void Validate(ProtocolEnvelopeV2 value)
        {
            if (value == null) throw new ArgumentException("Protocol envelope is invalid.");
            if (value.protocol != Version) throw new ArgumentException("Unsupported protocol version " + value.protocol + ".");
            if (string.IsNullOrWhiteSpace(value.kind)) throw new ArgumentException("Protocol message kind is required.");
            switch (value.kind)
            {
                case "hello": if (value.hello == null || value.hello.buildHash != BuildRevision || string.IsNullOrWhiteSpace(value.hello.contentHash)) throw new ArgumentException("Hello payload is missing or incompatible."); break;
                case "assignment": if (value.assignment == null || string.IsNullOrWhiteSpace(value.assignment.sessionId) || value.assignment.config == null) throw new ArgumentException("Assignment payload is missing."); break;
                case "input":
                    if (value.input == null || value.input.playerIndex < 0 || value.input.playerIndex > 1 || value.input.tick < 0 || value.input.move < -1 || value.input.move > 1 || value.input.buttons < 0 || value.input.buttons > 511)
                        throw new ArgumentException("Input payload is invalid.");
                    break;
                case "checksum": if (value.checksum == null || value.checksum.tick < 0) throw new ArgumentException("Checksum payload is invalid."); ParseChecksum(value.checksum.checksum); break;
                case "keyframe": if (value.keyframe == null || value.keyframe.tick < 0 || value.keyframe.state.Tick != value.keyframe.tick ||
                    value.keyframe.state.Projectiles == null || value.keyframe.state.Projectiles.Length != DeterministicMatchSimulation.ProjectileCapacity)
                    throw new ArgumentException("Keyframe payload is invalid."); ParseChecksum(value.keyframe.checksum); break;
                case "desync": if (value.desync == null || value.desync.tick < 0) throw new ArgumentException("Desync payload is invalid."); break;
                case "ping": case "pong": case "peer-joined": case "peer-left": case "error": break;
                default: throw new ArgumentException("Unsupported protocol message kind " + value.kind + ".");
            }
        }
    }

    /// <summary>Desktop/mobile WebSocket transport. Tokens are sent as authorization headers, never in URLs or PlayerPrefs.</summary>
    public sealed class WebSocketRollbackTransport : IDisposable
    {
        private readonly ClientWebSocket socket = new ClientWebSocket();
        private readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource lifetime;
        private Task receiveTask;

        public bool IsConnected { get { return socket.State == WebSocketState.Open; } }
        public string LastError { get; private set; }

        public async Task ConnectAsync(string relayUrl, string bearerToken, CancellationToken cancellationToken)
        {
            if (lifetime != null) throw new InvalidOperationException("Transport is already started.");
            Uri uri = RealtimeProtocolV2.NormalizeSocketUri(relayUrl);
            if (string.IsNullOrWhiteSpace(bearerToken)) throw new ArgumentException("A relay bearer token is required.");
            socket.Options.SetRequestHeader("Authorization", "Bearer " + bearerToken.Trim());
            socket.Options.SetRequestHeader("X-Neon-Clash-Protocol", RealtimeProtocolV2.Version.ToString());
            socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await socket.ConnectAsync(uri, lifetime.Token);
            receiveTask = ReceiveLoop(lifetime.Token);
        }

        public async Task SendAsync(ProtocolEnvelopeV2 envelope, CancellationToken cancellationToken)
        {
            if (!IsConnected) throw new InvalidOperationException("Relay transport is not connected.");
            byte[] bytes = Encoding.UTF8.GetBytes(RealtimeProtocolV2.Encode(envelope));
            await sendLock.WaitAsync(cancellationToken);
            try { await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken); }
            finally { sendLock.Release(); }
        }

        public bool TryDequeue(out ProtocolEnvelopeV2 envelope)
        {
            string json;
            if (!incoming.TryDequeue(out json)) { envelope = null; return false; }
            envelope = RealtimeProtocolV2.Decode(json);
            return true;
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (lifetime != null) lifetime.Cancel();
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client close", cancellationToken);
            if (receiveTask != null)
            {
                try { await receiveTask; } catch (OperationCanceledException) { }
            }
        }

        public void Dispose()
        {
            if (lifetime != null) lifetime.Cancel();
            socket.Dispose(); sendLock.Dispose();
            if (lifetime != null) lifetime.Dispose();
        }

        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[8192];
            try
            {
                while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    using (MemoryStream message = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                            if (result.MessageType == WebSocketMessageType.Close) return;
                            if (result.MessageType != WebSocketMessageType.Text) throw new InvalidDataException("Relay sent a non-text packet.");
                            message.Write(buffer, 0, result.Count);
                            if (message.Length > RealtimeProtocolV2.MaximumPacketBytes) throw new InvalidDataException("Relay packet exceeded the size limit.");
                        } while (!result.EndOfMessage);
                        incoming.Enqueue(Encoding.UTF8.GetString(message.ToArray()));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { LastError = error.Message; }
        }
    }
}
