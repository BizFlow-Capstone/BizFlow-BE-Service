#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/backups"
FILENAME="bizflow_db_${DATE}.sql"

echo "Starting backup at ${DATE}..."

mysqldump -h${MYSQL_HOST} \
  -u${MYSQL_USER} \
  -p${MYSQL_PASSWORD} \
  ${MYSQL_DATABASE} > ${BACKUP_DIR}/${FILENAME}

# Compress backup
gzip ${BACKUP_DIR}/${FILENAME}

# Delete backups older than 7 days
find ${BACKUP_DIR} -name "*.sql.gz" -mtime +7 -delete

echo "Backup completed: ${FILENAME}.gz"