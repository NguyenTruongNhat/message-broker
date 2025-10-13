using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Share
{
    public record UpdateProduct
    {
        public Guid ProductId { get; init; }
        public string Name { get; init; } = default!;
        public decimal Price { get; init; }
    }

    public record ProductUpdated2
    {
        public Guid ProductId { get; init; }
        public string Name { get; init; } = default!;
        public decimal Price { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
