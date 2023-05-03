using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace screendemo.api
{
    class ImageParameters
    {
        [JsonPropertyName("format")] public string format { get; set; }
        [JsonPropertyName("quality")] public int quality { get; set; }
    }
}
