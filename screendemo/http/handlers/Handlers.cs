using screendemo.api;
using System.Net;
using System.Text;

namespace screendemo.http.handlers
{
    class SetImageParametersHandler : HttpHandler
    {
        public override void HandleRequest(HttpListenerContext context)
        {
            var argsParser = new HttpPostArgsParser(context.Request.InputStream);

            ImageParameters paramets = new ImageParameters();

            paramets.Format = argsParser.Arguments.GetValueOrDefault("format", "png");
            paramets.Quality = int.Parse(argsParser.Arguments.GetValueOrDefault("quality", "100"));

            ImageParametersChanged?.Invoke(this, paramets);

            context.Response.Redirect("/");
            context.Response.Close();
        }

        public override bool IsHandleThat(HttpListenerRequest request)
        {
            return request.HttpMethod == "POST" && request.Url != null && request.Url.AbsolutePath == "/api/setImageParameters";
        }

        public EventHandler<ImageParameters> ImageParametersChanged;
    }

    class ImageGetHandler : HttpHandler
    {
        private byte[] _image;
        private string _contentType;
        public ImageGetHandler()
        {
            _image = File.ReadAllBytes("resources/plug.jpg");
            _contentType = "image/jpeg";
        }
        public void PutImage(byte[] data, string contentType)
        {
            _image = data;
            _contentType = contentType;
        }
        public override void HandleRequest(HttpListenerContext context)
        {
            byte[] data = _image;
            context.Response.ContentType = _contentType;
            context.Response.ContentEncoding = null;
            context.Response.ContentLength64 = data.LongLength;

            context.Response.OutputStream.WriteAsync(data);
            context.Response.Close();
        }

        public override bool IsHandleThat(HttpListenerRequest request)
        {
            return request.HttpMethod == "GET" && request.Url != null && request.Url.AbsolutePath == "/api/img";
        }
    }

    class GetJsonHandler : HttpHandler
    {
        private readonly string _absolutePath;
        private string _json = "{}";
        public GetJsonHandler(string absolutePath)
        {
            _absolutePath = absolutePath;
        }

        public void PutJson(string json)
        {
            _json = json;
        }

        public override void HandleRequest(HttpListenerContext context)
        {
            byte[] data = Encoding.UTF8.GetBytes(_json);
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = data.LongLength;
            context.Response.OutputStream.WriteAsync(data);
            context.Response.Close();
        }

        public override bool IsHandleThat(HttpListenerRequest request)
        {
            return request.HttpMethod == "GET" && request.Url != null && request.Url.AbsolutePath == _absolutePath;
        }
    }

    class GetHtmlHandler : HttpHandler
    {
        private readonly string _absolutePath;
        private readonly string _htmlPath;

        public GetHtmlHandler(string absolutePath, string htmlPath)
        {
            _absolutePath = absolutePath;
            _htmlPath = htmlPath;
        }

        public override void HandleRequest(HttpListenerContext context)
        {
            string pageData = File.ReadAllText(_htmlPath);
            byte[] data = Encoding.UTF8.GetBytes(pageData);
            context.Response.ContentType = "text/html";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = data.LongLength;

            context.Response.OutputStream.WriteAsync(data);
            context.Response.Close();
        }

        public override bool IsHandleThat(HttpListenerRequest request)
        {
            return request.HttpMethod == "GET" && request.Url != null && request.Url.AbsolutePath == _absolutePath;
        }
    }
}
