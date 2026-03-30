"""
Gerçekçi bir test WAV dosyası oluşturur ve C header dosyasına dönüştürür.
ESP32 firmware'ine gömülmek üzere kullanılır.

Kullanım: python generate_audio_header.py
Çıktı:   ../src/audio_sample.h
"""

import numpy as np
import struct
import os

# ==============================
# Ayarlar
# ==============================
SAMPLE_RATE = 16000
DURATION = 1.5      # saniye
OUTPUT_HEADER = os.path.join(os.path.dirname(__file__), '..', 'src', 'audio_sample.h')

def generate_realistic_audio(sample_rate, duration):
    """
    Gerçekçi ses verisi oluşturur:
    - İnsan sesi frekansları (100-300 Hz temel + harmonikler)
    - Arka plan gürültüsü
    - Zaman içinde değişen genlik (konuşma simülasyonu)
    """
    t = np.linspace(0, duration, int(sample_rate * duration), endpoint=False)
    
    # Temel frekans (insan sesi ~150 Hz)
    fundamental = np.sin(2 * np.pi * 150 * t)
    
    # Harmonikler (ses rengini oluşturur)
    harmonic2 = 0.5 * np.sin(2 * np.pi * 300 * t)
    harmonic3 = 0.25 * np.sin(2 * np.pi * 450 * t)
    harmonic4 = 0.15 * np.sin(2 * np.pi * 600 * t)
    harmonic5 = 0.1 * np.sin(2 * np.pi * 900 * t)
    
    # Konuşma sinyali
    speech = fundamental + harmonic2 + harmonic3 + harmonic4 + harmonic5
    
    # Genlik zarfı — konuşma gibi açılıp kapanan ses
    envelope = np.ones_like(t)
    # İlk 0.1 saniye sessizlik (fade in)
    fade_in = int(0.1 * sample_rate)
    envelope[:fade_in] = np.linspace(0, 1, fade_in)
    # 0.3-0.5 saniye arası sessizlik (konuşma arası)
    pause_start = int(0.3 * sample_rate)
    pause_end = int(0.5 * sample_rate)
    envelope[pause_start:pause_end] = np.linspace(1, 0.1, pause_end - pause_start)
    # 0.5-0.6 tekrar başla
    resume_start = int(0.5 * sample_rate)
    resume_end = int(0.6 * sample_rate)
    envelope[resume_start:resume_end] = np.linspace(0.1, 1, resume_end - resume_start)
    # Son 0.1 saniye fade out
    fade_out = int(0.1 * sample_rate)
    envelope[-fade_out:] = np.linspace(1, 0, fade_out)
    
    speech *= envelope
    
    # Arka plan gürültüsü (gerçek ortam simülasyonu)
    noise = np.random.normal(0, 0.15, len(t))
    
    # Düşük frekans gürültü (AC hum — 50Hz)
    ac_hum = 0.08 * np.sin(2 * np.pi * 50 * t)
    
    # Yüksek frekans gürültü (tiz hiss)
    high_noise = 0.05 * np.random.normal(0, 1, len(t))
    
    # Tümünü birleştir
    audio = speech * 0.6 + noise + ac_hum + high_noise
    
    # Normalize [-1, 1]
    audio = audio / np.max(np.abs(audio)) * 0.9
    
    # 16-bit PCM'e dönüştür
    audio_int16 = np.int16(audio * 32767)
    
    return audio_int16

def create_wav_bytes(audio_data, sample_rate):
    """WAV formatında byte dizisi oluşturur."""
    num_samples = len(audio_data)
    data_size = num_samples * 2  # 16-bit = 2 bytes
    file_size = data_size + 36
    
    header = struct.pack('<4sI4s', b'RIFF', file_size, b'WAVE')
    fmt = struct.pack('<4sIHHIIHH', b'fmt ', 16, 1, 1, sample_rate,
                      sample_rate * 2, 2, 16)
    data_header = struct.pack('<4sI', b'data', data_size)
    
    audio_bytes = audio_data.tobytes()
    
    return header + fmt + data_header + audio_bytes

def bytes_to_c_header(wav_bytes, var_name='AUDIO_SAMPLE'):
    """Byte dizisini C header dosyasına dönüştürür."""
    lines = []
    lines.append('// =======================================')
    lines.append('// Otomatik oluşturuldu — generate_audio_header.py')
    lines.append('// Gerçekçi ses verisi (konuşma + gürültü)')
    lines.append(f'// Sample Rate: {SAMPLE_RATE} Hz, Duration: {DURATION}s')
    lines.append('// =======================================')
    lines.append(f'#ifndef {var_name}_H')
    lines.append(f'#define {var_name}_H')
    lines.append('')
    lines.append('#include <stdint.h>')
    lines.append('')
    lines.append(f'const int {var_name}_SIZE = {len(wav_bytes)};')
    lines.append(f'const int {var_name}_SAMPLE_RATE = {SAMPLE_RATE};')
    lines.append(f'const float {var_name}_DURATION = {DURATION}f;')
    lines.append('')
    lines.append(f'const uint8_t {var_name}_DATA[] PROGMEM = {{')
    
    # 16 byte per line
    for i in range(0, len(wav_bytes), 16):
        chunk = wav_bytes[i:i+16]
        hex_values = ', '.join(f'0x{b:02X}' for b in chunk)
        if i + 16 < len(wav_bytes):
            lines.append(f'    {hex_values},')
        else:
            lines.append(f'    {hex_values}')
    
    lines.append('};')
    lines.append('')
    lines.append(f'#endif // {var_name}_H')
    lines.append('')
    
    return '\n'.join(lines)

# ==============================
# Ana çalıştırma
# ==============================
if __name__ == '__main__':
    print(f"Gerçekçi ses verisi oluşturuluyor...")
    print(f"  Sample Rate: {SAMPLE_RATE} Hz")
    print(f"  Duration: {DURATION} s")
    
    audio = generate_realistic_audio(SAMPLE_RATE, DURATION)
    wav_bytes = create_wav_bytes(audio, SAMPLE_RATE)
    
    print(f"  WAV boyutu: {len(wav_bytes)} bytes")
    print(f"  Örneklem sayısı: {len(audio)}")
    
    header_content = bytes_to_c_header(wav_bytes)
    
    os.makedirs(os.path.dirname(OUTPUT_HEADER), exist_ok=True)
    with open(OUTPUT_HEADER, 'w') as f:
        f.write(header_content)
    
    print(f"\n✅ Header dosyası oluşturuldu: {OUTPUT_HEADER}")
    print(f"   Dosya boyutu: {os.path.getsize(OUTPUT_HEADER)} bytes")
