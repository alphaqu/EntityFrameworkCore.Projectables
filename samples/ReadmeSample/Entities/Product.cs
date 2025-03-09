using EntityFrameworkCore.Projectables;

namespace ReadmeSample.Entities
{
    public record Product
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal ListPrice { get; set; }

    }
}