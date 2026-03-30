#include <Arduino.h>
#include <WiFi.h>
#include <HTTPClient.h>
#include "audio_sample.h"  // Gömülü gerçekçi ses verisi

// ==============================
// Configuration
// ==============================
const char* ssid = "Wokwi-GUEST";
const char* password = "";

// Backend API URL
// host.wokwi.internal → Wokwi simülatöründen localhost'a erişim sağlar
// Gerçek donanımda çalıştırırken IP adresini güncelleyin:
// const char* BACKEND_URL = "http://<BACKEND_IP>:5076/api/audio/upload";
const char* BACKEND_URL = "http://host.wokwi.internal:5076/api/audio/upload";
const char* DEVICE_ID = "esp32-wokwi";

const int CHUNK_SIZE = 1024;  // 1 KB chunk boyutu

// ==============================
// Gömülü WAV Dosyasını Chunk Chunk Okuyup Gönder
// ==============================
void sendAudioChunked() {
    if (WiFi.status() != WL_CONNECTED) {
        Serial.println("HATA: WiFi bağlı değil!");
        return;
    }

    int fileSize = AUDIO_SAMPLE_SIZE;

    Serial.println("\n===== Kayıt Gönderimi Başlatılıyor =====");
    Serial.printf("[SES] Gömülü gerçek ses verisi: %d bytes\n", fileSize);
    Serial.printf("[SES] Sample Rate: %d Hz, Süre: %.1f sn\n",
                  AUDIO_SAMPLE_SAMPLE_RATE, AUDIO_SAMPLE_DURATION);
    Serial.printf("[AYAR] Chunk boyutu: %d bytes\n", CHUNK_SIZE);

    // --- Chunk chunk okuma simülasyonu ---
    int totalChunks = (fileSize + CHUNK_SIZE - 1) / CHUNK_SIZE;
    int totalBytesRead = 0;
    int chunkIndex = 0;

    Serial.println("[STREAM] Ses verisi chunk chunk okunuyor...");

    while (totalBytesRead < fileSize) {
        int bytesToRead = min(CHUNK_SIZE, fileSize - totalBytesRead);
        chunkIndex++;
        totalBytesRead += bytesToRead;

        // Her 5 chunk'ta bir logla (çok fazla log olmasın)
        if (chunkIndex % 5 == 1 || totalBytesRead == fileSize) {
            Serial.printf("  [CHUNK %02d/%02d] %4d bytes okundu  |  Toplam: %5d / %5d bytes\n",
                          chunkIndex, totalChunks, bytesToRead, totalBytesRead, fileSize);
        }
        delay(20);  // Gerçekçi okuma gecikmesi
    }

    Serial.printf("[STREAM] Okuma tamamlandı: %d chunk, %d bytes\n", chunkIndex, totalBytesRead);

    // --- Multipart form-data oluştur ---
    String boundary = "----ESP32Boundary" + String(millis());

    String partHeader = "--" + boundary + "\r\n";
    partHeader += "Content-Disposition: form-data; name=\"file\"; filename=\"esp32_recording.wav\"\r\n";
    partHeader += "Content-Type: audio/wav\r\n\r\n";

    // Süreyi integer'a çevir
    int durationInt = (int)AUDIO_SAMPLE_DURATION;

    String partFooter = "\r\n--" + boundary + "\r\n";
    partFooter += "Content-Disposition: form-data; name=\"deviceId\"\r\n\r\n";
    partFooter += String(DEVICE_ID);
    partFooter += "\r\n--" + boundary + "\r\n";
    partFooter += "Content-Disposition: form-data; name=\"durationInSeconds\"\r\n\r\n";
    partFooter += String(durationInt);
    partFooter += "\r\n--" + boundary + "--\r\n";

    int payloadSize = partHeader.length() + fileSize + partFooter.length();

    uint8_t* payload = (uint8_t*)malloc(payloadSize);
    if (payload == NULL) {
        Serial.println("HATA: Payload için bellek yetersiz!");
        return;
    }

    // Payload birleştir — PROGMEM'den oku
    int offset = 0;
    memcpy(payload + offset, partHeader.c_str(), partHeader.length());
    offset += partHeader.length();

    // PROGMEM'den ses verisini kopyala
    memcpy_P(payload + offset, AUDIO_SAMPLE_DATA, fileSize);
    offset += fileSize;

    memcpy(payload + offset, partFooter.c_str(), partFooter.length());

    // --- HTTP POST gönder ---
    HTTPClient http;
    http.begin(BACKEND_URL);
    http.addHeader("Content-Type", "multipart/form-data; boundary=" + boundary);

    Serial.printf("[HTTP] >>> %d bytes gönderiliyor -> %s\n", payloadSize, BACKEND_URL);
    int httpCode = http.sendRequest("POST", payload, payloadSize);

    if (httpCode > 0) {
        Serial.printf("[HTTP] BAŞARILI! Yanıt Kodu: %d\n", httpCode);
        if (httpCode == 200 || httpCode == 201) {
            String response = http.getString();
            if (response.length() > 200) {
                response = response.substring(0, 200) + "...";
            }
            Serial.printf("[HTTP] Yanıt: %s\n", response.c_str());
        }
    } else {
        Serial.printf("[HTTP] HATA: %s\n", http.errorToString(httpCode).c_str());
    }

    free(payload);
    http.end();
    Serial.println("===== Gönderim Tamamlandı =====\n");
}

// ==============================
// Setup
// ==============================
void setup() {
    Serial.begin(115200);
    Serial.println("\n=== ESP32 Ses Kaydı Mock - Başlatılıyor ===");
    Serial.printf("Cihaz ID: %s\n", DEVICE_ID);
    Serial.printf("Gömülü ses: %d bytes, %.1f sn, %d Hz\n",
                  AUDIO_SAMPLE_SIZE, AUDIO_SAMPLE_DURATION, AUDIO_SAMPLE_SAMPLE_RATE);

    // WiFi bağlantısı
    WiFi.begin(ssid, password);
    Serial.print("[WiFi] Bağlanılıyor");
    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print(".");
    }
    Serial.printf("\n[WiFi] Bağlandı! IP: %s\n", WiFi.localIP().toString().c_str());
    Serial.println("=== Başlatma Tamamlandı ===\n");
}

// ==============================
// Main Loop — her 15 saniyede bir gönder
// ==============================
void loop() {
    sendAudioChunked();
    delay(15000);
}