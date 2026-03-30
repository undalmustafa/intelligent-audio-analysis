using System;

namespace AudioBackend.Models
{
    public class AudioRecord
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public DateTime RecordDate { get; set; }
        public int DurationInSeconds { get; set; }
        
        // Ham ses verisi PostgreSQL'de bytea olarak tutulacak
        public byte[] RawAudioData { get; set; } = Array.Empty<byte>();
        
        // Python işledikten sonra temizlenmiş halini buraya kaydedeceğiz
        public byte[]? FilteredAudioData { get; set; } 
        
        // İşlem durumunu takip etmek için (Pending, Processing, Completed, Failed)
        public string Status { get; set; } = "Pending";
    }
}