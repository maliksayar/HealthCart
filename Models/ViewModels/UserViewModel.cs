using System;

namespace HealthCart.Models.ViewModels;

public class UserViewModel
{
 public List<User> Users { get; set; }= [];

 public User ? User { get; set; } 
}