#!/usr/bin/env bash
# JJDevHub — daily backup of PostgreSQL + MongoDB
# Usage:  ./backup.sh              (local only)
#         ./backup.sh --upload     (local + upload to Cloudflare R2 / S3)
#
# Recommended cron (run daily at 03:00):
#   0 3 * * * /opt/jjdevhub/infra/docker/backup.sh --upload >> /var/log/jjdevhub-backup.log 2>&1

set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/data/backups}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
UPLOAD="${1:-}"

PG_CONTAINER="jjdevhub-db"
PG_USER="${DB_USER:-postgres}"
PG_DB="jjdevhub_content"

MONGO_CONTAINER="jjdevhub-mongo"
MONGO_DB="jjdevhub_content_read"

mkdir -p "$BACKUP_DIR"

echo "=== JJDevHub backup started at $(date -Iseconds) ==="

# ── PostgreSQL ───────────────────────────────────────────────────
PG_FILE="$BACKUP_DIR/pg_${PG_DB}_${TIMESTAMP}.sql.gz"
echo "[pg_dump] Backing up $PG_DB ..."
docker exec "$PG_CONTAINER" pg_dump -U "$PG_USER" "$PG_DB" | gzip > "$PG_FILE"
echo "[pg_dump] Done: $PG_FILE ($(du -h "$PG_FILE" | cut -f1))"

# ── MongoDB ──────────────────────────────────────────────────────
MONGO_DIR="$BACKUP_DIR/mongo_${MONGO_DB}_${TIMESTAMP}"
MONGO_FILE="${MONGO_DIR}.tar.gz"
echo "[mongodump] Backing up $MONGO_DB ..."
docker exec "$MONGO_CONTAINER" mongodump --db "$MONGO_DB" --out /tmp/mongodump --quiet
docker cp "$MONGO_CONTAINER:/tmp/mongodump/$MONGO_DB" "$MONGO_DIR"
docker exec "$MONGO_CONTAINER" rm -rf /tmp/mongodump
tar -czf "$MONGO_FILE" -C "$BACKUP_DIR" "$(basename "$MONGO_DIR")"
rm -rf "$MONGO_DIR"
echo "[mongodump] Done: $MONGO_FILE ($(du -h "$MONGO_FILE" | cut -f1))"

# ── Cleanup old backups ─────────────────────────────────────────
echo "[cleanup] Removing backups older than $RETENTION_DAYS days ..."
find "$BACKUP_DIR" -name "pg_*.sql.gz" -mtime +"$RETENTION_DAYS" -delete -print
find "$BACKUP_DIR" -name "mongo_*.tar.gz" -mtime +"$RETENTION_DAYS" -delete -print

# ── Optional: upload to S3-compatible storage ────────────────────
if [ "$UPLOAD" = "--upload" ]; then
    if command -v rclone &> /dev/null; then
        RCLONE_REMOTE="${RCLONE_REMOTE:-r2:jjdevhub-backups}"
        echo "[upload] Uploading to $RCLONE_REMOTE ..."
        rclone copy "$PG_FILE" "$RCLONE_REMOTE/postgres/"
        rclone copy "$MONGO_FILE" "$RCLONE_REMOTE/mongodb/"
        echo "[upload] Done."
    else
        echo "[upload] rclone not installed — skipping upload."
        echo "         Install: https://rclone.org/install/"
        echo "         Configure: rclone config (Cloudflare R2 / Backblaze B2 / AWS S3)"
    fi
fi

echo "=== Backup finished at $(date -Iseconds) ==="
