using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using App.Websockets;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace n_websockets.N.Package.Websockets.Utility
{
    public abstract class BinaryMessageStream<T> : IWebSocketStreamHandler where T : struct
    {
        private readonly int _bytesPerMessage;

        private readonly byte[] _buffer;

        private int _processedBytes;

        private int _ingestedBytes;

        private int _processedMessages;

        private int _bufferOffset;

        private ConcurrentQueue<T> _pendingMessages;

        public int PendingCount => _pendingMessages?.Count ?? 0;

        public BinaryMessageStream()
        {
            _bytesPerMessage = UnsafeUtility.SizeOf<T>();
            _buffer = new byte[_bytesPerMessage];
            _bufferOffset = 0;
        }

        protected abstract T AsStruct(byte[] bytes);

        protected abstract void OnConnected();

        protected abstract void OnClosed();
        
        public WebSocketWriteResult Write(WebSocketReceiveResult result, byte[] bufferArray, int bufferOffset, int byteCount)
        {
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                _ingestedBytes += byteCount;
                for (var i = 0; i < byteCount; i++)
                {
                    _buffer[_bufferOffset] = bufferArray[bufferOffset + i];
                    _bufferOffset += 1;
                    if (_bufferOffset != _bytesPerMessage) continue;
                    _pendingMessages.Enqueue(AsStruct(_buffer));
                    _processedMessages += 1;
                    _processedBytes += _bytesPerMessage;
                    _bufferOffset = 0;
                }

                return _bufferOffset == 0
                    ? WebSocketWriteResult.MessageCompleted
                    : WebSocketWriteResult.MessageContinues;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                return result.EndOfMessage
                    ? WebSocketWriteResult.MessageCompleted
                    : WebSocketWriteResult.MessageContinues;
            }

            return WebSocketWriteResult.Closed;
        }

        public void Connected()
        {
            _processedBytes = 0;
            _ingestedBytes = 0;
            _processedMessages = 0;
            _pendingMessages = new ConcurrentQueue<T>();
            OnConnected();
        }

        public void Closed()
        {
            OnClosed();
        }

        public void LogState()
        {
            Debug.Log(
                $"{nameof(NetworkGeometryReader)}: in: {_ingestedBytes}, out: {_processedBytes} ({_processedMessages} messages, {PendingCount} remaining)");
        }

        public bool IsEmpty()
        {
            return _pendingMessages?.IsEmpty ?? true;
        }

        public IEnumerable<T> GetBuffer(int max)
        {
            var count = 0;
            if (_pendingMessages == null)
            {
                yield break;
            }

            while (_pendingMessages.TryDequeue(out var buffer))
            {
                yield return buffer;
                count += 1;
                if (count >= max)
                {
                    break;
                }
            }
        }
    }
}