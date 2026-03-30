using AudioBackend.DTOs;
using AudioBackend.Models;

namespace AudioBackend.Services
{
    /// <summary>
    /// Service interface for audio record operations.
    /// Follows Interface Segregation for clean architecture.
    /// </summary>
    public interface IAudioService
    {
        Task<AudioRecordDto> UploadAsync(AudioUploadDto uploadDto);
        Task<List<AudioRecordDto>> GetAllRecordsAsync();
        Task<AudioRecord?> GetByIdAsync(Guid id);
        Task<bool> TriggerProcessingAsync(Guid id);
    }
}
