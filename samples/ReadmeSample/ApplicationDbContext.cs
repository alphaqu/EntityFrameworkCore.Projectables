using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using EntityFrameworkCore.Projectables;
using EntityFrameworkCore.Projectables.Extensions;
using Microsoft.EntityFrameworkCore;
using ReadmeSample.Entities;

namespace ReadmeSample
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder
        )
        {
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=ReadmeSample;Trusted_Connection=True"
            );
            optionsBuilder.UseProjectables();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<OrderItem>()
                .HasKey(x => new { x.OrderId, x.ProductId });
        }

    }


    public class Item
    {
        public required int Id { get; set; }
        public string Name { get; set; }
        public List<string> Labels { get; set; }
    }

    public class SupplierProduct : Item
    {
        public string SupplierName { get; set; }
        public required int SupplierId { get; set; }
        public List<string> Tags { get; set; }

    }

    public class DbItem
    {
        public required int Id { get; set; }
        public string Name { get; set; }


        public string[] db_Labels { get; set; } = [];

        [Projectable]
        [NotMapped]
        public List<string> Labels
        {
            get => db_Labels.ToList();
            set => db_Labels = value.ToArray();
        }

        [Projectable]
        public Item ToModel() => new Item() {
            Id = Id, Name = Name, Labels = Labels,
        };

    }

    public class DbSupplierProduct : DbItem
    {
        public string SupplierName { get; set; }
        public required int SupplierId { get; set; }


        public string[] db_Tags { get; set; } = [];

        [Projectable]
        [NotMapped]
        public List<string> Tags
        {
            get => db_Tags.ToList();
            set => db_Tags = value.ToArray();
        }

        [Projectable]
        public new SupplierProduct ToModel() =>
            Projectable<SupplierProduct>.Join(
                base.ToModel(),
                new { Id, SupplierName, SupplierId, Tags }
            );

        [Projectable]
        public SupplierProduct ToModel2() =>
            Projectable<SupplierProduct>.Join(
                ToModel(),
                new { Id = 69 }
            );
    }

}