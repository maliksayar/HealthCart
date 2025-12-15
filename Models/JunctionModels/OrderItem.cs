using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HealthCart.Models.DomainModels;
using HealthCart.Types;

namespace HealthCart.Models.JunctionModels;

public class OrderItem
{

    [Key]

    public Guid OrderItemId {get;set;} =Guid.NewGuid();
    

    public  Guid OrderId { get; set; }  // FK to Order
    [ForeignKey("OrderId")] // Foreign key to Order
    public Order? Order { get; set; }  // Navigation property to Order



    public  Guid ProductId { get; set; }  // FK to Product
    [ForeignKey("ProductId")] // Foreign key to Product
    public Product? Product { get; set; }  // Navigation property to Product



    public required int Quantity { get; set; } 

}