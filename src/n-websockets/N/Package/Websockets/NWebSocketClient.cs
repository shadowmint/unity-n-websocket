using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using n_websockets.N.Package.Websockets;
using n_websockets.N.Package.Websockets.Infrastructure.Logging;
using Newtonsoft.Json;
using UnityEngine;

namespace N.Package.Infrastructure
{
    public class NWebSocketClient
    {
        private bool _closed;

        private readonly IWebSocketLogger _logger;
        private readonly IWebSocketStreamHandler _streamHandler;

        public bool Connected => _state == InternalState.Connected;

        private InternalState _state;

        private ClientWebSocket _websocket;

        private ArraySegment<byte> _buffer;

        private int _totalBytes;

        public NWebSocketClient(IWebSocketLogger logger, IWebSocketStreamHandler streamHandler)
        {
            _logger = logger;
            _streamHandler = streamHandler;
            _state = InternalState.Idle;
            _totalBytes = 0;
        }

        public async Task NextEvent()
        {
            if (_websocket == null || _websocket.State != WebSocketState.Open)
            {
                _state = InternalState.Idle;
                return;
            }

            try
            {
                WebSocketWriteResult messageState;
                do
                {
                    var result = await _websocket.ReceiveAsync(_buffer, CancellationToken.None);
                    _totalBytes += result.Count;
                    messageState = _streamHandler.Write(result, _buffer.Array, _buffer.Offset, result.Count);
                } while (!_closed && messageState == WebSocketWriteResult.MessageContinues);
            }
            catch (Exception error)
            {
                _logger.Warn($"failed to receive message after {_totalBytes} bytes", error);
                throw;
            }

            return;
        }

        public async Task<bool> TryConnect(string serviceUri, int timeoutMs, bool logConnectionErrors = true)
        {
            _websocket = new ClientWebSocket();
            try
            {
                using var cts = new CancellationTokenSource(timeoutMs);
                _state = InternalState.Connecting;
                await _websocket.ConnectAsync(new Uri(serviceUri), cts.Token);
                _state = InternalState.Connected;
                _buffer = WebSocket.CreateClientBuffer(1024, 16);
                _logger.Info($"Connected to: {serviceUri}");
                _streamHandler.Connected();
                return true;
            }
            catch (Exception error)
            {
                _state = InternalState.Idle;
                _websocket = null;
                if (logConnectionErrors)
                {
                    _logger.Warn("Failed to connect", error);
                }
                return false;
            }
        }

        public void Dispose()
        {
            _closed = true;
            if (_websocket != null)
            {
                _websocket.Dispose();
                _websocket = null;
                _streamHandler.Closed();
            }

            _state = InternalState.Idle;
        }

        private enum InternalState
        {
            Idle,
            Connecting,
            Connected,
        }
    }
}