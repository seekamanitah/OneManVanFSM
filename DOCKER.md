# OneManVanFSM Docker Deployment

## Prerequisites

Create a `.env` file in the project root. Use one of the presets:

```bash
# For PRODUCTION server:
cp .env.production .env

# For TEST/DEV server:
cp .env.development .env
```

Or copy `.env.example` and fill in your own values.

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

## Environments

| Setting | Production | Test/Dev |
|---|---|---|
| Port | `5002` | `6000` |
| Container | `onemanvanfsm-web` | `onemanvanfsm-web-test` |
| Database | `OneManVanFSM.db` | `OneManVanFSM_Test.db` |
| Data Volume | `OneManVanFSMData` | `OneManVanFSMData_Test` |
| Branch | `master` | `develop` |

Both containers can run **side-by-side** on the same server since they use different ports, container names, databases, and volumes.

## Git Branching

- **`master`** — Production-ready code. Deploy from here.
- **`develop`** — Active development and testing. All daily work goes here.
- Merge `develop ? master` only when a milestone passes testing.

```bash
# Deploy production from master:
git checkout master && docker compose up -d --build

# Deploy test from develop:
git checkout develop && docker compose up -d --build
```

## Database

SQLite database is persisted at the `DATA_VOLUME` path on the host (mapped to `/app/data/` inside the container).
- Production: `/media/AppDatabases/OneManVanFSMData/OneManVanFSM.db`
- Test: `/media/AppDatabases/OneManVanFSMData_Test/OneManVanFSM_Test.db`
