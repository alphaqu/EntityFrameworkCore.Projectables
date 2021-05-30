﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityFrameworkCore.Projections;

namespace ReadmeSample.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedDate { get; set; }

        public decimal TaxRate { get; set; }

        public User User { get; set; }
        public ICollection<OrderItem> Items { get; set; }

        [Projectable] public decimal Subtotal => Items.Sum(item => item.Product.ListPrice * item.Quantity);
        [Projectable] public decimal Tax => Subtotal * TaxRate;
        [Projectable] public decimal GrandTotal => Subtotal + Tax;
    }
}
