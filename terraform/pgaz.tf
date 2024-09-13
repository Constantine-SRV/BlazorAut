


resource "azurerm_postgresql_flexible_server" "pg_db" {
  name                   = "pgdbwebaz"
  location              = azurerm_resource_group.rg-azweb.location
  resource_group_name   = azurerm_resource_group.rg-azweb.name
  version                = "16"
  administrator_login    = "dbuser"
  administrator_password =var.db_password

  #sku_name = "GP_Standard_D2s_v3" 
  #sku_name  = "Standard_B2ms" #wrong
  sku_name = "B_Standard_B1ms"


  storage_mb                  = 32768  # 32 GB
  backup_retention_days       = 7
  geo_redundant_backup_enabled = false
  auto_grow_enabled           = false
  public_network_access_enabled = true

  tags = {
    environment = "production"
  }
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure_ips" {
  name              = "AllowAllAzureServicesAndResourcesWithinAzureIps_2024-5-31_20-32-3"
  server_id         = azurerm_postgresql_flexible_server.pg_db.id
  start_ip_address  = "0.0.0.0"
  end_ip_address    = "0.0.0.0"
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "client_ip" {
  name              = "ClientIPAddress_2024-5-31_20-29-50"
  server_id         = azurerm_postgresql_flexible_server.pg_db.id
  start_ip_address  = "213.149.160.0"
  end_ip_address    = "213.149.170.254"
}
