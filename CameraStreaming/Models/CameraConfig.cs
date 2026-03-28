using System.Text.Json.Serialization;

namespace CameraStreaming.Models
{
    public class CameraConfig
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("fps")]
        public int Fps { get; set; }

        [JsonPropertyName("backend")]
        public int Backend { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; } = "zh-CN";

        [JsonPropertyName("windowShape")]
        public string WindowShape { get; set; } = "circle";

        public CameraConfig()
        {
            Index = 0;
            Width = 640;
            Height = 480;
            Fps = 30;
            Backend = 0;
        }
    }
}
