using AudioBackend.Data;
using AudioBackend.DTOs;
using AudioBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AudioBackend.Services
{
    /// <summary>
    /// Business logic for audio record operations.
    /// Handles upload, retrieval, and processing trigger.
    /// </summary>
    public class AudioService : IAudioService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AudioService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AudioService(
            AppDbContext context,
            ILogger<AudioService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Uploads audio file and stores it in PostgreSQL as bytea.
        /// </summary>
        public async Task<AudioRecordDto> UploadAsync(AudioUploadDto uploadDto)
        {
            using var memoryStream = new MemoryStream();
            await uploadDto.File.CopyToAsync(memoryStream);

            var record = new AudioRecord
            {
                Id = Guid.NewGuid(),
                DeviceId = uploadDto.DeviceId,
                RecordDate = DateTime.UtcNow,
                DurationInSeconds = uploadDto.DurationInSeconds,
                RawAudioData = memoryStream.ToArray(),
                Status = "Pending"
            };

            _context.AudioRecords.Add(record);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audio uploaded: {Id}, Size: {Size} bytes", record.Id, record.RawAudioData.Length);

            return MapToDto(record);
        }

        /// <summary>
        /// Returns all audio records as DTOs (without binary data).
        /// </summary>
        public async Task<List<AudioRecordDto>> GetAllRecordsAsync()
        {
            var records = await _context.AudioRecords
                .OrderByDescending(r => r.RecordDate)
                .Select(r => new AudioRecordDto
                {
                    Id = r.Id,
                    DeviceId = r.DeviceId,
                    RecordDate = r.RecordDate,
                    DurationInSeconds = r.DurationInSeconds,
                    Status = r.Status,
                    HasProcessedAudio = r.FilteredAudioData != null
                })
                .ToListAsync();

            return records;
        }

        /// <summary>
        /// Gets a single audio record by ID (including binary data).
        /// </summary>
        public async Task<AudioRecord?> GetByIdAsync(Guid id)
        {
            return await _context.AudioRecords.FindAsync(id);
        }

        /// <summary>
        /// Triggers Python service to process the audio.
        /// Sends raw audio to Python, receives processed audio back.
        /// </summary>
        public async Task<bool> TriggerProcessingAsync(Guid id)
        {
            var record = await _context.AudioRecords.FindAsync(id);
            if (record == null) return false;

            try
            {
                record.Status = "Processing";
                await _context.SaveChangesAsync();

                // Send to Python service
                var client = _httpClientFactory.CreateClient();
                var content = new MultipartFormDataContent();
                content.Add(new ByteArrayContent(record.RawAudioData), "file", "audio.wav");

                var pythonUrl = Environment.GetEnvironmentVariable("PYTHON_SERVICE_URL") 
                    ?? "http://localhost:5002";

                var response = await client.PostAsync($"{pythonUrl}/process", content);

                if (response.IsSuccessStatusCode)
                {
                    var processedBytes = await response.Content.ReadAsByteArrayAsync();
                    record.FilteredAudioData = processedBytes;
                    record.Status = "Completed";
                    _logger.LogInformation("Audio processed successfully: {Id}", id);
                }
                else
                {
                    record.Status = "Failed";
                    _logger.LogWarning("Python service returned {StatusCode} for {Id}", response.StatusCode, id);
                }

                await _context.SaveChangesAsync();
                return record.Status == "Completed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio {Id}", id);
                record.Status = "Failed";
                await _context.SaveChangesAsync();
                return false;
            }
        }

        /// <summary>
        /// Maps an AudioRecord entity to a lightweight DTO.
        /// </summary>
        private static AudioRecordDto MapToDto(AudioRecord record)
        {
            return new AudioRecordDto
            {
                Id = record.Id,
                DeviceId = record.DeviceId,
                RecordDate = record.RecordDate,
                DurationInSeconds = record.DurationInSeconds,
                Status = record.Status,
                HasProcessedAudio = record.FilteredAudioData != null
            };
        }
    }
}
