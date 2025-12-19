using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("CustomerCarts")]
public class CustomerCart
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid CustomerId { get; set; }
    
    public Guid EstablishmentId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
    
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }
    
    public virtual ICollection<CustomerCartItem> Items { get; set; } = new List<CustomerCartItem>();
    
    // Computed
    [NotMapped]
    public int TotalItems => Items?.Sum(i => i.Quantity) ?? 0;
    
    [NotMapped]
    public decimal TotalValue => Items?.Sum(i => i.TotalPrice) ?? 0;
}

[Table("CustomerCartItems")]
public class CustomerCartItem
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid CartId { get; set; }
    
    public Guid ProductId { get; set; }
    
    public int Quantity { get; set; } = 1;
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    [ForeignKey("CartId")]
    public virtual CustomerCart? Cart { get; set; }
    
    [ForeignKey("ProductId")]
    public virtual CatalogProduct? Product { get; set; }
    
    // Computed
    [NotMapped]
    public decimal TotalPrice => UnitPrice * Quantity;
}
