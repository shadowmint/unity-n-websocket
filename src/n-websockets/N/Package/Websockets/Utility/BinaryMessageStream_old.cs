using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using App.Websockets;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace n_websockets.N.Package.Websockets.Utility
{
    public abstract class BinaryMessageStreamOld<T> : IWebSocketStreamHandler where T : struct
    {
        private readonly int _bytesPerMessage;

        private readonly byte[] _partial;

        private readonly byte[] _buffer;

        private int _partialSize;

        private int _processedBytes;

        private int _ingestedBytes;
        
        private int _processedMessages;
        
        private ConcurrentQueue<T> _pendingMessages;

        public int PendingCount => _pendingMessages?.Count ?? 0;
        
        public BinaryMessageStreamOld()
        {
            _bytesPerMessage = UnsafeUtility.SizeOf<T>();
            _partial = new byte[_bytesPerMessage];
            _buffer = new byte[_bytesPerMessage];

        }

        protected abstract T AsStruct(byte[] bytes);

        public WebSocketWriteResult Write(WebSocketReceiveResult result, byte[] bufferArray, int bufferOffset, int byteCount)
        {
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                Debug.Log($"{byteCount} -> {_ingestedBytes}");
                _ingestedBytes += byteCount;

                // If we don't have enough bytes yet, save this partial
                var totalBytes = _partialSize + byteCount;
                if (totalBytes < _bytesPerMessage)
                {
                    Array.Copy(_partial, _partialSize, bufferArray, 0, byteCount);
                    _partialSize += byteCount;
                    return WebSocketWriteResult.MessageContinues;
                }

                // Calculate the leftover bytes from the incoming
                // Example:
                //
                // - 32 bytes per message, 8 in partial, incoming 70 bytes
                // - totalBytes = 8 + 70 = 78
                // - totalMessages = 78 / 32 = 2
                // - bufferSize = 2 * 32 = 64
                // - remainingPartial = 78 - 64 = 14
                // - consumedBytes = 70 - 14 = 56
                //
                // ie. The 70 incoming bytes has two full records in it with 6 left over, but because
                //     we have a partial buffer of size 8, we effectively have a buffer of size 78, with
                //     14 left over.
                var totalMessages = totalBytes / _bytesPerMessage;
                var bufferSize = totalMessages * _bytesPerMessage;
                var remainingPartial = totalBytes - bufferSize;
                var consumedBytes = byteCount - remainingPartial;

                // Collect partial and new data
                var offset = 0;
                for (var i = 0; i < totalMessages; i++)
                {
                    if (_partialSize > 0)
                    {
                        Array.Copy(_partial, 0, _buffer, 0, _partialSize);
                        Array.Copy(bufferArray, bufferOffset, _buffer, _partialSize, _bytesPerMessage - _partialSize);
                        _partialSize = 0;
                        offset = _bytesPerMessage;
                    }
                    else
                    {
                        Array.Copy(bufferArray, bufferOffset + offset, _buffer, 0, _bytesPerMessage);
                        offset += _bytesPerMessage;
                    }

                    _pendingMessages.Enqueue(AsStruct(_buffer));
                    _processedMessages += 1;
                    _processedBytes += _bytesPerMessage;
                }

                Debug.Log($"-> {_processedMessages}");

                // If there were any leftovers, rebuild the partial
                _partialSize = remainingPartial;
                if (remainingPartial > 0)
                {
                    Array.Copy(bufferArray, consumedBytes, _partial, 0, remainingPartial);
                }

                return WebSocketWriteResult.MessageCompleted;
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
            _partialSize = 0;
            _processedBytes = 0;
            _ingestedBytes = 0;
            _processedMessages = 0;
            _pendingMessages = new ConcurrentQueue<T>();
        }

        public void Closed()
        {
        }

        public void LogState()
        {
            Debug.Log($"{nameof(NetworkGeometryReader)}: in: {_ingestedBytes}, out: {_processedBytes} ({_processedMessages} messages)");
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
            while (_pendingMessages.TryDequeue(out var buffer) && count < max)
            {
                yield return buffer;
                count += 1;
            }
        }
    }
}