import os
from dotenv import load_dotenv
from sqlalchemy.ext.asyncio import (
    create_async_engine,
    async_sessionmaker,
    AsyncSession,
)
from typing import AsyncGenerator

load_dotenv()

ASYNC_DATABASE_URL = os.getenv("DATABASE_URL")

_engine = None
_SessionFactory: async_sessionmaker[AsyncSession] | None = None


async def get_session() -> AsyncGenerator[AsyncSession, None]:
    session_factory = get_session_factory()
    async with session_factory() as session:
        yield session


async def get_pool():
    
    # старое имя но теперь инициализирует engine и session factory
    
    global _engine, _SessionFactory
    if _engine is None:
        _engine = create_async_engine(ASYNC_DATABASE_URL, echo=False)
        _SessionFactory = async_sessionmaker(_engine, expire_on_commit=False)
    return _engine


def get_session_factory() -> async_sessionmaker[AsyncSession]:
    global _SessionFactory
    if _SessionFactory is None:
        raise RuntimeError("Session factory is not initialized. Call get_pool() first.")
    return _SessionFactory


async def close_pool():
    global _engine, _SessionFactory
    if _engine is not None:
        await _engine.dispose()
        _engine = None
        _SessionFactory = None
