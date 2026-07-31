using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NeonClash
{
    [Serializable] internal sealed class RelayHelloRequestV2 { public ProtocolHelloV2 hello; }
    [Serializable] internal sealed class RelayCreateRequestV2 { public ProtocolHelloV2 hello; public ProtocolMatchConfigV2 config; }
    [Serializable] internal sealed class RelayAssignmentResponseV2 { public ProtocolAssignmentV2 assignment; public string error; public string message; }

    /// <summary>HTTPS lobby client for protocol 2. Credentials stay only in the returned in-memory assignment.</summary>
    public sealed class RelaySessionServiceV2
    {
        private readonly string baseUrl;
        private readonly string contentHash;
        private readonly string region;

        public RelaySessionServiceV2(string serviceBaseUrl, GameContentCatalog catalog, string preferredRegion)
        {
            baseUrl = CloudflareRoomService.NormalizeBaseUrl(serviceBaseUrl);
            contentHash = RealtimeProtocolV2.ComputeContentHash(catalog);
            region = string.IsNullOrWhiteSpace(preferredRegion) ? "auto" : preferredRegion.Trim().ToLowerInvariant();
        }

        public IEnumerator Create(ProtocolMatchConfigV2 config, Action<ProtocolAssignmentV2> success, Action<string> failure)
        {
            if (config == null) throw new ArgumentNullException("config");
            RelayCreateRequestV2 body = new RelayCreateRequestV2 { hello = Hello("host"), config = config };
            return Send("/v2/sessions", JsonUtility.ToJson(body), success, failure);
        }

        public IEnumerator Join(string code, bool spectate, Action<ProtocolAssignmentV2> success, Action<string> failure)
        {
            string normalized = NormalizeCode(code);
            string role = spectate ? "spectator" : "guest";
            RelayHelloRequestV2 body = new RelayHelloRequestV2 { hello = Hello(role) };
            return Send("/v2/sessions/" + UnityWebRequest.EscapeURL(normalized) + "/" + (spectate ? "spectate" : "join"),
                JsonUtility.ToJson(body), success, failure);
        }

        public string SocketUrl(ProtocolAssignmentV2 assignment)
        {
            if (assignment == null || string.IsNullOrWhiteSpace(assignment.joinCode)) throw new ArgumentException("Relay assignment is incomplete.");
            Uri source = new Uri(baseUrl);
            UriBuilder socket = new UriBuilder(source) { Scheme = source.Scheme == Uri.UriSchemeHttps ? "wss" : "ws", Port = source.IsDefaultPort ? -1 : source.Port,
                Path = "/v2/sessions/" + assignment.joinCode + "/socket", Query = string.Empty };
            return socket.Uri.AbsoluteUri;
        }

        private ProtocolHelloV2 Hello(string role)
        {
            return new ProtocolHelloV2 { buildHash = RealtimeProtocolV2.BuildRevision, contentHash = contentHash, requestedRole = role, region = region };
        }

        private IEnumerator Send(string path, string json, Action<ProtocolAssignmentV2> success, Action<string> failure)
        {
            using (UnityWebRequest request = new UnityWebRequest(baseUrl + path, "POST"))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                if (bytes.Length > RealtimeProtocolV2.MaximumPacketBytes) throw new ArgumentException("Lobby request exceeds the protocol packet limit.");
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Neon-Clash-Protocol", RealtimeProtocolV2.Version.ToString());
                request.timeout = 12;
                yield return request.SendWebRequest();
                RelayAssignmentResponseV2 response = null;
                if (!string.IsNullOrWhiteSpace(request.downloadHandler.text))
                    response = JsonUtility.FromJson<RelayAssignmentResponseV2>(request.downloadHandler.text);
                if (request.result != UnityWebRequest.Result.Success || response == null || response.assignment == null)
                {
                    string detail = response != null && !string.IsNullOrWhiteSpace(response.message) ? response.message : request.error;
                    failure((detail ?? "Relay returned an invalid response.") + " (HTTP " + request.responseCode + ")");
                }
                else success(response.assignment);
            }
        }

        private static string NormalizeCode(string value)
        {
            string result = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (result.Length != 6) throw new ArgumentException("Join code must contain six characters.");
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            for (int i = 0; i < result.Length; i++) if (alphabet.IndexOf(result[i]) < 0) throw new ArgumentException("Join code contains an ambiguous or invalid character.");
            return result;
        }
    }
}
