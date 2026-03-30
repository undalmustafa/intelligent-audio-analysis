# Intelligent Audio Analysis & Noise Reduction

A full-stack IoT study case project for capturing, storing, and processing audio with intelligent noise reduction. Audio is captured by an ESP32 microcontroller, sent to a .NET 8 backend, stored in PostgreSQL, processed by a Python noise reduction service, and compared via a web dashboard.

## Architecture

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

┌──────────────┐
│  ESP32 MCU   │──── HTTP POST ────▶ .NET Backend
│ (PlatformIO) │     (WAV audio)
└──────────────┘
```

## Project Structure

```
intelligent-audio-analysis/
├── frontend/               # HTML + Vanilla JS web dashboard
│   ├── index.html
│   ├── style.css
│   └── app.js
├── backend/                # .NET 8 Web API
│   ├── Controllers/        # REST API endpoints
│   ├── Services/           # Business logic + background processing
│   ├── Models/             # EF Core entities
│   ├── DTOs/               # Data transfer objects
│   ├── Data/               # Database context
│   ├── Migrations/         # EF Core migrations
│   └── Program.cs          # App configuration & DI
├── python-service/         # Python noise reduction service
│   ├── main.py             # Flask REST API
│   ├── processing.py       # Audio processing pipeline
│   └── requirements.txt
├── edge/                   # ESP32 IoT device (PlatformIO)
│   ├── src/main.cpp        # Firmware: audio capture + HTTP upload
│   └── tools/              # Audio sample header generator
├── .github/workflows/      # CI/CD pipelines
│   ├── backend-ci.yml      # .NET build & validation
│   └── python-ci.yml       # Python lint & validation
├── docker-compose.yaml     # PostgreSQL container
└── README.md
```

## Tech Stack

| Layer       | Technology                     |
|-------------|--------------------------------|
| Frontend    | HTML5, CSS3, Vanilla JavaScript|
| Backend     | .NET 8 Web API, Entity Framework Core |
| Database    | PostgreSQL 15 (Docker)         |
| Processing  | Python 3.x (Flask, scipy, noisereduce) |
| Edge Device | ESP32 (PlatformIO/Wokwi)      |
| CI/CD       | GitHub Actions                 |

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Python 3.10+](https://www.python.org/downloads/)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/)

### 1. Clone Repository

```bash
git clone https://github.com/undalmustafa/intelligent-audio-analysis.git
cd intelligent-audio-analysis
```

### 2. Start the Database
```bash
docker-compose up -d
```

### 2. Run the Backend
```bash
cd backend
dotnet run
```

### 3. Run the Python Service
```bash
cd python-service
python -m venv venv
source venv/bin/activate   # Windows: venv\Scripts\activate
pip install -r requirements.txt
python main.py
```

### 4. Open Frontend
Open `frontend/index.html` in your browser, or use a local dev server.

### 5. Running PlatformIO
```bash
cd edge
pio run
pio run --target upload
```

## Simulation with Wokwi

This project supports ESP32 simulation with Wokwi.

Running simulation:
1. Open VS Code
2. Open edge/ folder
3. Install Wokwi for VS Code extension
4. Ctrl + Shift + P
5. Wokwi: Start Simulator


## ⚙️ How It Works

1. **ESP32** captures audio and sends raw WAV data via HTTP POST to the .NET backend
2. **.NET Backend** stores the raw audio in PostgreSQL and enqueues it for processing
3. **Background Service** picks up the job, sends audio to the Python service
4. **Python Service** applies bandpass filtering + spectral gating noise reduction
5. **Backend** stores the processed audio back in PostgreSQL
6. **Frontend** displays records with status tracking and audio comparison (raw vs processed)

## Development Roadmap

- [x] Project setup & folder structure
- [x] PostgreSQL database with Docker Compose
- [x] .NET 8 backend with EF Core (models, DTOs, migrations)
- [x] REST API endpoints (upload, list, stream, process)
- [x] Python noise reduction service (bandpass filter + spectral gating)
- [x] ESP32 edge device firmware (audio capture + HTTP upload)
- [x] Frontend dashboard with status monitoring
- [x] Background processing queue for reliable audio processing
- [x] CI/CD with GitHub Actions

