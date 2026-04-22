from __future__ import annotations

from copy import deepcopy
from typing import Dict, List, Optional
from urllib.parse import parse_qs, urlencode

from fastapi import Depends, FastAPI, Header, HTTPException, Query, Request, status
from fastapi.responses import RedirectResponse
from fastapi.templating import Jinja2Templates

from src.schemas import StoredFile, User


app = FastAPI(title="Задача 8")
templates = Jinja2Templates(directory="templates")

users_db: List[User] = [
    User(id=1, username="Алиса", role="user"),
    User(id=2, username="Боб", role="user"),
    User(id=3, username="Администратор", role="admin"),
]

INITIAL_FILES_DB: List[StoredFile] = [
    StoredFile(id=1, name="otchet_alisy.pdf", size_kb=128, owner_id=1),
    StoredFile(id=2, name="otchet_boba.xlsx", size_kb=256, owner_id=2),
    StoredFile(id=3, name="plan_admina.docx", size_kb=64, owner_id=3),
]

files_db: List[StoredFile] = deepcopy(INITIAL_FILES_DB)


def reset_files_db() -> None:
    global files_db
    files_db = deepcopy(INITIAL_FILES_DB)


def get_user_by_id(user_id: int) -> Optional[User]:
    for user in users_db:
        if user.id == user_id:
            return user
    return None


def get_file_by_id(file_id: int) -> Optional[StoredFile]:
    for file_item in files_db:
        if file_item.id == file_id:
            return file_item
    return None


def file_to_response(file_item: StoredFile) -> Dict[str, object]:
    owner = get_user_by_id(file_item.owner_id)
    owner_name = owner.username if owner else "Неизвестно"
    return {
        "id": file_item.id,
        "имя": file_item.name,
        "размер_кб": file_item.size_kb,
        "владелец": owner_name,
    }


def get_current_user(
    user_id: Optional[int] = Query(default=None),
    x_user_id: Optional[str] = Header(default=None, alias="X-User-Id"),
) -> User:
    raw_user_id: Optional[str]
    if user_id is not None:
        raw_user_id = str(user_id)
    else:
        raw_user_id = x_user_id

    if raw_user_id is None:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Не указан текущий пользователь",
        )

    try:
        actual_user_id = int(raw_user_id)
    except ValueError as exc:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Идентификатор пользователя должен быть числом",
        ) from exc

    user = get_user_by_id(actual_user_id)
    if user is None:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Пользователь не найден",
        )
    return user


def ensure_admin(current_user: User) -> User:
    if current_user.role != "admin":
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Доступ разрешён только администратору",
        )
    return current_user


def authorize_file_access(file_id: int, current_user: User) -> StoredFile:
    file_item = get_file_by_id(file_id)
    if file_item is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Файл не найден",
        )

    if current_user.role == "admin" or file_item.owner_id == current_user.id:
        return file_item

    raise HTTPException(
        status_code=status.HTTP_404_NOT_FOUND,
        detail="Файл не найден",
    )


def check_file_permissions(
    file_id: int,
    current_user: User = Depends(get_current_user),
) -> StoredFile:
    return authorize_file_access(file_id, current_user)


def remove_file(file_id: int) -> None:
    global files_db
    files_db = [file_item for file_item in files_db if file_item.id != file_id]


def build_page_context(
    request: Request,
    current_user: User,
    message: str = "",
    selected_file: Optional[Dict[str, object]] = None,
) -> Dict[str, object]:
    my_files = [
        file_to_response(file_item)
        for file_item in files_db
        if file_item.owner_id == current_user.id
    ]
    all_files = [file_to_response(file_item) for file_item in files_db]
    return {
        "request": request,
        "users": users_db,
        "current_user": current_user,
        "message": message,
        "selected_file": selected_file,
        "my_files": my_files,
        "all_files": all_files if current_user.role == "admin" else [],
    }


@app.get("/")
async def home(
    request: Request,
    user_id: int = Query(default=1),
    message: str = Query(default=""),
) -> object:
    current_user = get_user_by_id(user_id) or users_db[0]
    context = build_page_context(request, current_user, message=message)
    return templates.TemplateResponse("files.html", context)


@app.get("/ui/file")
async def ui_open_file(
    request: Request,
    user_id: int,
    file_id: int,
) -> object:
    current_user = get_user_by_id(user_id) or users_db[0]
    selected_file: Optional[Dict[str, object]] = None
    message = ""

    try:
        selected_file = file_to_response(authorize_file_access(file_id, current_user))
        message = "Файл успешно открыт"
    except HTTPException as exc:
        message = str(exc.detail)

    context = build_page_context(
        request=request,
        current_user=current_user,
        message=message,
        selected_file=selected_file,
    )
    return templates.TemplateResponse("files.html", context)


@app.post("/ui/delete")
async def ui_delete_file(request: Request) -> RedirectResponse:
    raw_body = (await request.body()).decode("utf-8", "replace")
    data = parse_qs(raw_body)

    try:
        user_id = int(data.get("user_id", ["1"])[0])
        file_id = int(data.get("file_id", ["0"])[0])
    except ValueError:
        params = urlencode({"user_id": 1, "message": "Некорректные данные формы"})
        return RedirectResponse(url=f"/?{params}", status_code=status.HTTP_303_SEE_OTHER)

    current_user = get_user_by_id(user_id) or users_db[0]

    try:
        authorize_file_access(file_id, current_user)
        remove_file(file_id)
        message = "Файл удалён"
    except HTTPException as exc:
        message = str(exc.detail)

    params = urlencode({"user_id": current_user.id, "message": message})
    return RedirectResponse(url=f"/?{params}", status_code=status.HTTP_303_SEE_OTHER)


@app.get("/files/my")
async def get_my_files(current_user: User = Depends(get_current_user)) -> Dict[str, object]:
    my_files = [
        file_to_response(file_item)
        for file_item in files_db
        if file_item.owner_id == current_user.id
    ]
    return {"пользователь": current_user.username, "файлы": my_files}


@app.get("/files/all")
async def get_all_files(current_user: User = Depends(get_current_user)) -> Dict[str, object]:
    ensure_admin(current_user)
    return {
        "пользователь": current_user.username,
        "файлы": [file_to_response(file_item) for file_item in files_db],
    }


@app.get("/files/{file_id}")
async def get_file_info(file_item: StoredFile = Depends(check_file_permissions)) -> Dict[str, object]:
    return file_to_response(file_item)


@app.delete("/files/{file_id}")
async def delete_file(file_item: StoredFile = Depends(check_file_permissions)) -> Dict[str, object]:
    response_file = file_to_response(file_item)
    remove_file(file_item.id)
    return {"сообщение": "Файл удалён", "файл": response_file}
