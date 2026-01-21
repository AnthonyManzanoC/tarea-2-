namespace CoronelExpress.Models
{
    public class Promotion
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime GeneratedOn { get; set; }
        public int SubscriptionId { get; set; }
        public string Prize { get; set; }
        public Subscription Subscription { get; set; } // Add this property
    }
}
