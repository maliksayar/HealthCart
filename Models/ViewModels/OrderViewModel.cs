using System;
using HealthCart.Models.DomainModels;

namespace HealthCart.Models.ViewModels;

public class OrderViewModel
{


 public List<Order> Orders { get; set; }= [];

 public Order ? Order { get; set; } 


}