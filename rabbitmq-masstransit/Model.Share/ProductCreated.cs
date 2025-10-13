namespace Model.Share
{
    public record ProductCreated : IMessage
    {
        public Guid ProductId { get; init; }
        public string Name { get; init; } = default!;
        public decimal Price { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
