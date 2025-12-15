using HealthCart.Models.DomainModels;

namespace HealthCart.Models.ViewModels
{
    public class ProductViewModel
    {
        public List<Product> Products { get; set; } = [];

        public Product? Product { get; set; }
        public User ? User {get ;set;}

    }
}