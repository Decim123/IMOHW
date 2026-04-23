from __future__ import annotations

import base64
import subprocess
import sys
import time
from pathlib import Path

import requests


PROJECT_ROOT = Path(__file__).resolve().parent
BASE_URL = "http://127.0.0.1:8010"
MAX_FILE_SIZE_BYTES = 2 * 1024 * 1024
STORAGE_DIR = PROJECT_ROOT / "storage"
TINY_PNG = base64.b64decode(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO/aRX0AAAAASUVORK5CYII="
)


def wait_for_server() -> None:
    for _ in range(50):
        try:
            response = requests.get(f"{BASE_URL}/", timeout=0.5)
            if response.status_code == 200:
                return
        except requests.RequestException:
            time.sleep(0.2)
    raise RuntimeError("Сервер не запустился вовремя")


def assert_status(name: str, response: requests.Response, expected_status: int) -> None:
    if response.status_code != expected_status:
        raise AssertionError(
            f"{name}: ожидался статус {expected_status}, получен {response.status_code}, тело: {response.text}"
        )


def current_storage_files() -> set[Path]:
    return set(STORAGE_DIR.glob("*"))


def main() -> None:
    server = subprocess.Popen(
        [
            sys.executable,
            "-m",
            "uvicorn",
            "src.main:app",
            "--host",
            "127.0.0.1",
            "--port",
            "8010",
        ],
        cwd=PROJECT_ROOT,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
    )

    try:
        wait_for_server()

        alice_headers = {"X-User-Id": "1"}
        bob_headers = {"X-User-Id": "2"}

        fake_upload = requests.post(
            f"{BASE_URL}/files/upload",
            headers=alice_headers,
            files={"uploaded_file": ("fake.jpg", b"eto-ne-kartinka", "image/jpeg")},
            timeout=10,
        )
        assert_status("Test 1 (Fake JPG)", fake_upload, 400)
        print("Test 1 (Fake JPG): текстовый файл под видом JPG отклонён -> OK")

        huge_png = TINY_PNG + (b"0" * (MAX_FILE_SIZE_BYTES + 1))
        large_upload = requests.post(
            f"{BASE_URL}/files/upload",
            headers=alice_headers,
            files={"uploaded_file": ("big.png", huge_png, "image/png")},
            timeout=20,
        )
        assert_status("Test 2 (Size)", large_upload, 413)
        print("Test 2 (Size): файл больше 2 МБ отклонён -> OK")

        plain_upload = requests.post(
            f"{BASE_URL}/files/upload",
            headers=alice_headers,
            files={"uploaded_file": ("cat.png", TINY_PNG, "image/png")},
            timeout=10,
        )
        assert_status("Test 3 (Plain upload)", plain_upload, 201)
        plain_file_id = plain_upload.json()["файл"]["id"]
        if plain_upload.json()["файл"]["зашифрован"] is not False:
            raise AssertionError("Test 3 (Plain upload): файл не должен быть помечен как зашифрованный")
        print("Test 3 (Plain upload): обычный PNG загружен без шифрования -> OK")

        storage_before = current_storage_files()
        encrypted_upload = requests.post(
            f"{BASE_URL}/files/upload?encrypt=true",
            headers=alice_headers,
            files={"uploaded_file": ("salary.png", TINY_PNG, "image/png")},
            timeout=10,
        )
        assert_status("Test 4 (Encrypted upload)", encrypted_upload, 201)
        encrypted_payload = encrypted_upload.json()["файл"]
        encrypted_file_id = encrypted_payload["id"]
        if encrypted_payload["зашифрован"] is not True:
            raise AssertionError("Test 4 (Encrypted upload): файл должен быть помечен как зашифрованный")
        storage_after = current_storage_files()
        new_files = storage_after - storage_before
        if len(new_files) != 1:
            raise AssertionError("Test 4 (Encrypted upload): не удалось определить новый файл на диске")
        encrypted_disk_file = new_files.pop()
        if encrypted_disk_file.read_bytes() == TINY_PNG:
            raise AssertionError("Test 4 (Encrypted upload): файл на диске сохранился без шифрования")
        print("Test 4 (Encrypted upload): файл сохранён на диске в зашифрованном виде -> OK")

        foreign_download = requests.get(
            f"{BASE_URL}/files/{encrypted_file_id}/download",
            headers=bob_headers,
            timeout=10,
        )
        assert_status("Test 5 (Download IDOR)", foreign_download, 404)
        print("Test 5 (Download IDOR): Боб не скачал файл Алисы -> OK")

        own_plain_download = requests.get(
            f"{BASE_URL}/files/{plain_file_id}/download",
            headers=alice_headers,
            timeout=10,
        )
        assert_status("Test 6 (Plain download)", own_plain_download, 200)
        if own_plain_download.content != TINY_PNG:
            raise AssertionError("Test 6 (Plain download): скачанный обычный файл не совпадает с исходным")
        print("Test 6 (Plain download): обычный файл скачан без изменений -> OK")

        own_encrypted_download = requests.get(
            f"{BASE_URL}/files/{encrypted_file_id}/download",
            headers=alice_headers,
            timeout=10,
        )
        assert_status("Test 7 (Encrypted download)", own_encrypted_download, 200)
        content_disposition = own_encrypted_download.headers.get("Content-Disposition", "")
        if "attachment" not in content_disposition or "salary.png" not in content_disposition:
            raise AssertionError(
                "Test 7 (Encrypted download): заголовок Content-Disposition не заставляет скачивать файл"
            )
        if own_encrypted_download.content != TINY_PNG:
            raise AssertionError(
                "Test 7 (Encrypted download): после расшифровки пользователь получил неверное содержимое"
            )
        print("Test 7 (Encrypted download): зашифрованный файл расшифрован и скачан корректно -> OK")

        print("Все проверки безопасности успешно пройдены.")
    finally:
        server.terminate()
        try:
            server.wait(timeout=5)
        except subprocess.TimeoutExpired:
            server.kill()
            server.wait(timeout=5)


if __name__ == "__main__":
    main()
