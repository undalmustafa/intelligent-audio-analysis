using AudioBackend.DTOs;
using AudioBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AudioBackend.Controllers
{
    /// <summary>
    /// API Controller for audio recording operations.
    /// Handles upload, listing, streaming, and processing trigger.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AudioController : ControllerBase
    {
        private readonly IAudioService _audioService;
        private readonly AudioProcessingQueue _processingQueue;
        private readonly ILogger<AudioController> _logger;

        public AudioController(
            IAudioService audioService,
            AudioProcessingQueue processingQueue,
            ILogger<AudioController> logger)
        {
            _audioService = audioService;
            _processingQueue = processingQueue;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/audio/upload
        /// Upload a new audio recording with metadata.
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)] // 50 MB max
        public async Task<ActionResult<AudioRecordDto>> Upload([FromForm] AudioUploadDto uploadDto)
        {
            if (uploadDto.File == null || uploadDto.File.Length == 0)
            {
                return BadRequest(new { error = "No audio file provided." });
            }

            _logger.LogInformation("Upload request received: {FileName}, {Size} bytes",
                uploadDto.File.FileName, uploadDto.File.Length);

            var result = await _audioService.UploadAsync(uploadDto);

            // Enqueue for background processing (runs in its own DI scope)
            _logger.LogInformation("Enqueuing audio for background processing: {Id}", result.Id);
            await _processingQueue.EnqueueAsync(result.Id);

            return CreatedAtAction(nameof(GetRecords), new { id = result.Id }, result);
        }

        /// <summary>
        /// GET /api/audio/records
        /// Get all audio records (metadata only, no binary data).
        /// </summary>
        [HttpGet("records")]
        public async Task<ActionResult<List<AudioRecordDto>>> GetRecords()
        {
            var records = await _audioService.GetAllRecordsAsync();
            return Ok(records);
        }

        /// <summary>
        /// GET /api/audio/{id}/raw
        /// Stream the raw (unprocessed) audio file.
        /// </summary>
        [HttpGet("{id}/raw")]
        public async Task<IActionResult> GetRawAudio(Guid id)
        {
            var record = await _audioService.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound(new { error = "Audio record not found." });
            }

            return File(record.RawAudioData, "audio/wav", $"raw_{record.Id}.wav");
        }

        /// <summary>
        /// GET /api/audio/{id}/processed
        /// Stream the processed (noise-reduced) audio file.
        /// </summary>
        [HttpGet("{id}/processed")]
        public async Task<IActionResult> GetProcessedAudio(Guid id)
        {
            var record = await _audioService.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound(new { error = "Audio record not found." });
            }

            if (record.FilteredAudioData == null || record.FilteredAudioData.Length == 0)
            {
                return NotFound(new { error = "Processed audio not yet available." });
            }

            return File(record.FilteredAudioData, "audio/wav", $"processed_{record.Id}.wav");
        }

        /// <summary>
        /// POST /api/audio/{id}/process
        /// Manually trigger noise reduction processing via the Python service.
        /// Used for retrying failed or stuck records.
        /// </summary>
        [HttpPost("{id}/process")]
        public async Task<IActionResult> TriggerProcessing(Guid id)
        {
            var record = await _audioService.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound(new { error = "Audio record not found." });
            }

            _logger.LogInformation("Manual processing triggered for audio: {Id}, current status: {Status}", id, record.Status);

            // Enqueue for background processing
            await _processingQueue.EnqueueAsync(id);

            return Ok(new { message = "Audio queued for processing.", id });
        }
    }
}
