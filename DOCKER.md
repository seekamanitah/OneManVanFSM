# OneManVanFSM Docker Deployment

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

The app will be available at `http://192.168.100.107:8080`

## Database

SQLite database is persisted in the `app-data` Docker volume at `/app/data/OneManVanFSM.db`.
