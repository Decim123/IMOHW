from dotenv import load_dotenv
import os
import sys


def main() -> int:
    load_dotenv()
    secret = os.getenv("APP_SECRET")

    if not secret:
        print("Error: APP_SECRET is not set.")
        return 1

    print(f"System started. Secret hash: {secret[:3]}**")
    return 0


if __name__ == "__main__":
    sys.exit(main())
