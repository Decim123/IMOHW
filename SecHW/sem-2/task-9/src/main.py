from __future__ import annotations

import base64
import re
import uuid
from copy import deepcopy
from pathlib import Path
from typing import Dict, List, Optional
from urllib.parse import quote, urlencode

import filetype
from fastapi import Depends, FastAPI, File, Form, Header, HTTPException, Query, Request, UploadFile, status
from fastapi.responses import FileResponse, RedirectResponse
from fastapi.templating import Jinja2Templates

from src.schemas import StoredFile, User


BASE_DIR = Path(__file__).resolve().parent.parent
STORAGE_DIR = BASE_DIR / "storage"
MAX_FILE_SIZE_BYTES = 2 * 1024 * 1024
READ_CHUNK_SIZE = 64 * 1024
ALLOWED_MIME_TYPES = {
    "image/jpeg": ".jpg",
    "image/png": ".png",
}
SEED_PNG_BYTES = base64.b64decode(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO/aRX0AAAAASUVORK5CYII="
)
INITIAL_FILE_DEFINITIONS = [
    {"id": 1, "owner_id": 1, "original_name": "alice_report.png", "stored_name": "seed-alice.png"},
    {"id": 2, "owner_id": 2, "original_name": "bob_report.png", "stored_name": "seed-bob.png"},
    {"id": 3, "owner_id": 3, "original_name": "admin_report.png", "stored_name": "seed-admin.png"},
]

app = FastAPI(title="Задача 9")
templates = Jinja2Templates(directory=str(BASE_DIR / "templates"))

users_db: List[User] = [
    User(id=1, username="Алиса", role="user"),
    User(id=2, username="Боб", role="user"),
    User(id=3, username="Администратор", role="admin"),
]

INITIAL_FILES_DB: List[StoredFile] = []
files_db: List[StoredFile] = []


def get_user_by_id(user_id: int) -> Optional[User]:
    for user in users_db:
        if user.id == user_id:
            return user
    return None


def ensure_storage_dir() -> None:
    STORAGE_DIR.mkdir(parents=True, exist_ok=True)


def build_initial_files_db() -> List[StoredFile]:
    ensure_storage_dir()
    initial_files: List[StoredFile] = []

    for item in INITIAL_FILE_DEFINITIONS:
        relative_path = Path("storage") / item["stored_name"]
        absolute_path = (BASE_DIR / relative_path).resolve()
        absolute_path.write_bytes(SEED_PNG_BYTES)
        initial_files.append(
            StoredFile(
                id=item["id"],
                owner_id=item["owner_id"],
                original_name=item["original_name"],
                path=str(relative_path),
                size_bytes=len(SEED_PNG_BYTES),
                content_type="image/png",
            )
        )

    return initial_files


def reset_files_db() -> None:
    global files_db
    files_db = deepcopy(INITIAL_FILES_DB)


@app.on_event("startup")
def startup_event() -> None:
    global INITIAL_FILES_DB
    INITIAL_FILES_DB = build_initial_files_db()
    reset_files_db()


def get_file_by_id(file_id: int) -> Optional[StoredFile]:
    for file_item in files_db:
        if file_item.id == file_id:
            return file_item
    return None


def resolve_disk_path(relative_path: str) -> Path:
    storage_root = STORAGE_DIR.resolve()
    disk_path = (BASE_DIR / relative_path).resolve()
    if storage_root != disk_path and storage_root not in disk_path.parents:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Файл на диске не найден",
        )
    return disk_path


def sanitize_original_name(filename: Optional[str]) -> str:
    candidate = Path(filename or "").name
    candidate = re.sub(r"[\r\n\t]+", "", candidate).strip()
    return candidate or "file.bin"


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


def file_to_response(file_item: StoredFile) -> Dict[str, object]:
    owner = get_user_by_id(file_item.owner_id)
    owner_name = owner.username if owner else "Неизвестно"
    return {
        "id": file_item.id,
        "имя": file_item.original_name,
        "размер_байт": file_item.size_bytes,
        "тип": file_item.content_type,
        "владелец": owner_name,
    }


def next_file_id() -> int:
    max_id = max((file_item.id for file_item in files_db), default=0)
    return max_id + 1


def delete_file_from_disk(file_item: StoredFile) -> None:
    disk_path = resolve_disk_path(file_item.path)
    if disk_path.exists():
        disk_path.unlink()


def remove_file(file_id: int) -> StoredFile:
    global files_db
    file_item = get_file_by_id(file_id)
    if file_item is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Файл не найден",
        )

    delete_file_from_disk(file_item)
    files_db = [current_file for current_file in files_db if current_file.id != file_id]
    return file_item


