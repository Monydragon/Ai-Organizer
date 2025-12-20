# Ollama (Docker) setup for AI Desktop Organizer

This app expects Ollama at `http://localhost:11434` (default).

## Prereqs
- Docker Desktop (Windows)
- Docker engine running

## Start Ollama
From the project folder:

```powershell
cd "C:\Projects\Github\Avalonia\Ai-Organizer\Ai Organizer"
docker compose -f .\docker-compose.ollama.yml up -d
```

## Pull a model
Ollama only lists models that have already been pulled.

```powershell
docker exec -it ai-organizer-ollama ollama pull llama3.2
```

## Verify Ollama responds
```powershell
curl http://localhost:11434/api/tags
```

If it returns JSON with a `models` array, the app should show them after pressing **Refresh** in the Models tab.

## Troubleshooting
- If `/api/tags` returns empty `models`, pull at least one model as shown above.
- If connection fails:
  - ensure Docker Desktop is running
  - ensure port `11434` isn’t blocked
  - check container logs:
    ```powershell
    docker logs ai-organizer-ollama --tail 200
    ```

