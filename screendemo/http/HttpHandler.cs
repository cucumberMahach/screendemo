using System.Net;

namespace screendemo.http
{
    abstract class HttpHandler
    {
        public abstract bool IsHandleThat(HttpListenerRequest request);
        public abstract void HandleRequest(HttpListenerContext context);
    }
}
