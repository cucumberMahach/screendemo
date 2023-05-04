using System.Net;

namespace screendemo.http
{
    class HttpServer
    {
        private HttpListener? _listener;
        private int _requestCount = 0;
        private Task _listenTask;
        private bool _verbose = false;

        public HttpDispatcher Dispatcher { get; private set; }

        public HttpServer()
        {
            Dispatcher = new HttpDispatcher();
        }

        public void SetVerbose(bool verbose)
        {
            _verbose = verbose;
        }

        public void Start(string url)
        {
            if (_listener != null)
                return;

            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            _listenTask = HandleIncomingConnections();
        }

        public void Stop()
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener = null;
            }
        }

        private async Task HandleIncomingConnections()
        {
            if (_listener == null)
                return;

            bool runServer = true;
            while (runServer)
            {
                HttpListenerContext context = await _listener.GetContextAsync();

                if (_verbose)
                {
                    Console.WriteLine("Request #: {0}", ++_requestCount);
                    Console.WriteLine(context.Request.Url?.ToString());
                    Console.WriteLine(context.Request.HttpMethod);
                    Console.WriteLine();
                }

                Dispatcher.Dispatch(context);
            }
        }
    }
}
