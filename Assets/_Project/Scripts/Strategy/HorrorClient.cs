using System;

namespace NocturneAnnex.Strategy
{
    /// <summary>
    /// Asks the server's horror director for one suggestion at a time. Never per frame: it enforces a minimum gap, one request in
    /// flight, a timeout and no retry (a late scare is worse than none). Failures change nothing; null transport means offline.
    /// </summary>
    public sealed class HorrorClient
    {
        public float TimeoutSeconds = 2f;
        public float MinGapSeconds = 45f;

        readonly IStrategyTransport _transport;
        readonly Action<string> _onSuggestion;
        string _pendingId; float _sentAt;
        float _nextAllowed = float.NegativeInfinity;
        int _counter;

        public ServerState State { get; private set; }
        public string LastError { get; private set; }
        public int RequestsSent { get; private set; }

        /// <param name="onSuggestion">Receives the raw response JSON, only when it echoes the pending request id.</param>
        public HorrorClient(IStrategyTransport transport, Action<string> onSuggestion)
        {
            _transport = transport; _onSuggestion = onSuggestion;
            State = transport == null ? ServerState.Offline : ServerState.Idle;
        }

        /// <param name="makeBody">Builds the request JSON from the request id this client assigns.</param>
        public bool Ask(Func<string, string> makeBody, float now)
        {
            if (_transport == null || _pendingId != null || now < _nextAllowed) return false;
            _nextAllowed = now + MinGapSeconds;
            string id = "hz-" + (++_counter);
            _pendingId = id; _sentAt = now; RequestsSent++; State = ServerState.Pending;
            _transport.Send(id, makeBody(id), r => OnResult(id, r));
            return true;
        }

        public void Tick(float now)
        {
            if (_pendingId != null && now - _sentAt >= TimeoutSeconds) Fail("timeout");
        }

        void OnResult(string id, TransportResult r)
        {
            if (id != _pendingId) return;   // late answer for a request already failed
            if (!r.Ok) { Fail(r.Error ?? "transport error"); return; }
            var s = HorrorSuggestionGate.Parse(r.Body);
            if (s == null || s.request_id != id) { Fail("bad response"); return; }
            _pendingId = null; State = ServerState.Online;
            _onSuggestion?.Invoke(r.Body);
        }

        void Fail(string why) { LastError = why; _pendingId = null; State = ServerState.Failed; }
    }
}
