using System;
using UnityEngine;

namespace n_websockets.N.Package.Websockets.Infrastructure.Logging
{
    public class NWebSocketLogger : IWebSocketLogger
    {
        private readonly bool _mainThreadContext;

        public NWebSocketLogger(bool mainThreadContext = true)
        {
            _mainThreadContext = mainThreadContext;
        }

        public void Warn(string message, Exception error)
        {
            Debug.LogWarning(message);
            Debug.LogException(error);
            if (!_mainThreadContext)
            {
                System.Diagnostics.Debug.WriteLine($"{message}: {error}");
            }
        }

        public void Info(string message)
        {
            Debug.Log(message);
            if (!_mainThreadContext)
            {
                System.Diagnostics.Debug.WriteLine(message);
            }
        }
    }
}