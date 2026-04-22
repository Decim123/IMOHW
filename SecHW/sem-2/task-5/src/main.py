from __future__ import annotations

from typing import Dict

from fastapi import FastAPI

from src.schemas import UserCreate


app = FastAPI(title="Залача 5")


@app.post("/registration")
async def registration(user: UserCreate) -> Dict[str, str]:
    return {"msg": "Пользователь создан", "user": user.username}
