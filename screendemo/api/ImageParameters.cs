using System.Drawing.Imaging;
using System.Text.Json.Serialization;

namespace screendemo.api
{
    class ImageParameters
    {
        [JsonPropertyName("format")] public string Format { get; set; }
        [JsonPropertyName("quality")] public int Quality { get; set; }

        public ImageParameters()
        {
            Format = "png";
            Quality = 100;
        }

        public string GetImageContentType()
        {
            switch (Format)
            {
                case "png":
                    return "image/png";
                case "jpeg":
                    return "image/jpeg";
            }
            return "";
        }

        public ImageCodecInfo GetImageCodecInfo()
        {
            ImageCodecInfo codecInfo = null;

            if (Format == "png")
            {
                codecInfo = GetEncoder(ImageFormat.Png);
            } 
            else if (Format == "jpeg")
            {
                codecInfo = GetEncoder(ImageFormat.Jpeg);
            }

            return codecInfo;
        }

        public EncoderParameters GetImageEncoderParameters()
        {
            EncoderParameters codecParameters = new EncoderParameters(1);

            if (Format == "png")
            {
                EncoderParameter codecParameter = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
                codecParameters.Param[0] = codecParameter;
            }
            else if (Format == "jpeg")
            {
                EncoderParameter codecParameter = new EncoderParameter(Encoder.Quality, (long)Quality);
                codecParameters.Param[0] = codecParameter;
            }

            return codecParameters;
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
