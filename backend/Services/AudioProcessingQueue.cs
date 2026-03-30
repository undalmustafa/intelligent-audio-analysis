using System.Threading.Channels;
using AudioBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace AudioBackend.Services
{
    /// <summary>
    /// Thread-safe queue for audio processing jobs.
    /// Uses System.Threading.Channels for high-performance, bounded producer-consumer pattern.
    /// </summary>
    public class AudioProcessingQueue
    {
        private readonly Channel<Guid> _queue;

        public AudioProcessingQueue()
        {
            // Bounded channel to prevent memory issues if processing is slow
            _queue = Channel.CreateBounded<Guid>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        public async ValueTask EnqueueAsync(Guid audioRecordId)
        {
            await _queue.Writer.WriteAsync(audioRecordId);
        }

        public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Background hosted service that processes audio records from the queue.
    /// Runs in its own DI scope so DbContext is not disposed prematurely.
    /// </summary>
    public class AudioProcessingBackgroundService : BackgroundService
    {
        private readonly AudioProcessingQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AudioProcessingBackgroundService> _logger;

        public AudioProcessingBackgroundService(
            AudioProcessingQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<AudioProcessingBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Audio Processing Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Wait for a job from the queue
                    var audioId = await _queue.DequeueAsync(stoppingToken);

                    _logger.LogInformation("Dequeued audio for processing: {Id}", audioId);

                    // Create a new DI scope for each job so DbContext is fresh
                    using var scope = _scopeFactory.CreateScope();
                    var audioService = scope.ServiceProvider.GetRequiredService<IAudioService>();

                    var success = await audioService.TriggerProcessingAsync(audioId);

                    if (success)
                        _logger.LogInformation("Background processing completed for: {Id}", audioId);
                    else
                        _logger.LogWarning("Background processing failed for: {Id}", audioId);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in audio processing background service");
                    // Small delay to prevent tight error loops
                    await Task.Delay(1000, stoppingToken);
                }
            }

            _logger.LogInformation("Audio Processing Background Service stopped.");
        }
    }
}
