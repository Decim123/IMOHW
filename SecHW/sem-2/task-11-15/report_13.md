# Report 13

## Repository

- Repository URL: `<вставьте ссылку на репозиторий>`
- Task directory: `SecHW/sem-2/task-11-13`

## What Was Changed

### Logging

- Added application logger writing to `logs/app.log`
- Log format includes timestamp, level, and message
- Replaced `print()` calls in the test script with `logger.info()`

### Error Handling

- Added global exception middleware for unhandled errors
- Full traceback is written to `logs/app.log` with level `ERROR`
- Users receive safe JSON response:

```json
{"detail": "We are sorry, something went wrong."}
```

- Added test endpoint: `/cause_error`

### Security Audit

- Added `WARNING` logs for failed login attempts
- Added `WARNING` logs for чужой доступ к файлам (IDOR attempts)
- Added minimal `/login` endpoint for testing wrong password attempts

### Docker / Deploy

- Added volume `./logs:/app/logs` in `docker-compose.yml`
- Added `logs/` directory to the project

## Screenshots

### 1. Safe JSON response from `/cause_error`

![cause_error json](./screenshots/cause_error-json.png)

### 2. `logs/app.log` on server

Must include:
- traceback from `/cause_error`
- failed login attempt log entry

![app log on server](./screenshots/app-log-server.png)

### 3. `docker-compose.yml` with logs volume

![compose logs volume](./screenshots/docker-compose-logs-volume.png)

## Server Commands Used

```bash
git pull
docker-compose up -d --build
curl http://<host>:<port>/cause_error
curl -X POST -F "username=admin" -F "password=wrongpass" http://<host>:<port>/login
cat logs/app.log
```

## Notes

- The application now hides internal stack traces from users
- Technical details are available only in `logs/app.log`
- Log volume is mounted from host to container via `./logs:/app/logs`
