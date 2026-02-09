# OneManVanFSM Docker Deployment

## Prerequisites

Create a `.env` file in the project root (see `.env.example` for a template).

## Build and Run

```bash
# On the Ubuntu server (192.168.100.107), clone or copy the project then:

# Build and start
docker compose up -d --build

# View logs
docker compose logs -f web

# Stop
docker compose down
```

The app will be available at `http://192.168.100.107:5002` (or whatever `WEBUI_PORT` is set to in your `.env` file).

## Database

SQLite database is persisted at `/media/AppDatabases/OneManVanFSMData/OneManVanFSM.db` on the host (mapped to `/app/data/` inside the container).
