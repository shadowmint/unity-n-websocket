using n_websockets.N.Package.Websockets.Infrastructure.Logging;
using UnityEngine;

namespace n_websockets.N.Package.Websockets
{
    [System.Serializable]
    public class NWebSocketServiceConfig
    {
        [Tooltip("The endpoint to connect to. eg. ws://localhost:3000")]
        public string uri;

        [Tooltip("The connection timeout in ms")]
        public int connectTimeout = 2000;

        [Tooltip("How long to wait between attempts to reconnect")]
        public int reconnectInterval = 1000;

        public bool debug;
        
        public bool logConnectionErrors;
        
        public int maxPointsPerFrame = 1024;

        public IWebSocketLogger Logger { get; set; }
        
        public IWebSocketStreamHandler StreamHandler { get; set; }
    }
}