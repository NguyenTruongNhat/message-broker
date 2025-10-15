namespace Model.Share
{
    public class NotificationMessage
    {
        public int Type { get; set; } = 1; // 1 = Email, 2 = SMS
        public string Message { get; set; }
    }
}
