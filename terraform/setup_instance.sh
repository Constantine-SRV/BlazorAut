#!/bin/bash

# Default settings for Azure
PFX_FILE_NAME=${PFX_FILE_NAME:-"webaws_pam4_com.pfx"}
DB_HOST=${DB_HOST:-"pgaz.pam4.com"}
AZURE_STORAGE_ACCOUNT="constantine2zu"
AZURE_CONTAINER_NAME="web"

# Add Microsoft repository for Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Update system and install necessary tools
sudo apt-get update
sudo apt-get install -y postgresql-client azure-cli jq

# Download PFX file from Azure Blob Storage
echo "!-Downloading PFX file from Azure..."
az storage blob download --container-name $AZURE_CONTAINER_NAME --name $PFX_FILE_NAME --file /etc/ssl/certs/webaws_pam4_com.pfx --account-name $AZURE_STORAGE_ACCOUNT --auth-mode key --account-key ${ACC_KEY}
sudo chmod 600 /etc/ssl/certs/webaws_pam4_com.pfx

# Download and setup the web application
sudo mkdir -p /var/www/BlazorAut
echo "!-Downloading web app archive..."
curl -L -o /var/www/BlazorAut/BlazorAut.tar.gz https://github.com/Constantine-SRV/BlazorAut/releases/download/latest_release/BlazorAut-linux.tar.gz
echo "!-Extracting web app archive..."
sudo tar -xf /var/www/BlazorAut/BlazorAut.tar.gz -C /var/www/BlazorAut
sudo chmod +x /var/www/BlazorAut/BlazorAut
sudo chmod -R 755 /var/www/BlazorAut/wwwroot/

# Configure app settings
APPSETTINGS_PATH="/var/www/BlazorAut/appsettings.json"
jq --arg db_host "$DB_HOST" \
   --arg db_user "$DB_USER" \
   --arg db_pass "$DB_PASS" \
   '.ConnectionStrings.DefaultConnection = ("Host=" + $db_host + ";Database=dbwebaws;Username=" + $db_user + ";Password=" + $db_pass)' \
   $APPSETTINGS_PATH > $APPSETTINGS_PATH.tmp && mv $APPSETTINGS_PATH.tmp $APPSETTINGS_PATH

# Setup service
echo "[Unit]
Description=BlazorAut Web App

[Service]
Environment=\"AZURE_STORAGE_KEY=${ACC_KEY}\"
WorkingDirectory=/var/www/BlazorAut
ExecStart=/var/www/BlazorAut/BlazorAut
Restart=always
RestartSec=10
SyslogIdentifier=BlazorAut

[Install]
WantedBy=multi-user.target" | sudo tee /etc/systemd/system/BlazorAut.service

sudo systemctl daemon-reload
sudo systemctl enable BlazorAut
sudo systemctl start BlazorAut
