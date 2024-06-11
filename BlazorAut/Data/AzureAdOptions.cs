using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorAut.Data
{
   // [Table("azure_ad_options")]
    public class AzureAdOptions
    {
        public int Id { get; set; }
        public string Instance { get; set; }
        public string Domain { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string SenderUserId { get; set; }
    }
}
