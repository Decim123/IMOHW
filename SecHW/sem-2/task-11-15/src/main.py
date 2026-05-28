from __future__ import annotations

import base64
import json
import logging
import os
import re
import secrets
import uuid
from pathlib import Path
from typing import Dict, List, Optional
from urllib.parse import quote, urlencode

import filetype
from cryptography.fernet import Fernet, InvalidToken
from dotenv import load_dotenv
from fastapi import Depends, FastAPI, File, Form, Header, HTTPException, Query, Request, UploadFile, status
from fastapi.responses import FileResponse, JSONResponse, RedirectResponse, Response
from fastapi.templating import Jinja2Templates

from src.schemas import StoredFile, User


BASE_DIR = Path(__file__).resolve().parent.parent
STORAGE_DIR = BASE_DIR / "storage"
LOGS_DIR = BASE_DIR / "logs"
LOG_FILE_PATH = LOGS_DIR / "app.log"
METADATA_PATH = STORAGE_DIR / "files_db.json"
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
LOGIN_CREDENTIALS = {
    "alice": {"user_id": 1},
    "bob": {"user_id": 2},
    "admin": {"user_id": 3},
}


def configure_logging() -> logging.Logger:
    LOGS_DIR.mkdir(parents=True, exist_ok=True)
    app_logger = logging.getLogger("app")
    if app_logger.handlers:
        return app_logger

    app_logger.setLevel(logging.INFO)
    formatter = logging.Formatter("%(asctime)s %(levelname)s %(message)s")

    file_handler = logging.FileHandler(LOG_FILE_PATH, encoding="utf-8")
    file_handler.setFormatter(formatter)

    stream_handler = logging.StreamHandler()
    stream_handler.setFormatter(formatter)

    app_logger.addHandler(file_handler)
    app_logger.addHandler(stream_handler)
    app_logger.propagate = False
    return app_logger


logger = configure_logging()

load_dotenv(BASE_DIR / ".env", encoding="utf-8-sig")
FERNET_KEY = os.getenv("FERNET_KEY")
if not FERNET_KEY:
    raise RuntimeError("Переменная FERNET_KEY не найдена в .env")

cipher_suite = Fernet(FERNET_KEY.encode("utf-8"))

app = FastAPI(title="Задача 13")
templates = Jinja2Templates(directory=str(BASE_DIR / "templates"))

users_db: List[User] = [
    User(id=1, username="Алиса", role="user"),
    User(id=2, username="Боб", role="user"),
    User(id=3, username="Администратор", role="admin"),
]

files_db: List[StoredFile] = []


SECURITY_HEADERS = {
    "Content-Security-Policy": "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; form-action 'self'; base-uri 'self'; frame-ancestors 'none'",
    "X-Frame-Options": "DENY",
    "X-Content-Type-Options": "nosniff",
    "Referrer-Policy": "no-referrer",
}


def get_user_by_id(user_id: int) -> Optional[User]:
    for user in users_db:
        if user.id == user_id:
            return user
    return None


def get_user_label(user: Optional[User]) -> str:
    return user.username if user else "anonymous"


def log_security_event(action: str, actor: str, details: str = "") -> None:
    suffix = f" {details}" if details else ""
    logger.warning("Security event: actor=%s action=%s%s", actor, action, suffix)


def build_login_secret_env_name(login: str) -> str:
    return f"LOGIN_{login.upper()}_AUTH"


def authenticate_user(login: str, password: str) -> User:
    normalized_login = login.strip().lower()
    credentials = LOGIN_CREDENTIALS.get(normalized_login)

    expected_password = os.getenv(build_login_secret_env_name(normalized_login), "") if credentials else ""

    if credentials is None or not expected_password or not secrets.compare_digest(expected_password, password):
        log_security_event("login_failed", normalized_login or "anonymous", "reason=invalid_credentials")
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid username or password.",
        )

    user = get_user_by_id(credentials["user_id"])
    if user is None:
        logger.error("Authentication configuration points to missing user_id=%s", credentials["user_id"])
        raise RuntimeError("Configured login references a missing user")

    logger.info("Successful login: actor=%s user_id=%s", normalized_login, user.id)
    return user


def ensure_storage_dir() -> None:
    STORAGE_DIR.mkdir(parents=True, exist_ok=True)


def ensure_logs_dir() -> None:
    LOGS_DIR.mkdir(parents=True, exist_ok=True)


