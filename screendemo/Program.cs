using screendemo.api;
using screendemo.http;
using screendemo.http.handlers;
using screendemo.screen;
using System.Text.Json;

namespace screendemo
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> argsList = new List<string>(args);

            if (argsList.Contains("--help"))
            {
                Console.WriteLine("Screen sharing over http");
                Console.WriteLine();
                Console.WriteLine("Arguments:");
                Console.WriteLine("\t--help - show help");
                Console.WriteLine("\t--url \"http://localhost:8000/\" - use that url");
                Console.WriteLine("\t--verbose - show verbose messages");
                return;
            }

            int urlArgIndex = argsList.IndexOf("--url");
            string url = "http://localhost:8000/";

            if (urlArgIndex != -1)
            {
                if (urlArgIndex >= argsList.Count)
                {
                    Console.WriteLine("Url is not specified");
                    return;
                }
                url = argsList[urlArgIndex+1];
            }

            var adapters = ScreenStateLogger.GetAvaliableAdapters();
            Console.WriteLine("Select screen:");
            for (int i = 0; i < adapters.Count; i++)
            {
                Console.WriteLine(string.Format("{0}. {1}", i + 1, adapters[i].ToUserString()));
            }
            int num = Convert.ToInt32(Console.ReadLine());
            if (num-1 < 0 || num-1 >= adapters.Count)
            {
                Console.WriteLine("Incorrect screen number");
                return;
            }
            Console.WriteLine();
            
            ScreenDemoAdapter adapter = adapters[num - 1];

            ImageParameters imageParameters = new ImageParameters();

            HttpServer server = new HttpServer();
            server.SetVerbose(argsList.Contains("--verbose"));
            var screenStateLogger = new ScreenStateLogger(adapter, imageParameters.GetImageCodecInfo(), imageParameters.GetImageEncoderParameters());

            GetHtmlHandler getHtmlHandler = new GetHtmlHandler("/", "resources/server.html");
            ImageGetHandler imageGetHandler = new ImageGetHandler();
            GetJsonHandler getImageParametersHandler = new GetJsonHandler("/api/imageParameters");
            SetImageParametersHandler setImageParametersHandler = new SetImageParametersHandler();

            getImageParametersHandler.PutJson(JsonSerializer.Serialize(imageParameters));

            setImageParametersHandler.ImageParametersChanged += (sender, param) =>
            {
                imageParameters = param;
                getImageParametersHandler.PutJson(JsonSerializer.Serialize(imageParameters));

                screenStateLogger.SetImageCodec(imageParameters.GetImageCodecInfo(), imageParameters.GetImageEncoderParameters());
            };
            
            screenStateLogger.ScreenRefreshed += (sender, data) =>
            {
                imageGetHandler.PutImage(data, imageParameters.GetImageContentType());
            };

            server.Dispatcher.AddHandler(getHtmlHandler);
            server.Dispatcher.AddHandler(imageGetHandler);
            server.Dispatcher.AddHandler(getImageParametersHandler);
            server.Dispatcher.AddHandler(setImageParametersHandler);

            server.Start(url);
            screenStateLogger.Start();

            Console.ReadKey(true);
        }

        
    }
}



