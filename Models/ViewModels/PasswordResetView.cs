using System;

namespace HealthCart.Models.ViewModels;

public class PasswordResetView
{

 public required string Password { get; set; }

 public required string ConfirmPassword { get; set; }

}