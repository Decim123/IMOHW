from typing import Any, Tuple
import time

import crypto
import storage
import user


def record_token(payload: dict[str, Any]) -> None:
    # токен в storage
    db = storage.load_tokens()
    tokens = db.get("tokens", [])

    tokens.append(
        {
            "jti": payload.get("jti"),
            "sub": payload.get("sub"),
            "typ": payload.get("typ"),
            "exp": payload.get("exp"),
            "revoked": False,
        }
    )

    db["tokens"] = tokens
    storage.save_tokens(db)


def revoke_by_jti(jti: str) -> None:
    db = storage.load_tokens()
    tokens = db.get("tokens", [])
    for t in tokens:
        if t.get("jti") == jti:
            t["revoked"] = True
    db["tokens"] = tokens
    storage.save_tokens(db)


def is_revoked(jti: str) -> bool:
    db = storage.load_tokens()
    tokens = db.get("tokens", [])
    for t in tokens:
        if t.get("jti") == jti:
            return bool(t.get("revoked", False))
    return False


def is_expired(exp: Any) -> bool:
    try:
        exp_ts = int(exp)
    except (TypeError, ValueError):
        return True
    now = int(time.time())
    return exp_ts < now


def login(username: str, password: str) -> Tuple[str, str]:
    # Логин: пользователь и пароль -> проверка -> пара токенов
    u = user.get_user(username)
    if not u or not user.verify_password(u, password):
        raise RuntimeError("invalid credentials")

    access, a_payload = crypto.issue_access(username)
    refresh, r_payload = crypto.issue_refresh(username)

    record_token(a_payload)
    record_token(r_payload)

    return access, refresh


def verify_access(access: str) -> dict[str, Any]:
    payload = crypto.decode(access)
    # проверка access токена
    if payload.get("typ") != "access":
        raise RuntimeError("wrong token type")

    if is_revoked(payload["jti"]):
        raise RuntimeError("token revoked")

    if is_expired(payload["exp"]):
        raise RuntimeError("token expired")

    return payload


def refresh_pair(refresh_token: str) -> Tuple[str, str]:
    """
    Обновляем пару токенов по refresh
    1 проверяем JWT и exp
    2 убеждаемся, что typ == "refresh"
    3 проверяем, что не отозван
    4 отзываем старый refresh
    5 выдаём новые access/refresh и записываем их
    """
    payload = crypto.decode(refresh_token)

    if payload.get("typ") != "refresh":
        # это важно для теста test_refresh_type_enforced
        raise RuntimeError("wrong token type")

    if is_revoked(payload["jti"]):
        # важно, чтобы было слово "revoked"
        raise RuntimeError("refresh token revoked")

    if is_expired(payload["exp"]):
        raise RuntimeError("refresh token expired")

    # старый refresh отзываем
    revoke_by_jti(payload["jti"])

    access, a_payload = crypto.issue_access(payload["sub"])
    new_refresh, r_payload = crypto.issue_refresh(payload["sub"])

    record_token(a_payload)
    record_token(r_payload)

    return access, new_refresh


def revoke(token: str) -> None:
    
    # декодируем и помечаем jti как revoked
    
    payload = crypto.decode(token)
    jti = payload.get("jti")
    if not jti:
        raise RuntimeError("token has no jti")
    revoke_by_jti(jti)


def introspect(token: str) -> dict[str, Any]:
    try:
        payload = crypto.decode(token)
        active = (not is_revoked(payload["jti"])) and (not is_expired(payload["exp"]))
        return {
            "active": active,
            "sub": payload.get("sub"),
            "typ": payload.get("typ"),
            "exp": payload.get("exp"),
            "jti": payload.get("jti"),
        }
    except Exception:
        return {"active": False, "error": "invalid_token"}


'''

♰ skyceo 05:45 SecHW/hw_6/task
❯  
rm -f data/users.json data/tokens.json
pytest -q

........                                                                                                         [100%]
8 passed in 4.26s

♰ skyceo 05:47 SecHW/hw_6/task
❯  

'''