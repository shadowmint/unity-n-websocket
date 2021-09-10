using System;

namespace n_websockets.N.Package.Websockets.Infrastructure.Logging
{
    public interface IWebSocketLogger
    {
        void Warn(string message, Exception error);
        void Info(string message);
    }
}