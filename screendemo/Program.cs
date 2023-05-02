using screendemo;
using System;
using System.Drawing.Imaging;
using System.Net;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Diagnostics;

namespace screendemo
{
    public class Laputa : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {

        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Use tx, txlocal or rx in args");
                return;
            }

            if (args[0] == "tx")
            {
                Console.WriteLine("tx");

                var wssv = new WebSocketServer("ws://127.0.0.1:8001/");
                wssv.AddWebSocketService<Laputa>("/stream");
                wssv.Start();

                ImageCodecInfo codecInfo = GetEncoder(ImageFormat.Jpeg);
                EncoderParameters codecParameters = new EncoderParameters(1);
                EncoderParameter codecParameter = new EncoderParameter(Encoder.Quality, 10L);
                codecParameters.Param[0] = codecParameter;

                var screenStateLogger = new ScreenStateLogger(codecInfo, codecParameters);
                screenStateLogger.ScreenRefreshed += (sender, data) =>
                {
                    wssv.WebSocketServices.Broadcast(data);
                };
                screenStateLogger.Start();
            }
            else if (args[0] == "rx")
            {
                Console.WriteLine("rx");
                Console.WriteLine("Enter link:");
                string link = Console.ReadLine();
                if (link == null)
                    return;

                HttpServer server = new HttpServer();
                server.Start("http://localhost:8000/");

                var ws = new WebSocket(string.Format("ws://{0}/stream", link));

                ws.OnMessage += (sender, message) =>
                {
                    server.PutImage(message.RawData);
                };

                ws.Connect();


                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:8000/",
                    UseShellExecute = true
                });
            }
            else if (args[0] == "txlocal")
            {
                Console.WriteLine("txlocal");

                HttpServer server = new HttpServer();
                server.Start("http://localhost:8000/");
                
                //ImageCodecInfo codecInfo = GetEncoder(ImageFormat.Jpeg);
                ImageCodecInfo codecInfo = GetEncoder(ImageFormat.Png);
                EncoderParameters codecParameters = new EncoderParameters(1);
                //EncoderParameter codecParameter = new EncoderParameter(Encoder.Quality, 50L);
                EncoderParameter codecParameter = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
                codecParameters.Param[0] = codecParameter;

                var screenStateLogger = new ScreenStateLogger(codecInfo, codecParameters);
                screenStateLogger.ScreenRefreshed += (sender, data) =>
                {
                    server.PutImage(data);
                };
                screenStateLogger.Start();

                server.ParamsChanged += (sender, param) =>
                {
                    Params p = (Params)param;
                    if (p.format == "png")
                    {
                        ImageCodecInfo codecInfo = GetEncoder(ImageFormat.Png);
                        EncoderParameters codecParameters = new EncoderParameters(1);
                        EncoderParameter codecParameter = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
                        codecParameters.Param[0] = codecParameter;
                        screenStateLogger.SetImageCodec(codecInfo, codecParameters);
                    }
                    else if (p.format == "jpg")
                    {
                        ImageCodecInfo codecInfo = GetEncoder(ImageFormat.Jpeg);
                        EncoderParameters codecParameters = new EncoderParameters(1);
                        EncoderParameter codecParameter = new EncoderParameter(Encoder.Quality, (long)p.quality);
                        codecParameters.Param[0] = codecParameter;
                        screenStateLogger.SetImageCodec(codecInfo, codecParameters);
                    }
                };

            }
            Console.ReadKey(true);
        }

        static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}



