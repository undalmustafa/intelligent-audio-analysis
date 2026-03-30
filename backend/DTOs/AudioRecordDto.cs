namespace AudioBackend.DTOs
{
    /// <summary>
    /// Response DTO for audio record metadata.
    /// Excludes binary audio data for lightweight API responses.
    /// </summary>
    public class AudioRecordDto
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public DateTime RecordDate { get; set; }
        public int DurationInSeconds { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool HasProcessedAudio { get; set; }
    }
}
