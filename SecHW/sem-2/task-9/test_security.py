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

        valid_upload = requests.post(
            f"{BASE_URL}/files/upload",
            headers=alice_headers,
            files={"uploaded_file": ("cat.png", TINY_PNG, "image/png")},
            timeout=10,
        )
        assert_status("Test 3 (Upload)", valid_upload, 201)
        uploaded_file_id = valid_upload.json()["файл"]["id"]
        print("Test 3 (Upload): корректный PNG загружен -> OK")

        foreign_download = requests.get(
            f"{BASE_URL}/files/{uploaded_file_id}/download",
            headers=bob_headers,
            timeout=10,
        )
        assert_status("Test 4 (Download IDOR)", foreign_download, 404)
        print("Test 4 (Download IDOR): Боб не скачал файл Алисы -> OK")

        own_download = requests.get(
            f"{BASE_URL}/files/{uploaded_file_id}/download",
            headers=alice_headers,
            timeout=10,
        )
        assert_status("Test 5 (Download)", own_download, 200)
        content_disposition = own_download.headers.get("Content-Disposition", "")
        if "attachment" not in content_disposition or "cat.png" not in content_disposition:
            raise AssertionError(
                "Test 5 (Download): заголовок Content-Disposition не заставляет скачивать файл"
            )
        if own_download.content != TINY_PNG:
            raise AssertionError("Test 5 (Download): скачанный файл не совпадает с загруженным")
        print("Test 5 (Download): Алиса скачала свой файл с attachment-заголовком -> OK")

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
