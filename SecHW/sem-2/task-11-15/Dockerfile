FROM python:3.10.16-slim-bookworm

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /app

COPY requirements.txt ./

RUN apt-get update \
    && apt-get upgrade -y \
    && pip install --no-cache-dir --upgrade pip setuptools wheel \
    && pip install --no-cache-dir -r requirements.txt \
    && useradd --create-home --uid 1000 --shell /bin/bash appuser \
    && mkdir -p /app/storage /app/logs \
    && chown -R appuser:appuser /app \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

COPY src ./src
COPY templates ./templates
COPY .env.example ./.env.example

RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8000

CMD ["uvicorn", "src.main:app", "--host", "0.0.0.0", "--port", "8000"]
