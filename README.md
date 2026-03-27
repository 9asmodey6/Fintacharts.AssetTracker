# Asset Tracker API (Fintacharts Integration)

A high-performance market data tracking service built with **.NET 8**. 
The application synchronizes financial instruments from the Fintacharts platform, tracks real-time price updates via WebSockets, and provides historical data through REST.


🚀 Project Guide: [Explore Project Documentation](https://www.mintlify.com/9asmodey6/Fintacharts.AssetTracker/introduction)


> [!IMPORTANT]  
> **Automated Zero-Config Startup**
> No manual intervention required. Unlike traditional integrations, the _PriceUpdateWorker_ automatically performs a Self-Seeding process on startup:
- Fetches all available instruments from the Fintacharts REST API.
- Performs a high-speed UPSERT into the local PostgreSQL database.
- Immediately establishes WebSocket subscriptions for all synchronized assets.

---
## 🌟 Key Architectural Features

### 1. Vertical Slice Architecture (VSA)
Designed with maintainability in mind. Instead of traditional layers, the project is organized into self-contained slices: `GetAssets`, `GetPrices`, and `GetPriceHistory`. This reduces cognitive load and coupling.

### 2. Reactive Event-Driven Engine
Implemented a custom **InMemory Event Bus** to orchestrate background tasks:
- **Event Flow:** `GetAssets` (Feature) -> `InstrumentsSyncedEvent` -> `PriceUpdateWorker` (Background Service).
- **Reactive Reconnection:** The worker uses a `CancellationTokenSource` session management. When new assets are synced, it gracefully cancels the existing WebSocket session and immediately establishes a new one with updated subscriptions—**without restarting the application**.

### 3. High-Performance Price Persistence (Write-Behind Pattern)
To handle high-frequency market ticks without overwhelming the database, the system implements a dual-layer update strategy:
- **L1 (Instant Cache)**: Updates a thread-safe singleton PriceCache immediately upon receiving a tick. This ensures the API always returns sub-millisecond fresh data.
- **L2 (Batched Database)**: Instead of saving every tick, the system buffers updates in memory and performs a Bulk Flush every 5 seconds.
This reduces DB IOPS by up to 90% while maintaining a reliable historical record.

### 4. Robust Real-time Connectivity
- **Graceful Session Management:** Leverages `LinkedTokens` to coordinate application shutdown and reactive reconnection logic.
- **Automated Auth:** Built-in `FintachartsTokenManager` with thread-safe token retrieval and automatic background refresh.
---
## Architecture Overview

             ┌──────────────┐
             │   Swagger    │
             │   Client     │
             └──────┬───────┘
                    │
                    ▼
           GET /api/assets
                    │
                    ▼
           GetAssets Feature
                    │
                    ▼
         InstrumentsSyncedEvent
                    │
                    ▼
           PriceUpdateWorker
                    │
            WebSocket (Fintacharts)
                    │
                    ▼
           Real-time price updates
              │             │
              ▼             ▼
         PriceCache     PostgreSQL
---
## 🛠 Tech Stack

- .NET 8 / C# 12
- ASP.NET Minimal API
- PostgreSQL 16
- Entity Framework Core
- FluentValidation
- Docker / Docker Compose
- Swagger (OpenAPI 3)
---
## Logging

- **Information level (Default):** Shows core system events (startup, instrument sync, socket status).
- **Debug level:** Detailed price updates (ticks) are logged at the Debug level to avoid console noise. 

To see real-time ticks in the console, change the logging level in `appsettings.json`:
```bash
"Logging": {
    "LogLevel": {
        "Default": "Debug"
    }
}
```
---
## 🚀 Getting Started

### 1. Prerequisites
- Docker & Docker Compose installed.

### 2. Clone the repository
```bash
git clone https://github.com/9asmodey6/Fintacharts.AssetTracker.git
cd Fintacharts.AssetTracker
```

### 3. Configure Environment Variables
The application requires a `.env` file to handle secrets and connection strings. 

1. **Rename** the template file:
   - Linux/macOS: `cp .env.example .env`
   - Windows (PowerShell): `cp .env.example .env`

2. **Open** the newly created `.env` file and replace the placeholders with your actual Fintacharts credentials:
   - `FINTA_USER=your_login_here`
   - `FINTA_PASS=your_password_here`

> [!WARNING]
> Never commit the actual `.env` file to the repository. It is already included in `.gitignore`.

### 4. Launching with Docker
```bash
docker-compose up --build
```
### 5. Usage flow
1. Open Swagger UI: **http://localhost:8080/swagger**
2. Execute GET `/api/assets` to fetch instruments and start the tracking engine.
3. Use GET `/api/prices` to see live updates.
4. Watch the application logs for live WebSocket Ticks.

*Note: This project was developed as part of a technical assessment for a .NET Backend Developer position.*
