from __future__ import annotations

from typing import Literal

from pydantic import BaseModel


class User(BaseModel):
    id: int
    username: str
    role: Literal["user", "admin"]


class StoredFile(BaseModel):
    id: int
    owner_id: int
    original_name: str
    path: str
    size_bytes: int
    content_type: str
