using System.Text.Json.Serialization;

namespace UrbanPulse.Producer.Models;

public class TomTomFlowRoot
{
    [JsonPropertyName("flowSegmentData")]
    public FlowSegmentData FlowData { get; set; } = null!;
}

public class FlowSegmentData
{
    [JsonPropertyName("currentSpeed")]
    public int CurrentSpeed { get; set; }

    [JsonPropertyName("freeFlowSpeed")]
    public int FreeFlowSpeed { get; set; }

    [JsonPropertyName("currentTravelTime")]
    public int CurrentTravelTime { get; set; }

    [JsonPropertyName("coordinates")]
    public FlowCoordinates Coordinates { get; set; } = null!;
}

public class FlowCoordinates
{
    [JsonPropertyName("coordinate")]
    public List<TomTomPointV5> Points { get; set; } = null!;
}

public class TomTomPointV5
{
    [JsonPropertyName("latitude")]
    public double Lat { get; set; }

    [JsonPropertyName("longitude")]
    public double Lon { get; set; }
}