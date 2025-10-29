import time
from collections import deque

def rate_limit(calls, per_seconds):
    def decorator(func):
        last_calls = deque()

        def wrapper(*args, **kwargs):
            now = time.time()

            while last_calls and now - last_calls[0] > per_seconds:
                last_calls.popleft()

            if len(last_calls) >= calls:
                raise RuntimeError("Превышен лимит вызовов функции")

            last_calls.append(now)
            return func(*args, **kwargs)

        return wrapper
    return decorator

@rate_limit(calls=2, per_seconds=1.0)
def ping():
    return "pong"

print(ping())  # ок
print(ping())  # ок
print(ping())  # RuntimeError