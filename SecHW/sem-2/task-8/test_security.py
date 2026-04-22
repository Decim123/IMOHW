from __future__ import annotations

import subprocess
import sys
import time
from pathlib import Path

import requests


PROJECT_ROOT = Path(__file__).resolve().parent
BASE_URL = "http://127.0.0.1:8010"


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
        admin_headers = {"X-User-Id": "3"}

        response_1 = requests.get(f"{BASE_URL}/files/2", headers=alice_headers, timeout=5)
        assert_status("Test 1 (IDOR)", response_1, 404)
        print("Test 1 (IDOR): пользователь Алиса не получил файл Боба -> OK")

        response_2 = requests.get(f"{BASE_URL}/files/1", headers=alice_headers, timeout=5)
        assert_status("Test 2 (Access)", response_2, 200)
        print("Test 2 (Access): пользователь Алиса получил свой файл -> OK")

        response_3 = requests.delete(f"{BASE_URL}/files/2", headers=admin_headers, timeout=5)
        assert_status("Test 3 (Admin delete)", response_3, 200)

        response_4 = requests.get(f"{BASE_URL}/files/all", headers=admin_headers, timeout=5)
        assert_status("Test 3 (Admin verify)", response_4, 200)
        all_files = response_4.json()["файлы"]
        if any(file_item["id"] == 2 for file_item in all_files):
            raise AssertionError("Test 3 (Admin): файл Боба не исчез после удаления")
        print("Test 3 (Admin): администратор удалил файл Боба, файл исчез -> OK")

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
