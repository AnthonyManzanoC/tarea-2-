using System.ComponentModel.DataAnnotations;

namespace CoronelExpress.Models
{
    public class TermsAcceptance
    {
        [Key]

        public int Id { get; set; }
        public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
        public string UserIp { get; set; }

        // Relación con el cliente
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
    }
}
