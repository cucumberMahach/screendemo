using screendemo.api;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace screendemo
{
    class HttpServer
    {
        private HttpListener listener;
        private int requestCount = 0;
        private byte[] image;
        private ImageParameters param;

        public EventHandler<ImageParameters> ImageParametersChanged;

        public HttpServer()
        {
            image = File.ReadAllBytes("plug.jpg");
            param = new ImageParameters();
            param.format = "png";
            param.quality = 100;
        }

        public ImageParameters GetImageParameters()
        {
            return param;
        }

        public void Start(string url)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            Task listenTask = HandleIncomingConnections();
            //listener.Close();
        }

        public void Stop()
        {
            listener.Stop();
        }

        public void PutImage(byte[] data)
        {
            image = data;
        }

        public byte[] GetImage()
        {
            return image;
        }

        public async Task HandleIncomingConnections()
        {
            bool runServer = true;

            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/api/setImageParameters")
                {
                    var stream = req.InputStream;
                    string[] str;
                    using (var reader = new StreamReader(stream))
                    {
                        str = reader.ReadToEnd().Split('&');
                    }

                    ImageParameters p = new ImageParameters();

                    foreach (string pair in str)
                    {
                        string[] splitted = pair.Split('=');
                        string key = splitted[0];
                        string value = splitted[1];
                        switch (key)
                        {
                            case "format":
                                p.format = value;
                                break;
                            case "quality":
                                p.quality = int.Parse(value);
                                break;
                        }
                    }

                    param = p;

                    ImageParametersChanged?.Invoke(this, p);

                    resp.Redirect("/");
                    resp.Close();
                }
                else if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/img")
                {
                    byte[] data = GetImage();
                    resp.ContentType = "image/png";
                    resp.ContentEncoding = null;
                    resp.ContentLength64 = data.LongLength;

                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
                else if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/api/imageParameters")
                {
                    string json = JsonSerializer.Serialize<ImageParameters>(GetImageParameters());
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    resp.ContentType = "application/json";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
                else
                {
                    string pageData = File.ReadAllText("server.html");
                    byte[] data = Encoding.UTF8.GetBytes(pageData);
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
            }
        }
    }
}
