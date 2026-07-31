using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NeonClash
{
    [Serializable]
    public sealed class OnlineRoomConfigDto
    {
        public string playerId;
        public string cpuId;
        public string stageId;
        public string difficulty;
        public string outfitId;
    }

    [Serializable]
    public sealed class LegacyInputDto
    {
        public bool left, right, jump, crouch, guard, lightPunch, heavyPunch, lightKick, heavyKick, special, impact;

        public FighterCommand ToCommand()
        {
            FighterCommand command = new FighterCommand { Move = (left ? -1 : 0) + (right ? 1 : 0) };
            if (jump) command.Buttons |= FighterButtons.Jump;
            if (crouch) command.Buttons |= FighterButtons.Crouch;
            if (guard) command.Buttons |= FighterButtons.Guard;
            if (lightPunch) command.Buttons |= FighterButtons.LightPunch;
            if (heavyPunch) command.Buttons |= FighterButtons.HeavyPunch;
            if (lightKick) command.Buttons |= FighterButtons.LightKick;
            if (heavyKick) command.Buttons |= FighterButtons.HeavyKick;
            if (special) command.Buttons |= FighterButtons.Special;
            if (impact) command.Buttons |= FighterButtons.Impact;
            return command;
        }
    }

    [Serializable]
    public sealed class LegacyCombatantSnapshotDto
    {
        public float x, y, vx, vy, health, drive, attackTime, hurtTime, stunTime, flashTime, comboWindow;
        public int facing, combo, wins;
        public bool grounded, crouching, guarding, attackHit;
        public string attack;
    }

    [Serializable]
    public sealed class LegacyProjectileSnapshotDto
    {
        public float x, y, vx, life, damage;
        public int owner;
        public string color;
    }

    [Serializable]
    public sealed class LegacyMatchSnapshotDto
    {
        public LegacyCombatantSnapshotDto p1;
        public LegacyCombatantSnapshotDto p2;
        public float timer;
        public int round;
        public string roundState;
        public LegacyProjectileSnapshotDto[] projectiles;
    }

    [Serializable]
    public sealed class OnlineRoomDto
    {
        public string id, token, role, status;
        public int players, spectators;
        public OnlineRoomConfigDto config;
        public LegacyInputDto guestInput;
        public LegacyMatchSnapshotDto state;
    }

    [Serializable] internal sealed class RoomEnvelopeDto { public OnlineRoomDto room; public string error; }
    [Serializable] internal sealed class CreateRoomRequestDto { public OnlineRoomConfigDto config; }
    [Serializable] internal sealed class JoinRoomRequestDto { public string action; }
    [Serializable] internal sealed class InputRequestDto { public string token; public LegacyInputDto input; }
    [Serializable] internal sealed class StateRequestDto { public string token, status; public LegacyMatchSnapshotDto state; }

    public interface IOnlineRoomService
    {
        IEnumerator CreateRoom(OnlineRoomConfigDto config, Action<OnlineRoomDto> success, Action<string> failure);
        IEnumerator JoinRoom(string code, bool spectate, Action<OnlineRoomDto> success, Action<string> failure);
        IEnumerator PollRoom(string code, string token, Action<OnlineRoomDto> success, Action<string> failure);
        IEnumerator SendInput(string code, string token, LegacyInputDto input, Action failure);
        IEnumerator PublishState(string code, string token, LegacyMatchSnapshotDto state, string status, Action failure);
    }

    /// <summary>
    /// Compatibility adapter for the existing /api/rooms routes. Tokens remain in
    /// memory and are never written to PlayerPrefs. This transport is intentionally
    /// separate from deterministic combat so it can later be replaced by rollback or
    /// a dedicated relay without changing fighter rules.
    /// </summary>
    public sealed class CloudflareRoomService : IOnlineRoomService
    {
        public const int ProtocolVersion = 1;
        private readonly string baseUrl;

        public CloudflareRoomService(string serviceBaseUrl)
        {
            baseUrl = NormalizeBaseUrl(serviceBaseUrl);
        }

        public static string NormalizeBaseUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A room-service base URL is required.");
            string normalized = value.Trim().TrimEnd('/');
            Uri uri;
            if (!Uri.TryCreate(normalized, UriKind.Absolute, out uri)) throw new ArgumentException("Room-service URL must be absolute.");
            bool local = uri.Host == "localhost" || uri.Host == "127.0.0.1";
            if (uri.Scheme != Uri.UriSchemeHttps && !(local && uri.Scheme == Uri.UriSchemeHttp))
                throw new ArgumentException("Room service must use HTTPS except for localhost development.");
            return normalized;
        }

        public IEnumerator CreateRoom(OnlineRoomConfigDto config, Action<OnlineRoomDto> success, Action<string> failure)
        {
            return SendRoom("POST", "/api/rooms", JsonUtility.ToJson(new CreateRoomRequestDto { config = config }), success, failure);
        }

        public IEnumerator JoinRoom(string code, bool spectate, Action<OnlineRoomDto> success, Action<string> failure)
        {
            return SendRoom("POST", RoomPath(code), JsonUtility.ToJson(new JoinRoomRequestDto { action = spectate ? "spectate" : "join" }), success, failure);
        }

        public IEnumerator PollRoom(string code, string token, Action<OnlineRoomDto> success, Action<string> failure)
        {
            string path = RoomPath(code) + "?token=" + UnityWebRequest.EscapeURL(token ?? string.Empty);
            return SendRoom("GET", path, null, success, failure);
        }

        public IEnumerator SendInput(string code, string token, LegacyInputDto input, Action failure)
        {
            return SendNoContent(RoomPath(code) + "/input", JsonUtility.ToJson(new InputRequestDto { token = token, input = input }), failure);
        }

        public IEnumerator PublishState(string code, string token, LegacyMatchSnapshotDto state, string status, Action failure)
        {
            return SendNoContent(RoomPath(code) + "/state", JsonUtility.ToJson(new StateRequestDto { token = token, state = state, status = status }), failure);
        }

        private IEnumerator SendRoom(string method, string path, string json, Action<OnlineRoomDto> success, Action<string> failure)
        {
            using (UnityWebRequest request = CreateRequest(method, path, json))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    failure(request.error + " (HTTP " + request.responseCode + ")");
                    yield break;
                }
                RoomEnvelopeDto envelope = JsonUtility.FromJson<RoomEnvelopeDto>(request.downloadHandler.text);
                if (envelope == null || envelope.room == null) failure(envelope != null ? envelope.error : "Invalid room response.");
                else success(envelope.room);
            }
        }

        private IEnumerator SendNoContent(string path, string json, Action failure)
        {
            using (UnityWebRequest request = CreateRequest("POST", path, json))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) failure();
            }
        }

        private UnityWebRequest CreateRequest(string method, string path, string json)
        {
            UnityWebRequest request = new UnityWebRequest(baseUrl + path, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            if (json != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.SetRequestHeader("X-Neon-Clash-Protocol", ProtocolVersion.ToString());
            request.timeout = 10;
            return request;
        }

        private static string RoomPath(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Room code is required.");
            return "/api/rooms/" + UnityWebRequest.EscapeURL(code.Trim().ToUpperInvariant());
        }
    }

    public static class LegacyWebSnapshotAdapter
    {
        private const float CanvasCentre = 640f;
        private const float CanvasFloor = 566f;
        private const float HorizontalScale = 558f / FighterSimulation.ArenaHalfWidth;
        private const float VerticalScale = 0.15f;

        public static LegacyCombatantSnapshotDto ToLegacy(FighterState state, int wins)
        {
            return new LegacyCombatantSnapshotDto
            {
                x = CanvasCentre + state.PositionX * HorizontalScale,
                y = CanvasFloor - state.PositionY * VerticalScale,
                vx = state.VelocityX * HorizontalScale,
                vy = -state.VelocityY * VerticalScale,
                facing = state.Facing,
                health = state.Health / 1000f,
                drive = state.Drive / 1000f,
                grounded = state.Grounded,
                crouching = state.Crouching,
                guarding = state.Guarding,
                attack = ActionName(state.Action),
                attackTime = state.ActionTick / (float)FighterSimulation.TickRate,
                attackHit = state.ActionConnected,
                hurtTime = state.HurtTicks / (float)FighterSimulation.TickRate,
                stunTime = state.StunTicks / (float)FighterSimulation.TickRate,
                combo = state.ComboCount,
                comboWindow = state.ComboWindowTicks / (float)FighterSimulation.TickRate,
                wins = wins
            };
        }

        public static FighterState FromLegacy(LegacyCombatantSnapshotDto value)
        {
            FighterState state = FighterSimulation.CreateInitialState(Mathf.RoundToInt((value.x - CanvasCentre) / HorizontalScale), value.facing);
            state.PositionY = Mathf.Max(0, Mathf.RoundToInt((CanvasFloor - value.y) / VerticalScale));
            state.VelocityX = Mathf.RoundToInt(value.vx / HorizontalScale);
            state.VelocityY = Mathf.RoundToInt(-value.vy / VerticalScale);
            state.Health = Mathf.Clamp(Mathf.RoundToInt(value.health * 1000f), 0, FighterSimulation.MaxHealth);
            state.Drive = Mathf.Clamp(Mathf.RoundToInt(value.drive * 1000f), 0, FighterSimulation.MaxDrive);
            state.Grounded = value.grounded; state.Crouching = value.crouching; state.Guarding = value.guarding;
            return state;
        }

        private static string ActionName(CombatAction action)
        {
            switch (action)
            {
                case CombatAction.LightPunch: return "lightPunch";
                case CombatAction.HeavyPunch: return "heavyPunch";
                case CombatAction.LightKick: return "lightKick";
                case CombatAction.HeavyKick: return "heavyKick";
                case CombatAction.Special: return "special";
                case CombatAction.Impact: return "impact";
                default: return null;
            }
        }
    }
}
