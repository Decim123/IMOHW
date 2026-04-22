# Security Report 06

Проект:
- `sem-2/task-6`
- `http://82.114.226.145:8002/comments`

Что было уязвимо:
- комментарии выводились без экранирования через `safe`
- это позволяло выполнить XSS через payload вида `<img src=x onerror=alert('Hacked')>`

Что исправлено:
- добавлена санитизация через `bleach`
- разрешены только теги `b`, `i`, `u`, `em`, `strong`
- добавлен заголовок `Content-Security-Policy`

Функция-санитайзер:

```python
import re

def sanitize_comment(text: str) -> str:
    without_scripts = re.sub(
        r"<\s*(script|style)\b[^>]*>.*?<\s*/\s*\1\s*>",
        "",
        text,
        flags=re.IGNORECASE | re.DOTALL,
    )
    return bleach.clean(
        without_scripts,
        tags=["b", "i", "u", "em", "strong"],
        attributes={},
        strip=True,
    ).strip()
```

Проверочный payload:

```html
<b>Важное</b> <script>alert(1)</script>
```

Ожидаемый результат:
- на странице остается только `Важное` с жирным форматированием
- `script` удаляется
- браузер получает CSP: `default-src 'self'; script-src 'self'; style-src 'self'`

Что приложить в отчет:
- скрин XSS до защиты
- скрин заголовков ответа с CSP
- скрин консоли браузера с блокировкой инлайн-скрипта
