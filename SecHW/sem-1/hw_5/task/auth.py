import hashlib
import time
from typing import Optional

from passlib.context import CryptContext

from user import User, UserStorage
import validation

pwd_context = CryptContext(schemes=["argon2"], deprecated="auto")


def _hash_argon2(password: str) -> str:
    return pwd_context.hash(password)


def _verify_argon2(password: str, hashed: str) -> bool:
    try:
        return pwd_context.verify(password, hashed)
    except Exception:
        return False


def _is_md5_hash(value: str) -> bool:

    # Проверка, похоже ли значение на md5-хеш (32 hex-символа)
    
    if not isinstance(value, str):
        return False
    if len(value) != 32:
        return False
    hex_chars = "0123456789abcdef"
    return all(ch in hex_chars for ch in value.lower())


def _compute_delay(n: int) -> float:
    """
    Формула из README:
      - для n >= 1: 1.5^n + 1
      - для n <= 0: 0
    где n — количество подряд идущих неудачных попыток.
    """
    if n <= 0:
        return 0.0
    return (1.5 ** n) + 1.0


def _apply_backoff(user: User, storage: UserStorage, success: bool) -> None:
    
    # Обновляет счётчики пользователя после попытки логина, при неуспешном, вызывает time.sleep() с рассчитанной задержкой

    if success:
        # Успешно: сброс счётчика и задержки
        user.failed_attempts = 0
        user.backoff_seconds = 0.0
        user.save(storage)
        return

    # Неуспешно: увеличить счётчик, пересчитать задержку
    user.failed_attempts += 1
    delay = _compute_delay(user.failed_attempts)
    user.backoff_seconds = delay
    user.save(storage)

    time.sleep(delay)


def register_user(storage: UserStorage, username: str, email: str, password: str) -> User:
    
    # Регистрирует нового пользователя и сразу пароль в виде Argon2-хеша
    # md5 используется только для старых пользователей
    
    if User.exists(storage, username):
        raise ValueError("Пользователь с таким username уже существует")

    # валидация пароля
    _ = validation.validate_password(password)

    password_hash = _hash_argon2(password)
    user = User(
        username=username,
        email=email,
        password_hash=password_hash,
        failed_attempts=0,
        backoff_seconds=0.0,
    )
    user.save(storage)
    return user


def verify_credentials(storage: UserStorage, username: str, password: str) -> bool:
    """
    Проверяет логин и пароль пользователя.

    md5: при успешном логине миграция на Argon2
    новые записи: сразу на Argon2
    рост задержки по формуле при каждой неудачной попытке
    """
    user: Optional[User] = User.load(storage, username)
    if user is None:
        return False

    stored_hash = user.password_hash
    success = False

    if _is_md5_hash(stored_hash):
        # Старый пользователь с md5
        md5_hex = hashlib.md5(password.encode("utf-8")).hexdigest()
        if md5_hex == stored_hash:
            # миграция на Argon2
            user.password_hash = _hash_argon2(password)
            success = True
    else:
        # считать,  что это Argon2 хеш
        if _verify_argon2(password, stored_hash):
            success = True

    _apply_backoff(user, storage, success)
    return success


''' результат

♰ skyceo 22:52 SecHW/hw_5/task
❯  ./run_tests.sh 3
🔐 Запуск тестов argon2 с ростом задержки
============================================================= test session starts ==============================================================
platform linux -- Python 3.13.7, pytest-8.4.2, pluggy-1.6.0
rootdir: /home/skyceo/projects/IMOHW/SecHW/hw_5/task
configfile: pytest.ini
plugins: django-4.11.1
collected 7 items                                                                                                                              

tests/test_migration_argon2.py ..                                                                                                        [ 28%]
tests/test_delay.py ...                                                                                                                  [ 71%]
tests/test_password_charset_policy.py .                                                                                                  [ 85%]
tests/test_password_length_policy.py .                                                                                                   [100%]

=============================================================== warnings summary ===============================================================
tests/test_migration_argon2.py::test_md5_user_is_migrated_to_argon2_on_successful_login
                     ts/IMOHW/SecHW/venv/lib/python3.13/site-packages/passlib/handlers/argon2.py:716: DeprecationWarning: Accessing argon2.__version__ is deprecated and will be removed in a future release. Use importlib.metadata directly to query for argon2-cffi's packaging metadata.
    _argon2_cffi.__version__, max_version)

-- Docs: https://docs.pytest.org/en/stable/how-to/capture-warnings.html
========================================================= 7 passed, 1 warning in 1.22s =========================================================

♰ skyceo 22:52 SecHW/hw_5/task
❯  

'''