async def save_uploaded_file(uploaded_file: UploadFile, current_user: User) -> StoredFile:
    ensure_storage_dir()

    first_chunk = await uploaded_file.read(READ_CHUNK_SIZE)
    if not first_chunk:
        await uploaded_file.close()
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Файл пустой",
        )

    detected_kind = filetype.guess(first_chunk)
    if detected_kind is None or detected_kind.mime not in ALLOWED_MIME_TYPES:
        await uploaded_file.close()
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Разрешены только JPEG и PNG, тип проверяется по содержимому файла",
        )

    total_size = len(first_chunk)
    if total_size > MAX_FILE_SIZE_BYTES:
        await uploaded_file.close()
        raise HTTPException(
            status_code=status.HTTP_413_REQUEST_ENTITY_TOO_LARGE,
            detail="Файл слишком большой. Максимум 2 МБ",
        )

    stored_name = f"{uuid.uuid4()}{ALLOWED_MIME_TYPES[detected_kind.mime]}"
    relative_path = Path("storage") / stored_name
    disk_path = resolve_disk_path(str(relative_path))
    original_name = sanitize_original_name(uploaded_file.filename)

    try:
        with disk_path.open("wb") as destination:
            destination.write(first_chunk)

            while True:
                chunk = await uploaded_file.read(READ_CHUNK_SIZE)
                if not chunk:
                    break

                total_size += len(chunk)
                if total_size > MAX_FILE_SIZE_BYTES:
                    destination.close()
                    disk_path.unlink(missing_ok=True)
                    raise HTTPException(
                        status_code=status.HTTP_413_REQUEST_ENTITY_TOO_LARGE,
                        detail="Файл слишком большой. Максимум 2 МБ",
                    )

                destination.write(chunk)
    finally:
        await uploaded_file.close()

    stored_file = StoredFile(
        id=next_file_id(),
        owner_id=current_user.id,
        original_name=original_name,
        path=str(relative_path),
        size_bytes=total_size,
        content_type=detected_kind.mime,
    )
    files_db.append(stored_file)
    return stored_file


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
        "max_file_size_mb": 2,
    }


def build_redirect(current_user: User, message: str) -> RedirectResponse:
    params = urlencode({"user_id": current_user.id, "message": message})
    return RedirectResponse(url=f"/?{params}", status_code=status.HTTP_303_SEE_OTHER)


def build_content_disposition(filename: str) -> str:
    quoted_name = quote(filename)
    return f"attachment; filename*=UTF-8''{quoted_name}"


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


@app.post("/ui/upload")
async def ui_upload_file(
    uploaded_file: UploadFile = File(...),
    current_user: User = Depends(get_current_user),
) -> RedirectResponse:
    try:
        stored_file = await save_uploaded_file(uploaded_file, current_user)
        return build_redirect(current_user, f"Файл загружен: {stored_file.original_name}")
    except HTTPException as exc:
        return build_redirect(current_user, str(exc.detail))


@app.post("/ui/delete")
async def ui_delete_file(
    user_id: int = Form(...),
    file_id: int = Form(...),
) -> RedirectResponse:
    current_user = get_user_by_id(user_id) or users_db[0]

    try:
        authorize_file_access(file_id, current_user)
        remove_file(file_id)
        message = "Файл удалён"
    except HTTPException as exc:
        message = str(exc.detail)

    return build_redirect(current_user, message)


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


@app.post("/files/upload", status_code=status.HTTP_201_CREATED)
async def upload_file(
    uploaded_file: UploadFile = File(...),
    current_user: User = Depends(get_current_user),
) -> Dict[str, object]:
    stored_file = await save_uploaded_file(uploaded_file, current_user)
    return {"сообщение": "Файл загружен", "файл": file_to_response(stored_file)}


@app.get("/files/{file_id}/download")
async def download_file(
    file_item: StoredFile = Depends(check_file_permissions),
) -> FileResponse:
    disk_path = resolve_disk_path(file_item.path)
    if not disk_path.exists():
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Файл на диске не найден",
        )

    response = FileResponse(path=disk_path, media_type=file_item.content_type)
    response.headers["Content-Disposition"] = build_content_disposition(file_item.original_name)
    return response


@app.get("/files/{file_id}")
async def get_file_info(file_item: StoredFile = Depends(check_file_permissions)) -> Dict[str, object]:
    return file_to_response(file_item)


@app.delete("/files/{file_id}")
async def delete_file(file_item: StoredFile = Depends(check_file_permissions)) -> Dict[str, object]:
    removed_file = remove_file(file_item.id)
    return {"сообщение": "Файл удалён", "файл": file_to_response(removed_file)}
