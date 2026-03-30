using Microsoft.AspNetCore.Http;

namespace AudioBackend.DTOs
{
    /// <summary>
    /// Request DTO for audio file upload.
    /// Accepts the audio file and associated metadata.
    /// </summary>
    public class AudioUploadDto
    {
        public IFormFile File { get; set; } = null!;
        public string DeviceId { get; set; } = "web-browser";
        public int DurationInSeconds { get; set; }
    }
}
