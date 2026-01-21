namespace CoronelExpress.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public DateTime SubscribedOn { get; set; }
    }
}