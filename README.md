# 🎙️ Intelligent Audio Analysis & Noise Reduction

A full-stack study case project for capturing, storing, and processing audio with intelligent noise reduction.

## 🏗️ Architecture

```
┌──────────────┐       ┌───────────────────┐       ┌──────────────────┐
│   Frontend   │──────▶│  .NET 8 Web API   │──────▶│   PostgreSQL     │
│  (HTML/JS)   │ REST  │   (AudioBackend)  │  EF   │   (AudioDb)      │
└──────────────┘       └───────┬───────────┘       └──────────────────┘
                               │
                               │ REST
                               ▼
                       ┌───────────────────┐
                       │  Python Service   │
                       │ (Noise Reduction) │
                       └───────────────────┘
```

## 🛠️ Tech Stack

| Layer       | Technology                     |
|-------------|--------------------------------|
| Frontend    | HTML5, CSS3, Vanilla JavaScript|
| Backend     | .NET 8 Web API, Entity Framework Core |
| Database    | PostgreSQL 15                  |
| Processing  | Python 3.x (Flask, scipy, noisereduce) |
| Edge Mock   | ESP32 (PlatformIO/Wokwi)      |
| CI/CD       | GitHub Actions                 |

## 📋 Development Roadmap

- [x] Step 1: Project setup & folder structure
- [ ] Step 2: Backend API endpoints (upload, list, stream)
- [ ] Step 3: Python noise reduction implementation
- [ ] Step 4: ESP32 edge device firmware
- [ ] Step 5: Frontend dashboard
- [ ] Step 6: Background processing pipeline
- [ ] Step 7: CI/CD with GitHub Actions

## 📄 License

This project is a study case for educational purposes.
