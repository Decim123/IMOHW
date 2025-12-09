from fastapi import HTTPException, Header, Depends
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from .db import get_session
from .models import User, Token


async def get_user_by_token(
    authorization: str | None = Header(None),
    session: AsyncSession = Depends(get_session),
):
    if not authorization:
        raise HTTPException(status_code=401, detail="Missing Authorization header")
    if not authorization.lower().startswith("bearer "):
        raise HTTPException(status_code=401, detail="Invalid Authorization header")

    token_value = authorization[7:]

    # доп проверка - ограничиваем длину токена
    if len(token_value) > 256:
        raise HTTPException(status_code=401, detail="Invalid token")

    stmt = (
        select(User.id, User.name)
        .join(Token, Token.user_id == User.id)
        .where(Token.value == token_value, Token.is_valid.is_(True))
    )
    result = await session.execute(stmt)
    row = result.first()

    if not row:
        raise HTTPException(status_code=401, detail="Invalid token")

    user_id, user_name = row
    return {"id": user_id, "name": user_name}
