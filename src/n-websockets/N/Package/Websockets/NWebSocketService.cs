using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using n_websockets.N.Package.Websockets.Infrastructure.Logging;
using n_websockets.N.Package.Websockets.Infrastructure.Services;
using N.Package.Infrastructure;

namespace n_websockets.N.Package.Websockets
{
    public class NWebSocketService : IBackgroundService
    {
        private readonly NWebSocketServiceConfig _config;
        private bool _running;
        private NWebSocketClient _socket;

        public NWebSocketService(NWebSocketServiceConfig config)
        {
            _config = config;
            _socket = new NWebSocketClient(config.Logger ?? new NWebSocketLogger(false), _config.StreamHandler);
        }

        public async Task RunAsync()
        {
            _running = true;
            while (_running)
            {
                await _socket.TryConnect(_config.uri, _config.connectTimeout, _config.logConnectionErrors);
                while (_socket.Connected && _running)
                {
                    try
                    {
                        await _socket.NextEvent();
                    }
                    catch (Exception)
                    {
                        // It's fine, just try to reconnect later.
                        _socket.Dispose();
                        _socket = new NWebSocketClient(_config.Logger ?? new NWebSocketLogger(false), _config.StreamHandler);
                    }
                }

                if (_running)
                {
                    Thread.Sleep(_config.reconnectInterval);
                }
            }

            _socket.Dispose();
        }

        public Task HaltAsync()
        {
            _running = false;
            return Task.CompletedTask;
        }

        public bool Running => _running;
    }
}