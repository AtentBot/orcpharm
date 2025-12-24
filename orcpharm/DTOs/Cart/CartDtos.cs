using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs.Cart;

/// <summary>
/// DTO para adicionar produtos do catálogo ao carrinho
/// </summary>
public class AddProductToCartDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(1, 999)]
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// DTO para atualizar quantidade de item no carrinho
/// </summary>
public class UpdateCartItemDto
{
    [Required]
    public Guid ItemId { get; set; }

    [Required]
    [Range(-999, 999)]
    public int Delta { get; set; }
}

/// <summary>
/// DTO para remover item do carrinho
/// </summary>
public class RemoveCartItemDto
{
    [Required]
    public Guid ItemId { get; set; }
}

/// <summary>
/// DTO para criar pedido a partir do carrinho
/// </summary>
public class CreateOrderFromCartDto
{
    [MaxLength(1000)]
    public string? Notes { get; set; }
}
public class AddFormulaToCartDto
{
    [Required]
    public Guid ProductTypeId { get; set; }

    [Required]
    public Guid ProductSubTypeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProductTypeName { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string ProductSubTypeName { get; set; } = default!;

    [Required]
    [Range(0.001, 10000)]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = default!;

    [Required]
    [MaxLength(20)]
    public string CustomerPhone { get; set; } = default!;

    [MaxLength(200)]
    [EmailAddress]
    public string? CustomerEmail { get; set; }

    [MaxLength(2000)]
    public string? CustomerNotes { get; set; }

    [Required]
    public List<FormulaIngredientDto> Ingredients { get; set; } = new();

    [Required]
    [Range(0.01, 999999)]
    public decimal EstimatedPrice { get; set; }
}

/// <summary>
/// DTO para ingrediente da fórmula personalizada
/// </summary>
public class FormulaIngredientDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = default!;

    [Required]
    [Range(0.001, 10000)]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = default!;

    [MaxLength(500)]
    public string? Notes { get; set; }
}