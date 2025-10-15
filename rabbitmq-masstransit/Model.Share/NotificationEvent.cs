namespace Model.Share
{
    public record NotificationEvent 
    {
        public Guid Id { get; init; }
        public int Type { get; set; } // 1 = Email, 2 = SMS
        public string Message { get; set; }
    }
}
