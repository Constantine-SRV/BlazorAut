using System.ComponentModel.DataAnnotations;

namespace BlazorAut.Data
{
    public class PinCode
    {
        [Key]
        public string PinName { get; set; }
        public string Pin { get; set; }
    }
}
