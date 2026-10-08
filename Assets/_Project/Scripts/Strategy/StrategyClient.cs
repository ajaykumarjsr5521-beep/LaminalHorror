using System;

namespace NocturneAnnex.Strategy
{
    public struct TransportResult
    {
        public bool Ok;
        public string Body;
        public string Error;
    }

    /// <summary>Sends one JSON request and calls back later (possibly never). Implementations must not block the main thread.</summary>
    public interface IStrategyTransport
    {
        void Send(string requestId, string json, Action<TransportResult> done);
    }

    public enum ServerState { Offline, Idle, Pending, Online, Failed }

    /// <summary>
    /// Asks the server for a strategy on meaningful triggers only (never per frame). Enforces its own timeout, one retry and a
    /// cooldown, and ignores late or stale answers. Every failure leaves the current strategy untouched.
    /// </summary>
    public sealed class StrategyClient
    {
        public float TimeoutSeconds = 2f;
        public float CooldownSeconds = 60f;
        public int MaxRetries = 1;

        readonly IStrategyTransport _transport;
        readonly StrategyExecutor _executor;
        string _pendingId; string _pendingJson; float _sentAt; int _retriesLeft;
        float _nextAllowed = float.NegativeInfinity;
        int _counter;

        public ServerState State { get; private set; }
        public string LastError { get; private set; }
        public int RequestsSent { get; private set; }
        public int CommandsApplied { get; private set; }
        public int CommandsRejected { get; private set; }
        public string PendingRequestId => _pendingId;

        /// <param name="transport">Null means offline: Request does nothing.</param>
        public StrategyClient(IStrategyTransport transport, StrategyExecutor executor)
        {
            _transport = transport; _executor = executor;
            State = transport == null ? ServerState.Offline : ServerState.Idle;
        }

        /// <summary>Call on a trigger (new room, death, behavior shift, strategy expired). Returns false when it did not send.</summary>
        public bool Request(PlayerProfile profile, string trigger, float now)
        {
            if (_transport == null || _pendingId != null || now < _nextAllowed) return false;
            _nextAllowed = now + CooldownSeconds;
            _pendingId = "req-" + (++_counter);
            _pendingJson = StrategyWire.ToJson(StrategyRequestDto.From(profile, _pendingId, trigger));
            _retriesLeft = MaxRetries;
            Send(now);
            return true;
        }

        /// <summary>Call from a slow tick (about once a second). Handles the timeout and the single retry.</summary>
        public void Tick(float now)
        {
            if (_pendingId == null || now - _sentAt < TimeoutSeconds) return;
            if (_retriesLeft > 0) { _retriesLeft--; Send(now); return; }
            Fail("timeout");
        }

        void Send(float now)
        {
            _sentAt = now; RequestsSent++; State = ServerState.Pending;
            string id = _pendingId;
            _transport.Send(id, _pendingJson, r => OnResult(id, r));
        }

        void OnResult(string id, TransportResult r)
        {
            if (id != _pendingId) return; // late answer for a request already failed or replaced
            if (!r.Ok) { if (_retriesLeft > 0) { _retriesLeft--; Send(_sentAt); } else Fail(r.Error ?? "transport error"); return; }
            if (!StrategyWire.TryParse(r.Body, out var echoed, out var cmd, out var err)) { Reject(err); return; }
            if (echoed != id) { Reject("request id mismatch"); return; }
            if (cmd.Strategy == StrategyType.None) { Done(true); return; }
            if (!_executor.TryApply(cmd, _sentAt)) { Reject("invalid: " + _executor.LastRejection); return; }
            CommandsApplied++; Done(true);
        }

        void Reject(string why) { CommandsRejected++; LastError = why; Done(true); }

        void Fail(string why) { LastError = why; _pendingId = null; State = ServerState.Failed; }

        void Done(bool online) { _pendingId = null; State = online ? ServerState.Online : ServerState.Failed; }
    }
}