def save_metadata() -> None:
    ensure_storage_dir()
    data = [file_item.model_dump() for file_item in files_db]
    METADATA_PATH.write_text(
        json.dumps(data, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


def build_initial_files_db() -> List[StoredFile]:
    ensure_storage_dir()
    initial_files: List[StoredFile] = []

    for item in INITIAL_FILE_DEFINITIONS:
        relative_path = Path("storage") / item["stored_name"]
        absolute_path = (BASE_DIR / relative_path).resolve()
        if not absolute_path.exists():
            absolute_path.write_bytes(SEED_PNG_BYTES)
        initial_files.append(
            StoredFile(
                id=item["id"],
                owner_id=item["owner_id"],
                original_name=item["original_name"],
                path=str(relative_path),
                size_bytes=len(SEED_PNG_BYTES),
                content_type="image/png",
                is_encrypted=False,
            )
        )

    return initial_files


def load_metadata() -> List[StoredFile]:
    if not METADATA_PATH.exists():
        initial_files = build_initial_files_db()
        data = [file_item.model_dump() for file_item in initial_files]
        METADATA_PATH.write_text(
            json.dumps(data, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
        return initial_files

    raw_data = json.loads(METADATA_PATH.read_text(encoding="utf-8"))
    loaded_files: List[StoredFile] = []
    metadata_changed = False

    for item in raw_data:
        file_item = StoredFile(**item)
        disk_path = resolve_disk_path(file_item.path)
        if disk_path.exists():
            loaded_files.append(file_item)
        else:
            metadata_changed = True

    if metadata_changed:
        global files_db
        files_db = loaded_files
        save_metadata()

    return loaded_files


@app.on_event("startup")
def startup_event() -> None:
    global files_db
    ensure_storage_dir()
    ensure_logs_dir()
    files_db = load_metadata()
    logger.info("Application startup complete")


@app.middleware("http")
async def unhandled_exception_logging_middleware(request: Request, call_next) -> Response:
    try:
        response = await call_next(request)
    except HTTPException:
        raise
    except Exception:
        logger.exception("Unhandled error for %s %s", request.method, request.url.path)
        response = JSONResponse(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            content={"detail": "We are sorry, something went wrong."},
        )

    for header_name, header_value in SECURITY_HEADERS.items():
        response.headers.setdefault(header_name, header_value)
    return response


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
        log_security_event(
            "admin_endpoint_denied",
            get_user_label(current_user),
            "endpoint=/files/all",
        )
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

    log_security_event(
        "idor_attempt",
        get_user_label(current_user),
        f"file_id={file_id} owner_id={file_item.owner_id}",
    )
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
        "зашифрован": file_item.is_encrypted,
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
    save_metadata()
    logger.info("File deleted: actor_id=%s file_id=%s path=%s", file_item.owner_id, file_item.id, file_item.path)
    return file_item


async def read_uploaded_bytes(uploaded_file: UploadFile) -> bytes:
    first_chunk = await uploaded_file.read(READ_CHUNK_SIZE)
    if not first_chunk:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Файл пустой",
        )

    detected_kind = filetype.guess(first_chunk)
    if detected_kind is None or detected_kind.mime not in ALLOWED_MIME_TYPES:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Разрешены только JPEG и PNG, тип проверяется по содержимому файла",
        )

    total_size = len(first_chunk)
    file_data = bytearray(first_chunk)

    while True:
        chunk = await uploaded_file.read(READ_CHUNK_SIZE)
        if not chunk:
            break

        total_size += len(chunk)
        if total_size > MAX_FILE_SIZE_BYTES:
            raise HTTPException(
                status_code=status.HTTP_413_REQUEST_ENTITY_TOO_LARGE,
                detail="Файл слишком большой. Максимум 2 МБ",
            )
        file_data.extend(chunk)

    return bytes(file_data)


def detect_allowed_kind(file_data: bytes) -> filetype.Type:
    detected_kind = filetype.guess(file_data)
    if detected_kind is None or detected_kind.mime not in ALLOWED_MIME_TYPES:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Разрешены только JPEG и PNG, тип проверяется по содержимому файла",
        )
    return detected_kind


async def save_uploaded_file(
    uploaded_file: UploadFile,
    current_user: User,
    encrypt: bool,
) -> StoredFile:
    ensure_storage_dir()
    try:
        file_data = await read_uploaded_bytes(uploaded_file)
    finally:
        await uploaded_file.close()

    detected_kind = detect_allowed_kind(file_data)
    original_name = sanitize_original_name(uploaded_file.filename)
    suffix = ".enc" if encrypt else ALLOWED_MIME_TYPES[detected_kind.mime]
    stored_name = f"{uuid.uuid4()}{suffix}"
    relative_path = Path("storage") / stored_name
    disk_path = resolve_disk_path(str(relative_path))
    stored_bytes = cipher_suite.encrypt(file_data) if encrypt else file_data
    disk_path.write_bytes(stored_bytes)

    stored_file = StoredFile(
        id=next_file_id(),
        owner_id=current_user.id,
        original_name=original_name,
        path=str(relative_path),
        size_bytes=len(file_data),
        content_type=detected_kind.mime,
        is_encrypted=encrypt,
    )
    files_db.append(stored_file)
    save_metadata()
    logger.info(
        "File uploaded: actor=%s file_id=%s original_name=%s encrypted=%s",
        current_user.username,
        stored_file.id,
        stored_file.original_name,
        stored_file.is_encrypted,
    )
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


def build_login_context(request: Request, message: str = "") -> Dict[str, object]:
    return {
        "request": request,
        "message": message,
        "login_accounts": [
            {"username": "alice", "secret_env": build_login_secret_env_name("alice"), "role": "user"},
            {"username": "bob", "secret_env": build_login_secret_env_name("bob"), "role": "user"},
            {"username": "admin", "secret_env": build_login_secret_env_name("admin"), "role": "admin"},
        ],
    }


def build_redirect(current_user: User, message: str) -> RedirectResponse:
    params = urlencode({"user_id": current_user.id, "message": message})
    return RedirectResponse(url=f"/?{params}", status_code=status.HTTP_303_SEE_OTHER)


def wants_html_response(request: Request) -> bool:
    accept_header = request.headers.get("accept", "")
    return "text/html" in accept_header


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


@app.get("/login")
async def login_page(
    request: Request,
    message: str = Query(default=""),
) -> object:
    return templates.TemplateResponse("login.html", build_login_context(request, message))


@app.post("/login")
async def login(
    request: Request,
    username: str = Form(...),
    password: str = Form(...),
) -> object:
    try:
        current_user = authenticate_user(username, password)
    except HTTPException as exc:
        if wants_html_response(request):
            return templates.TemplateResponse(
                "login.html",
                build_login_context(request, str(exc.detail)),
                status_code=exc.status_code,
            )
        raise

    if wants_html_response(request):
        params = urlencode(
            {
                "user_id": current_user.id,
                "message": f"Вход выполнен успешно: {current_user.username}",
            }
        )
        return RedirectResponse(url=f"/?{params}", status_code=status.HTTP_303_SEE_OTHER)

    return {
        "message": "Login successful.",
        "user_id": current_user.id,
        "username": current_user.username,
        "role": current_user.role,
    }


@app.get("/cause_error")
async def cause_error() -> Dict[str, int]:
    return {"value": 1 / 0}


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
    encrypt: Optional[str] = Form(default=None),
    current_user: User = Depends(get_current_user),
) -> RedirectResponse:
    encryption_enabled = encrypt is not None
    try:
        stored_file = await save_uploaded_file(uploaded_file, current_user, encrypt=encryption_enabled)
        suffix = " с шифрованием" if stored_file.is_encrypted else ""
        return build_redirect(current_user, f"Файл загружен{suffix}: {stored_file.original_name}")
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
    encrypt: bool = Query(default=False),
    uploaded_file: UploadFile = File(...),
    current_user: User = Depends(get_current_user),
) -> Dict[str, object]:
    stored_file = await save_uploaded_file(uploaded_file, current_user, encrypt=encrypt)
    return {"сообщение": "Файл загружен", "файл": file_to_response(stored_file)}


