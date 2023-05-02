using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace screendemo
{
    class Params
    {
        [JsonPropertyName("format")] public string format { get; set; }
        [JsonPropertyName ("quality")] public int quality { get; set; }
    }
    class HttpServer
    {
        private HttpListener listener;
        private int requestCount = 0;
        private byte[] image;
        private object locker = new object();
        private Params param;

        public EventHandler<Params> ParamsChanged;

        public HttpServer()
        {
            image = File.ReadAllBytes("plug.jpg");
            param = new Params();
            param.format = "png";
            param.quality = 100;
        }

        public Params GetParams()
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
            //listenTask.GetAwaiter().GetResult();

            //listener.Close();
        }

        public void Stop()
        {
            listener.Stop();
        }

        public void PutImage(byte[] data)
        {
            lock (locker)
            {
                image = data;
            }
        }

        public byte[] GetImage()
        {
            lock(locker)
            {
                return image.ToArray();
            }
        }

        public async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/api/setParams")
                {
                    var stream = req.InputStream;
                    string[] str;
                    using (var reader = new StreamReader(stream))
                    {
                        str = reader.ReadToEnd().Split('&');
                    }

                    Params p = new Params();

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

                    ParamsChanged?.Invoke(this, p);

                    resp.Redirect("/");
                    resp.Close();
                }
                else if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/img")
                {
                    // Write the response info
                    byte[] data = GetImage();
                    resp.ContentType = "image/png";
                    resp.ContentEncoding = null;
                    resp.ContentLength64 = data.LongLength;

                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
                else if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/api/params")
                {
                    string json = JsonSerializer.Serialize<Params>(GetParams());
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    resp.ContentType = "application/json";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
                else
                {
                    // Write the response info
                    string pageData = File.ReadAllText("server.html");
                    byte[] data = Encoding.UTF8.GetBytes(pageData);
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    // Write out to the response stream (asynchronously), then close it
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    resp.Close();
                }
            }
        }
    }
}
