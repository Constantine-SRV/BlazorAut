resource "azurerm_linux_virtual_machine" "vm_10_6" {
  name                  = "vm-10-6"
  resource_group_name   = azurerm_resource_group.rg-azweb.name
  location              = azurerm_resource_group.rg-azweb.location
  size                  = "Standard_B2ts_v2"
  admin_username        = "ubuntu"
  network_interface_ids = [azurerm_network_interface.vm_10_6.id]
  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }
  source_image_reference {
    publisher = "canonical"
    offer     = "0001-com-ubuntu-server-focal"
    sku       = "20_04-lts-gen2"
    version   = "latest"
  }
   
 admin_ssh_key {
    username   = "ubuntu"
    public_key = var.ssh_public_key  
  }
#   depends_on = [azurerm_postgresql_flexible_server.pg_db]
}

resource "azurerm_linux_virtual_machine" "vm_20_7" {
  name                  = "vm-20-7"
  resource_group_name   = azurerm_resource_group.rg-azweb.name
  location              = azurerm_resource_group.rg-azweb.location
  size                  = "Standard_B2ts_v2"
  admin_username        = "ubuntu"
  network_interface_ids = [azurerm_network_interface.vm_20_7.id]
  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }
  source_image_reference {
    publisher = "canonical"
    offer     = "0001-com-ubuntu-server-focal"
    sku       = "20_04-lts-gen2"
    version   = "latest"
  }
   
 admin_ssh_key {
    username   = "ubuntu"
    public_key = var.ssh_public_key  
  }
 # Provisioners
  provisioner "file" {
    source      = "setup_instance.sh"
    destination = "/tmp/setup_instance.sh"
    connection {
      type        = "ssh"
      host        = self.public_ip_address
      user        = "ubuntu"
      private_key = file("${path.module}/az_ssh_key.pem")
    }
  }

  provisioner "file" {
    source      = "restore_pg_dump.sh"
    destination = "/tmp/restore_pg_dump.sh"
    connection {
      type        = "ssh"
      host        = self.public_ip_address
      user        = "ubuntu"
      private_key = file("${path.module}/az_ssh_key.pem")
    }
  }
  provisioner "remote-exec" {
    inline = [
      "sudo mv /tmp/setup_instance.sh /usr/local/bin/setup_instance.sh"
      ,"sudo chmod +x /usr/local/bin/setup_instance.sh"
       , "export PFX_FILE_NAME='webaws_pam4_com.pfx'"
      ,"export APP_NAME='BlazorAut'"
        ,"export DB_USER='dbuser'"
        ,"export DB_PASS=${var.db_password}"
        ,"export DB_NAME='dbwebaws'"
        ,"export ACC_KEY=${var.arm_access_key}"
        ,"sudo mv /tmp/restore_pg_dump.sh /usr/local/bin/restore_pg_dump.sh"
        ,"sudo chmod +x /usr/local/bin/restore_pg_dump.sh"
        ,"sudo -E /usr/local/bin/restore_pg_dump.sh"
       ,"sudo -E /usr/local/bin/setup_instance.sh"
    ]
    connection {
      type        = "ssh"
      host        = self.public_ip_address
      user        = "ubuntu"
      private_key = file("${path.module}/az_ssh_key.pem")
    }
  }


  depends_on = [azurerm_postgresql_flexible_server.pg_db]
}
