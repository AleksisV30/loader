from __future__ import annotations

import hashlib
import hmac
import secrets
import sqlite3
from contextlib import contextmanager
from dataclasses import dataclass
from pathlib import Path
from typing import Iterator


@dataclass(frozen=True)
class Account:
    username: str
    password: str


class AccountStore:
    def __init__(self, database_path: str) -> None:
        self.database_path = Path(database_path)
        self.database_path.parent.mkdir(parents=True, exist_ok=True)
        with self._connect() as connection:
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS accounts (
                    discord_user_id TEXT PRIMARY KEY,
                    username TEXT NOT NULL UNIQUE,
                    password_salt BLOB NOT NULL,
                    password_hash BLOB NOT NULL,
                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                )
                """
            )

    @contextmanager
    def _connect(self) -> Iterator[sqlite3.Connection]:
        connection = sqlite3.connect(self.database_path)
        connection.row_factory = sqlite3.Row
        try:
            yield connection
        except Exception:
            connection.rollback()
            raise
        else:
            connection.commit()
        finally:
            connection.close()

    def create_account(self, discord_user_id: int) -> Account | None:
        username = f"user_{secrets.token_urlsafe(6)}"
        password = secrets.token_urlsafe(18)
        salt = secrets.token_bytes(16)
        password_hash = self._hash_password(password, salt)

        try:
            with self._connect() as connection:
                connection.execute(
                    "INSERT INTO accounts (discord_user_id, username, password_salt, password_hash) VALUES (?, ?, ?, ?)",
                    (str(discord_user_id), username, salt, password_hash),
                )
        except sqlite3.IntegrityError:
            return None
        return Account(username=username, password=password)

    def authenticate(self, username: str, password: str) -> bool:
        with self._connect() as connection:
            row = connection.execute(
                "SELECT password_salt, password_hash FROM accounts WHERE username = ?",
                (username,),
            ).fetchone()
        if row is None:
            return False
        candidate = self._hash_password(password, row["password_salt"])
        return hmac.compare_digest(candidate, row["password_hash"])

    def remove_account(self, discord_user_id: int) -> bool:
        with self._connect() as connection:
            result = connection.execute(
                "DELETE FROM accounts WHERE discord_user_id = ?",
                (str(discord_user_id),),
            )
        return result.rowcount == 1

    @staticmethod
    def _hash_password(password: str, salt: bytes) -> bytes:
        return hashlib.pbkdf2_hmac("sha256", password.encode(), salt, 300_000)