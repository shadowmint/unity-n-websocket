using System.Net.WebSockets;
using System.Threading.Tasks;

namespace n_websockets.N.Package.Websockets
{
    public interface IWebSocketStreamHandler
    {
        /// <summary>
        /// Return false the current message is completed.
        /// This is invoked when resultCount bytes have been written into bufferArray at the given offset.
        /// </summary>
        WebSocketWriteResult Write(WebSocketReceiveResult result, byte[] bufferArray, int bufferOffset, int byteCount);

        void Connected();
        void Closed();
    }
}