#access key in yaml
terraform {
    backend "azurerm" {
    storage_account_name = "constantine2zu"
    container_name       = "tf4config"
    key                  = "terraform3.tfstate"

  }
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}



provider "azurerm" {
  features {}
}