@app.get("/files/{file_id}/download")
async def download_file(
    file_item: StoredFile = Depends(check_file_permissions),
) -> Response:
    disk_path = resolve_disk_path(file_item.path)
    if not disk_path.exists():
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Файл на диске не найден",
        )

    if not file_item.is_encrypted:
        response = FileResponse(path=disk_path, media_type=file_item.content_type)
        response.headers["Content-Disposition"] = build_content_disposition(file_item.original_name)
        return response

    encrypted_data = disk_path.read_bytes()
    try:
        decrypted_data = cipher_suite.decrypt(encrypted_data)
    except InvalidToken as exc:
        logger.exception("Failed to decrypt file: file_id=%s path=%s", file_item.id, file_item.path)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Не удалось расшифровать файл",
        ) from exc

    return Response(
        content=decrypted_data,
        media_type=file_item.content_type,
        headers={"Content-Disposition": build_content_disposition(file_item.original_name)},
    )


@app.get("/files/{file_id}")
async def get_file_info(file_item: StoredFile = Depends(check_file_permissions)) -> Dict[str, object]:
    return file_to_response(file_item)


@app.delete("/files/{file_id}")
async def delete_file(file_item: StoredFile = Depends(check_file_permissions)) -> Dict[str, object]:
    removed_file = remove_file(file_item.id)
    return {"сообщение": "Файл удалён", "файл": file_to_response(removed_file)}

