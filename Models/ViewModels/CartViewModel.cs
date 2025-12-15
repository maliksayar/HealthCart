using System;
using HealthCart.Models.DomainModels;
using HealthCart.Models.JunctionModels;

namespace HealthCart.Models.ViewModels;

public class CartViewModel
{

public List<CartItem> CartItems { get; set; } = []  ;
public Cart? Cart { get; set; }

}