# File Manager

## Описание проекта

сервис на FastAPI для безопасной загрузки, хранения и скачивания файлов.

В проекте реализованы:

- регистрация и вход пользователей;
- защита браузерной части через `Session`;
- защита API через `JWT Bearer Token`;
- хэширование паролей через `passlib` и `bcrypt`;
- разграничение доступа к файлам и защита от IDOR;
- проверка типа файла по содержимому (`magic bytes`);
- переименование файлов в `UUID`;
- опциональное шифрование файлов через `Fernet`;
- логирование в файл `logs/app.log`;
- проверки безопасности через `Bandit`, `pre-commit`, GitHub Actions и ZAP.

Важно: в этой версии проекта метаданные пользователей и файлов хранятся в JSON-файлах внутри volume `storage`. Postgres в текущей реализации не используется.

## Технологии

- Python 3.10
- FastAPI
- Uvicorn
- Jinja2
- Docker / Docker Compose
- Passlib + bcrypt
- JWT (`PyJWT`)
- Fernet (`cryptography`)
- Bandit
- OWASP ZAP Baseline Scan

## Структура проекта

Финальный проект находится в каталоге:

```bash
SecHW/sem-2/task-11-15
```

## Запуск проекта

### 1. Клонирование репозитория

```bash
git clone https://github.com/Decim123/IMOHW
cd IMOHW/SecHW/sem-2/task-11-15
```

### 2. Подготовка окружения

```bash
cp .env.example .env
```

На Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

### 3. Запуск одной командой

```bash
docker compose up -d --build
```

После запуска сервис будет доступен по адресу:

```text
http://localhost:8000
```

Swagger-документация:

```text
http://localhost:8000/docs
```

## Тестовые учетные записи

Если используется `.env.example`, доступны тестовые учетные записи:

- `alice` / `alice12345`
- `bob` / `bob12345`
- `admin` / `admin12345`

можно зарегистрировать нового пользователя через страницу:

```text
http://localhost:8000/register
```

## Аутентификация

### Браузер

Для браузера используется `Session`:

- вход через `/login`;
- после успешного входа создается cookie-сессия;
- без входа домашняя страница и UI-операции недоступны.

### API

Для API используется `JWT`.

Получение токена:

```bash
curl -X POST \
  -H "Accept: application/json" \
  -F "username=alice" \
  -F "password=alice12345" \
  http://localhost:8000/login
```

Ответ содержит `access_token`.

Пример запроса к API:

```bash
curl -H "Authorization: Bearer <access_token>" http://localhost:8000/files/my
```

## Работа с файлами

Разрешены только:

- `JPEG`
- `PNG`
- `PDF`

Ограничения и защита:

- тип файла проверяется по содержимому, а не только по расширению;
- имя файла на диске заменяется на `UUID`;
- максимальный размер файла — `2 МБ`;
- чужие файлы недоступны;
- при попытке доступа к чужому файлу возвращается `404`.

## Основные маршруты

### UI

- `GET /login` — страница входа
- `GET /register` — страница регистрации
- `GET /` — домашняя страница после входа
- `POST /logout` — выход из системы

### API

- `POST /login` — вход и получение JWT
- `POST /register` — регистрация нового пользователя
- `GET /files/my` — список своих файлов
- `GET /files/all` — список всех файлов только для администратора
- `POST /files/upload?encrypt=true` — загрузка файла с опциональным шифрованием
- `GET /files/{file_id}` — метаданные файла
- `GET /files/{file_id}/download` — скачивание файла
- `DELETE /files/{file_id}` — удаление файла
- `GET /cause_error` — тестовый маршрут для проверки глобального обработчика ошибок

## Volume и сохранность данных

В `docker-compose.yml` подключены volumes:

- `./storage:/app/storage`
- `./logs:/app/logs`

Это значит:

- загруженные файлы сохраняются между перезапусками контейнера;
- метаданные пользователей и файлов сохраняются между перезапусками;
- лог-файл `logs/app.log` сохраняется на хосте.

## Проверки качества и безопасности

### Локально

Запуск Bandit:

```bash
bandit -c bandit.yaml -r .
```

Проверка pre-commit:

```bash
pre-commit run --all-files
```

Автотест безопасности:

```bash
python test_security.py
```

### CI

GitHub Actions workflow:

```text
.github/workflows/security.yml
```

При каждом `push` и `pull_request` запускается `Bandit`.

## Логи

Логи приложения пишутся в:

```text
logs/app.log
```

Туда попадают:

- информационные сообщения о запуске и загрузке файлов;
- неудачные попытки входа;
- попытки доступа к чужим файлам;
- traceback необработанных ошибок.

## Серверный запуск

Для серверного сценария можно использовать:

```text
docker-compose.host.yml
```
