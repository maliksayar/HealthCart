using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthCart.Models.DomainModels;

public class Review
{

    public Guid ReviewId { get; set; } = Guid.NewGuid();
    public required int Stars { get; set; }
    public required string ReviewText { get; set; }


    public required Guid ProductId { get; set; } //FK
    [ForeignKey("ProductId")]
    public Product? Product { get; set; } // naviagtion property 



    public required Guid UserId { get; set; } //FK
    [ForeignKey("UserId")]
    public User? User { get; set; } // naviagtion property 



    public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public required DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


}