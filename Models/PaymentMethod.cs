namespace CoronelExpress.Models
{
    public class PaymentMethod
    {
        public int Id { get; set; }
        public string Method { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}