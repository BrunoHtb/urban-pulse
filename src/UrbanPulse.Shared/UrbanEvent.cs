namespace UrbanPulse.Shared
{
    public class UrbanEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = "Traffic";
        public string Description { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Severity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public GeoLocation Location => new GeoLocation(Latitude, Longitude);
    }

    public record GeoLocation(double Lat, double Lon);
}
