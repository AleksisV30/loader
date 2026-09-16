# Discord account and client download service

This project creates one account per Discord user and protects a normal client package download with those credentials. It does not include a client payload or executable; place your reviewed, disclosed C# client package at the path configured by `DOWNLOAD_FILE`.

## Setup

1. Create a Discord application and bot, then copy `.env.example` to `.env`.
2. Set `DISCORD_TOKEN` in `.env` and invite the bot with the `bot` and `applications.commands` scopes.
3. Install dependencies with `python -m pip install -r requirements.txt`.
4. Put the approved client archive at `client/FlintfixClient.zip`.

To package this Windows downloader itself for testing:

```powershell
dotnet publish client/FlintfixClient.csproj -c Release -o client/publish
Compress-Archive -Path client/publish/* -DestinationPath client/FlintfixClient.zip -Force
```

Run the bot:

```powershell
python bot.py
```

Run the download API in another terminal:

```powershell
python -m uvicorn api:app --host 127.0.0.1 --port 8000
```

The C# client can request `GET /download/client` using HTTP Basic Auth with the username and password received by the Discord user. After a successful download, it opens the saved ZIP in Windows Explorer for review; it does not execute it. Use HTTPS and a reverse proxy before exposing the API outside localhost.

To change the file served, set `DOWNLOAD_FILE` in `.env` to a `.zip` or `.exe` path. Set `CLIENT_FILENAME` to the name users receive, then restart the API. The client asks for confirmation before opening a downloaded executable.

## Build the C# download client

Install the .NET 8 SDK, then start the API and build the client:

```powershell
python -m uvicorn api:app --host 127.0.0.1 --port 8000
dotnet build client/FlintfixClient.csproj
dotnet run --project client/FlintfixClient.csproj
```

The client prompts for the credentials sent by Discord, saves the package in your Windows `Downloads` folder, and asks for confirmation before opening the single executable inside it.

The Windows client opens a login window with a masked password field. The downloaded file is saved to your Windows `Downloads` folder. The client can be published as one self-contained Windows executable with `dotnet publish client/FlintfixClient.csproj -c Release`.

## Deploy on an Oracle Cloud VM

The repository includes `Dockerfile`, `docker-compose.yml`, and `Caddyfile` for a Linux VM. On the VM:

1. Install Docker and Docker Compose, then copy this project to the VM.
2. Point your domain's DNS `A` record, such as `api.example.com`, to the VM's public IP.
3. Replace `api.example.com` in `Caddyfile` with your real domain.
4. Copy `.env.production.example` to `.env.production` and set the rotated Discord token.
5. Start the services with `docker compose up -d --build`.
6. Check them with `docker compose ps` and `curl https://api.example.com/health`.

Before publishing the user-facing EXE, set `FLINTFIX_API_URL` in the build environment to your HTTPS endpoint, for example `https://api.example.com/download/client`. The client defaults to localhost for local testing.

## Deploy the API on Render

This repository includes `render.yaml` for the API web service.

1. Push the project to a private GitHub repository. Do not commit `.env`, `.env.production`, or any bot token.
2. In Render, choose **New > Blueprint**, connect the repository, and apply `render.yaml`.
3. Render will provide a URL such as `https://flintfix-api.onrender.com`.
4. Confirm `https://YOUR-SERVICE.onrender.com/health` returns `{"status":"ok"}`.
5. The API download endpoint is `https://YOUR-SERVICE.onrender.com/download/client`.

The Render free web service can sleep after inactivity, and its local filesystem is ephemeral. SQLite accounts can therefore be lost after a redeploy or restart. Use a persistent database before treating this as production. Run the Discord bot as a separate always-on worker or VM, not inside the web service.

After Render gives you the final URL, replace the localhost URL in `client/Program.cs` with `https://YOUR-SERVICE.onrender.com/download/client`, then publish the Windows client again.