using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NocturneAnnex.Strategy
{
    /// <summary>
    /// UnityWebRequest transport for POST {baseUrl}/strategy. Asynchronous (a coroutine on the host), so the main thread never
    /// waits. The request has its own short timeout; StrategyClient enforces the overall one as well.
    /// </summary>
    public sealed class HttpStrategyTransport : IStrategyTransport
    {
        readonly MonoBehaviour _host;
        readonly string _url;
        readonly int _timeoutSeconds;

        public HttpStrategyTransport(MonoBehaviour host, string baseUrl = "http://127.0.0.1:8765", int timeoutSeconds = 2)
        {
            _host = host; _url = baseUrl.TrimEnd('/') + "/strategy"; _timeoutSeconds = timeoutSeconds;
        }

        public void Send(string requestId, string json, Action<TransportResult> done) =>
            _host.StartCoroutine(Run(requestId, json, done));

        IEnumerator Run(string requestId, string json, Action<TransportResult> done)
        {
            using (var req = new UnityWebRequest(_url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("x-request-id", requestId);
                req.timeout = _timeoutSeconds;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                    done(new TransportResult { Ok = true, Body = req.downloadHandler.text });
                else
                    done(new TransportResult { Ok = false, Error = req.error });
            }
        }
    }
}
