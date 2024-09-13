variable "win_password" {
  type        = string
  description = "password for windows"
   default = "ChangeAA5!_bib"
}
variable "win_user" {
  type        = string
  description = "user for windows"
   default = "user101"
}
variable "dns_comp_name" {
  type        = string
  description = "dns"
   default = "comp301"
}
variable "hetzner_dns_key" {
  type        = string
  description = "Hetzner API Secret Key"
   default = "0000"
}
variable "ssh_public_key" {
  type        = string
  description = "SSH"
   default = ""
}

variable "ssh_private_key" {
  type        = string
  description = "SSH private key for VM access"
  default     = ""
}
# for postgres
variable "db_password" {
  type        = string
  description = "password for Postgres"
   default = "0000"
}
#ARM_ACCESS_KEY
variable "arm_access_key" {
  type        = string
  description = "az storage key"
  default     = ""
}

