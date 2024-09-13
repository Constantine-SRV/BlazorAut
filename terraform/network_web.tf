
#for ubuntu, PG and LB
resource "azurerm_resource_group" "rg-azweb" {
  name     = "azweb-resources"
  location = "northeurope"
  provisioner "local-exec" {
    command = "find $(pwd) -name az_ssh_key.pem"
  }
}


# ubuntu


resource "azurerm_virtual_network" "vnet" {
  name                = "vnet-0_0"
  address_space       = ["10.10.0.0/16"]
  location            = azurerm_resource_group.rg-azweb.location
  resource_group_name = azurerm_resource_group.rg-azweb.name
}

resource "azurerm_subnet" "subnet_10_0" {
  name                 = "subnet-10_0"
  resource_group_name  = azurerm_resource_group.rg-azweb.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.10.10.0/24"]
}

resource "azurerm_subnet" "subnet_20_0" {
  name                 = "subnet-20_0"
  resource_group_name  = azurerm_resource_group.rg-azweb.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.10.20.0/24"]
}

resource "azurerm_network_interface" "vm_10_6" {
  name                = "nic-vm-10_6"
  location            = azurerm_resource_group.rg-azweb.location
  resource_group_name = azurerm_resource_group.rg-azweb.name

  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.subnet_10_0.id
    private_ip_address_allocation = "Static"
    private_ip_address            = "10.10.10.6"
    public_ip_address_id          = azurerm_public_ip.vm_10_6.id
  }
}

resource "azurerm_network_interface" "vm_20_7" {
  name                = "nic-vm-20_7"
  location            = azurerm_resource_group.rg-azweb.location
  resource_group_name = azurerm_resource_group.rg-azweb.name

  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.subnet_20_0.id
    private_ip_address_allocation = "Static"
    private_ip_address            = "10.10.20.7"
    public_ip_address_id          = azurerm_public_ip.vm_20_7.id
  }
}

resource "azurerm_public_ip" "vm_10_6" {
  name                = "pip-vm-10_6"
  location            = azurerm_resource_group.rg-azweb.location
  resource_group_name = azurerm_resource_group.rg-azweb.name
  allocation_method   = "Dynamic"
  domain_name_label   = "webaz06"
}

resource "azurerm_public_ip" "vm_20_7" {
  name                = "pip-vm-20_7"
  location            = azurerm_resource_group.rg-azweb.location
  resource_group_name = azurerm_resource_group.rg-azweb.name
  allocation_method   = "Dynamic"
  domain_name_label   = "webaz07"
}