from fastapi import FastAPI, Depends, HTTPException, Query, Path
from typing import Annotated, Any
from contextlib import asynccontextmanager

from pydantic import BaseModel, constr
import hashlib
import secrets

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from .db import get_pool, close_pool, get_session
from .auth import get_user_by_token
from .models import User, Token, Order, Goods

pool = None  # для совместимости с lifespan


@asynccontextmanager
async def lifespan(app: FastAPI):
    global pool
    pool = await get_pool()
    yield
    pool = None
    await close_pool()


app = FastAPI(title="SQLi Lab (safe edition)", lifespan=lifespan)


# доп: защита от чрезмерно длинных входных данных
class AuthRequest(BaseModel):
    name: constr(min_length=1, max_length=100)
    password: constr(min_length=1, max_length=100)


@app.post("/auth/token")
async def auth_token(
    body: AuthRequest,
    session: AsyncSession = Depends(get_session),
):
    # стоит поставить bcrypt
    pass_hash = hashlib.md5(body.password.encode()).hexdigest()
    '''
    row = await conn.fetchrow(
        f"SELECT id, password_hash FROM users WHERE name = '{body.name}' AND password_hash = '{pass_hash}'"
    )
    '''
    stmt = select(User).where(
        User.name == body.name,
        User.password_hash == pass_hash,
    )
    result = await session.execute(stmt)
    user: User | None = result.scalar_one_or_none()

    if user is None:
        raise HTTPException(status_code=401, detail="Invalid credentials")

    '''    
    token_row = await conn.fetchrow(
        f"SELECT value FROM tokens WHERE user_id = {row['id']} AND is_valid = TRUE LIMIT 1"
    )
    '''
    token_stmt = (
        select(Token)
        .where(Token.user_id == user.id, Token.is_valid.is_(True))
        .limit(1)
    )
    token_result = await session.execute(token_stmt)
    existing_token: Token | None = token_result.scalar_one_or_none()

    if existing_token is None:
        token_value = secrets.token_urlsafe(64)
        new_token = Token(user_id=user.id, value=token_value, is_valid=True)
        session.add(new_token)
        await session.commit()
        token = token_value
    else:
        token = existing_token.value

    return {"token": token}


@app.get("/orders")
async def list_orders(
    user: Annotated[dict[str, Any], Depends(get_user_by_token)],
    session: AsyncSession = Depends(get_session),
    # доп: ограничиваем limit/offset
    limit: int = Query(10, ge=1, le=100),
    offset: int = Query(0, ge=0),
):
    '''
    rows = await conn.fetch(
      f"SELECT id, user_id, created_at FROM orders WHERE user_id = {user['id']} "
      f"ORDER BY created_at DESC LIMIT {limit} OFFSET {offset}"
    )
    '''

    stmt = (
        select(Order)
        .where(Order.user_id == user["id"])
        .order_by(Order.created_at.desc())
        .limit(limit)
        .offset(offset)
    )
    result = await session.execute(stmt)
    orders = result.scalars().all()

    return [
        {
            "id": o.id,
            "user_id": o.user_id,
            "created_at": o.created_at.isoformat(),
        }
        for o in orders
    ]


@app.get("/orders/{order_id}")
async def order_details(
    user: Annotated[dict[str, Any], Depends(get_user_by_token)],
    session: AsyncSession = Depends(get_session),
    # типизируем как int так как тест хочет 404/401/400/422, а 500 DataError его не устраивает
    order_id: int = Path(...),
):
    '''
    order = await conn.fetchrow(
      f"SELECT id, user_id, created_at FROM orders WHERE id = {order_id} AND user_id = {user['id']}"
    )
    '''

    order_stmt = select(Order).where(
        Order.id == order_id,
        Order.user_id == user["id"],
    )
    order_result = await session.execute(order_stmt)
    order: Order | None = order_result.scalar_one_or_none()

    if order is None:
        raise HTTPException(status_code=404, detail="Order not found")

    '''
    goods = await conn.fetch(
      f"SELECT id, name, count, price FROM goods WHERE order_id = {order_id}"
    )
    '''

    goods_stmt = select(Goods).where(Goods.order_id == order_id)
    goods_result = await session.execute(goods_stmt)
    goods_list = goods_result.scalars().all()

    return {
        "order": {
            "id": order.id,
            "user_id": order.user_id,
            "created_at": order.created_at.isoformat(),
        },
        "goods": [
            {
                "id": g.id,
                "name": g.name,
                "count": g.count,
                "price": float(g.price),
            }
            for g in goods_list
        ],
    }
