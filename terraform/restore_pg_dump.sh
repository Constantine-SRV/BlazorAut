#!/bin/bash

DB_HOST=${DB_HOST:-"pgaz.pam4.com"}
DB_USER=${DB_USER:-"dbuser"}
DB_PASS=${DB_PASS:-"password"}
DB_NAME=${DB_NAME:-"dbwebaws"}
ST_ACCOUNT=${ST_ACCOUNT:-"constantine2zu"}
ACC_KEY=${ACC_KEY:-"XXX"}


echo "!-apt-get update"
#sudo apt-get update
#sudo apt-get install -y postgresql-client
sudo sh -c 'echo "deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -
sudo apt-get update
sudo apt-get install -y postgresql-client-16

echo "!- Setup az-cli"
sudo apt install azure-cli -y


echo "!-Downloading database dump from S3..."
az storage blob download --container-name web --name dbwebaws_backup.dump --file ~/dbwebaws_backup.dump --account-name constantine2zu --auth-mode key --account-key ${ACC_KEY}

echo "!-Pg_Dump arguments Length of DB_PASS: ${#DB_PASS}, First three characters: ${DB_PASS:0:3}"
#PGPASSWORD=$DB_PASS
echo "!-Creating the database $DB_NAME..."
PGPASSWORD=$DB_PASS psql "sslmode=require host=$DB_HOST port=5432 user=$DB_USER dbname=postgres" -c "CREATE DATABASE $DB_NAME;"


echo "!-Restoring database from dump..."
PGPASSWORD=$DB_PASS pg_restore -h $DB_HOST -U $DB_USER -d $DB_NAME -v ~/dbwebaws_backup.dump || echo "Failed to restore database"


echo "1-Database restoration complete."
