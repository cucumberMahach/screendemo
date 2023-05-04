using System.Net;

namespace screendemo.http
{
    class HttpDispatcher
    {
        private List<HttpHandler> _handlers = new List<HttpHandler>();

        public void Dispatch(HttpListenerContext context)
        {
            foreach (var handler in _handlers)
            {
                if (handler.IsHandleThat(context.Request))
                {
                    handler.HandleRequest(context);
                    break;
                }
            }
        }

        public void AddHandler(HttpHandler handler)
        {
            _handlers.Add(handler);
        }

        public void RemoveHandler(HttpHandler handler)
        {
            _handlers.Remove(handler);
        }
    }
}
