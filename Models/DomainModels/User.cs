using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using HealthCart.Models.DomainModels;
using HealthCart.Types;
using Microsoft.EntityFrameworkCore.Metadata.Internal;


namespace HealthCart.Models;

public class User
{

    [Key]
    public  Guid UserId {get;set;} = Guid.NewGuid();
    public required string  Username {get ;set;}
    public required string  Email {get ;set;}
    public required string  Password {get ;set;}
    public string?  ProfilePicUrl {get ;set;}
    public string? Phone {get; set ; }

    public string ? ResetPassToken {get; set;}

    public DateTime? ResetPassTokenExpiry {get; set;}
    public Role Role {get;set;} = Role.User;
    public Address? Address { get; set; }
    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];


    
   
    public DateTime DateCreated { get; set; }  = DateTime.UtcNow;
    public DateTime? DateModified { get; set; } = DateTime.UtcNow;
 


}