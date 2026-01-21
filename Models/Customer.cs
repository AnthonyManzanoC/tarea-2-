namespace CoronelExpress.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

        // NUEVO: Campo para almacenar el RUC o Cédula del cliente
        public string RUC { get; set; }
        public ICollection<TermsAcceptance> TermsAcceptances { get; set; }

        public ICollection<Order> Orders { get; set; }
    }
}