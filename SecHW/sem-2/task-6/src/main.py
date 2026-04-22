from __future__ import annotations

import re
from urllib.parse import parse_qs
from typing import Dict, List

import bleach
from fastapi import FastAPI, Request
from fastapi.responses import RedirectResponse, Response
from fastapi.templating import Jinja2Templates
from markupsafe import Markup

from src.schemas import UserCreate


app = FastAPI(title="Задача 6")
templates = Jinja2Templates(directory="templates")
COMMENTS: List[str] = []
ALLOWED_TAGS = ["b", "i", "u", "em", "strong"]
CSP_POLICY = "default-src 'self'; script-src 'self'; style-src 'self'"


def sanitize_comment(text: str) -> str:
    without_scripts = re.sub(
        r"<\s*(script|style)\b[^>]*>.*?<\s*/\s*\1\s*>",
        "",
        text,
        flags=re.IGNORECASE | re.DOTALL,
    )
    return bleach.clean(
        without_scripts,
        tags=ALLOWED_TAGS,
        attributes={},
        strip=True,
    ).strip()


@app.middleware("http")
async def add_csp_header(request: Request, call_next) -> Response:
    response = await call_next(request)
    response.headers["Content-Security-Policy"] = CSP_POLICY
    return response


@app.get("/")
async def home() -> RedirectResponse:
    return RedirectResponse(url="/comments", status_code=303)


@app.post("/registration")
async def registration(user: UserCreate) -> Dict[str, str]:
    return {"msg": "Пользователь создан", "user": user.username}


@app.get("/comments")
async def comments_page(request: Request):
    safe_comments = [Markup(comment) for comment in COMMENTS]
    return templates.TemplateResponse(
        request=request,
        name="comments.html",
        context={"comments": safe_comments},
    )


@app.post("/comments")
async def create_comment(request: Request) -> RedirectResponse:
    raw_body = (await request.body()).decode("utf-8", "replace")
    text = parse_qs(raw_body).get("text", [""])[0]
    cleaned = sanitize_comment(text)
    if cleaned:
        COMMENTS.append(cleaned)
    return RedirectResponse(url="/comments", status_code=303)
