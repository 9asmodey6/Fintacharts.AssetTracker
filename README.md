# Asset Tracker API (Fintacharts Integration)

A high-performance market data tracking service built with **.NET 8**. 
The application synchronizes financial instruments from the Fintacharts platform, tracks real-time price updates via WebSockets, and provides historical data through REST.

[Explore Project Documentation](https://www.mintlify.com/9asmodey6/Fintacharts.AssetTracker/introduction)

> [!IMPORTANT]  
> **Initialization Step:** After launching the application, you **must** call the `GET /api/assets` endpoint first. 
> This seeds the local database with instruments and triggers the internal `EventBus`, which notifies the Background Service to start WebSocket subscriptions for the synchronized assets.
---
## 🌟 Key Architectural Features

### 1. Vertical Slice Architecture (VSA)
Designed with maintainability in mind. Instead of traditional layers, the project is organized into self-contained slices: `GetAssets`, `GetPrices`, and `GetPriceHistory`. This reduces cognitive load and coupling.

### 2. Reactive Event-Driven Engine
Implemented a custom **InMemory Event Bus** to orchestrate background tasks:
- **Event Flow:** `GetAssets` (Feature) -> `InstrumentsSyncedEvent` -> `PriceUpdateWorker` (Background Service).
- **Reactive Reconnection:** The worker uses a `CancellationTokenSource` session management. When new assets are synced, it gracefully cancels the existing WebSocket session and immediately establishes a new one with updated subscriptions—**without restarting the application**.

### 3. High-Performance Caching
To ensure sub-millisecond response times:
- **L1 (In-Memory):** Thread-safe `ConcurrentDictionary` for O(1) access to the latest market ticks.
- **L2 (Persistent):** PostgreSQL with optimized `UPSERT` logic (`ON CONFLICT DO UPDATE`) to handle high-frequency price updates without DB bloat.

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
