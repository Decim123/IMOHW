from __future__ import annotations

import re

from pydantic import BaseModel, EmailStr, Field, field_validator, model_validator


SPECIAL_CHAR_PATTERN = r"[!@#$%^&*]"


class UserCreate(BaseModel):
    username: str = Field(
        ...,
        min_length=4,
        max_length=20,
        pattern=r"^[A-Za-z0-9]+$",
    )
    email: EmailStr
    password: str
    confirm_password: str
    age: int = Field(..., ge=18, le=100)

    @field_validator("password")
    @classmethod
    def validate_password(cls, value: str) -> str:
        if value == "123":
            raise ValueError("[Пароль] Не может быть '123'")
        if not re.search(r"[A-Z]", value):
            raise ValueError("[Пароль] Хотяб 1 символ верхнего регистра")
        if not re.search(r"\d", value):
            raise ValueError("[Пароль] Хотяб 1 число")
        if not re.search(SPECIAL_CHAR_PATTERN, value):
            raise ValueError(
                "[Пароль] Хотяб 1 спец символ (!@#$%^&*)"
            )
        return value

    @model_validator(mode="after")
    def passwords_match(self) -> UserCreate:
        if self.password != self.confirm_password:
            raise ValueError("[Пароль] Не совпадает пароль")
        return self
