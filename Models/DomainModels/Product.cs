using System;
using System.ComponentModel.DataAnnotations;
using HealthCart.Models.JunctionModels;
using HealthCart.Types;

namespace HealthCart.Models.DomainModels;

public class Product
{

    [Key]
    public required Guid ProductId { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? ImageUrl { get; set; }
    public required decimal Price { get; set; }
    public required decimal Discount { get; set; }
    public required string ReasonForDiscount { get; set; }
    public required int Stock { get; set; }
    public int Rating { get; set; } = 0;
    public int? Sold { get; set; } = 0;
    public required ProductCategory Category { get; set; } = ProductCategory.All;
    public required string SubCategory { get; set; }
    public required string Brand { get; set; }
    public required string Color { get; set; }
    public required string Size { get; set; }

    public ICollection<Review> Reviews { get; set; } = [];

    public required bool IsDeleted { get; set; } = false;
    public required bool IsActive { get; set; } = true;
    public ICollection<CartItem> CartItems { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public required DateTime UpdatedAt { get; set; } = DateTime.UtcNow;



}