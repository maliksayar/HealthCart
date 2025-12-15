using System;
using HealthCart.Models.DomainModels;
using HealthCart.Models.JunctionModels;

namespace HealthCart.Models.ViewModels;

public class HybridViewModel
{

    public Cart? Cart {get; set;}
    public  List<CartItem> CartItems {get; set;} =[];


   
    public  List<Order> Orders {get; set;} =[];
    public  List<OrderItem> OrderItems {get; set;} =[];    


    public Order? Order {get; set;}

    public Address? Address {get; set;}


}