def retry3(func):
    def wrapper(*args, **kwargs):
        last_exc = None
        for _ in range(3):
            try:
                return func(*args, **kwargs)
            except Exception as e:
                last_exc = e
        raise last_exc
    return wrapper

i = 0

@retry3
def flaky():
    global i
    i += 1
    if i < 3:
        raise ValueError("fail")
    return "success"

print(flaky())