FROM python:3.12-slim

WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY account_store.py api.py bot.py ./
COPY client ./client

ENV PYTHONUNBUFFERED=1
