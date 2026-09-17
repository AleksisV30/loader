from __future__ import annotations

import os
import logging
from pathlib import Path

from dotenv import load_dotenv
from fastapi import Depends, FastAPI, HTTPException, status
from fastapi.responses import FileResponse
from fastapi.security import HTTPBasic, HTTPBasicCredentials

from account_store import AccountStore

load_dotenv()
logging.basicConfig(level=logging.INFO)
app = FastAPI(title="Client Download API")
security = HTTPBasic()
store = AccountStore(os.getenv("DATABASE_PATH", "accounts.sqlite3"))
if store.database_url:
    logging.info("Using shared PostgreSQL account database")
else:
    logging.info("Using SQLite account database")


def require_account(credentials: HTTPBasicCredentials = Depends(security)) -> str:
    if not store.authenticate(credentials.username, credentials.password):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid username or password",
            headers={"WWW-Authenticate": "Basic"},
        )
    return credentials.username


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.get("/download/client")
def download_client(_: str = Depends(require_account)) -> FileResponse:
    download_path = Path(os.getenv("DOWNLOAD_FILE", "client/FlintfixClient.zip"))
    if not download_path.is_file():
        raise HTTPException(status_code=404, detail="Client package is not available")
    download_name = os.getenv("CLIENT_FILENAME", download_path.name)
    media_type = "application/octet-stream"
    if download_path.suffix.lower() == ".zip":
        media_type = "application/zip"
    elif download_path.suffix.lower() == ".exe":
        media_type = "application/vnd.microsoft.portable-executable"
    return FileResponse(download_path, filename=download_name, media_type=media_type)