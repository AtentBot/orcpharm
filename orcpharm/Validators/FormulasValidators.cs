using FluentValidation;
using DTOs.Formulas;

namespace Validators.Formulas;

public class CreateFormulaValidator : AbstractValidator<CreateFormulaDto>
{
    public CreateFormulaValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nome da fórmula é obrigatório")
            .MaximumLength(200)
            .WithMessage("Nome não pode exceder 200 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Descrição não pode exceder 500 caracteres");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Categoria é obrigatória")
            .MaximumLength(50)
            .WithMessage("Categoria não pode exceder 50 caracteres");

        RuleFor(x => x.PharmaceuticalForm)
            .NotEmpty()
            .WithMessage("Forma farmacêutica é obrigatória")
            .Must(form => new[] {
                "CAPSULA", "COMPRIMIDO", "SOLUCAO", "SUSPENSAO", "CREME",
                "POMADA", "GEL", "XAROPE", "PO", "SUPOSITORIO", "OVULO"
            }.Contains(form.ToUpper()))
            .WithMessage("Forma farmacêutica inválida");

        RuleFor(x => x.StandardYield)
            .GreaterThan(0)
            .WithMessage("Rendimento padrão deve ser maior que zero");

        RuleFor(x => x.ShelfLifeDays)
            .GreaterThan(0)
            .When(x => x.ShelfLifeDays.HasValue)
            .WithMessage("Prazo de validade deve ser maior que zero");

        RuleFor(x => x.PreparationInstructions)
            .MaximumLength(2000)
            .WithMessage("Instruções de preparo não podem exceder 2000 caracteres");

        RuleFor(x => x.StorageInstructions)
            .MaximumLength(1000)
            .WithMessage("Instruções de armazenamento não podem exceder 1000 caracteres");

        RuleFor(x => x.UsageInstructions)
            .MaximumLength(1000)
            .WithMessage("Instruções de uso não podem exceder 1000 caracteres");

        RuleFor(x => x.Components)
            .NotEmpty()
            .WithMessage("Fórmula deve ter pelo menos um componente");

        RuleForEach(x => x.Components)
            .SetValidator(new CreateFormulaComponentValidator());
    }
}

public class CreateFormulaComponentValidator : AbstractValidator<CreateFormulaComponentDto>
{
    public CreateFormulaComponentValidator()
    {
        RuleFor(x => x.RawMaterialId)
            .NotEqual(Guid.Empty)
            .WithMessage("Matéria-prima é obrigatória");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantidade deve ser maior que zero");

        RuleFor(x => x.Unit)
            .NotEmpty()
            .WithMessage("Unidade é obrigatória")
            .Must(unit => new[] { "g", "kg", "mg", "mL", "L", "UN", "%" }.Contains(unit))
            .WithMessage("Unidade inválida. Use: g, kg, mg, mL, L, UN ou %");

        RuleFor(x => x.ComponentType)
            .NotEmpty()
            .WithMessage("Tipo do componente é obrigatório")
            .Must(type => new[] {
                "ATIVO", "EXCIPIENTE", "VEICULO", "CONSERVANTE"
            }.Contains(type.ToUpper()))
            .WithMessage("Tipo de componente inválido");

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(500)
            .WithMessage("Instruções especiais não podem exceder 500 caracteres");
    }
}

public class UpdateFormulaValidator : AbstractValidator<UpdateFormulaDto>
{
    public UpdateFormulaValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nome da fórmula é obrigatório")
            .MaximumLength(200)
            .WithMessage("Nome não pode exceder 200 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Descrição não pode exceder 500 caracteres");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Categoria é obrigatória")
            .MaximumLength(50)
            .WithMessage("Categoria não pode exceder 50 caracteres");

        RuleFor(x => x.PharmaceuticalForm)
            .NotEmpty()
            .WithMessage("Forma farmacêutica é obrigatória")
            .Must(form => new[] {
                "CAPSULA", "COMPRIMIDO", "SOLUCAO", "SUSPENSAO", "CREME",
                "POMADA", "GEL", "XAROPE", "PO", "SUPOSITORIO", "OVULO"
            }.Contains(form.ToUpper()))
            .WithMessage("Forma farmacêutica inválida");

        RuleFor(x => x.StandardYield)
            .GreaterThan(0)
            .WithMessage("Rendimento padrão deve ser maior que zero");

        RuleFor(x => x.ShelfLifeDays)
            .GreaterThan(0)
            .When(x => x.ShelfLifeDays.HasValue)
            .WithMessage("Prazo de validade deve ser maior que zero");

        RuleFor(x => x.PreparationInstructions)
            .MaximumLength(2000)
            .WithMessage("Instruções de preparo não podem exceder 2000 caracteres");

        RuleFor(x => x.StorageInstructions)
            .MaximumLength(1000)
            .WithMessage("Instruções de armazenamento não podem exceder 1000 caracteres");

        RuleFor(x => x.UsageInstructions)
            .MaximumLength(1000)
            .WithMessage("Instruções de uso não podem exceder 1000 caracteres");

        RuleFor(x => x.Components)
            .NotEmpty()
            .WithMessage("Fórmula deve ter pelo menos um componente");

        RuleForEach(x => x.Components)
            .SetValidator(new UpdateFormulaComponentValidator());
    }
}

public class UpdateFormulaComponentValidator : AbstractValidator<UpdateFormulaComponentDto>
{
    public UpdateFormulaComponentValidator()
    {
        RuleFor(x => x.RawMaterialId)
            .NotEqual(Guid.Empty)
            .WithMessage("Matéria-prima é obrigatória");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantidade deve ser maior que zero");

        RuleFor(x => x.Unit)
            .NotEmpty()
            .WithMessage("Unidade é obrigatória")
            .Must(unit => new[] { "g", "kg", "mg", "mL", "L", "UN", "%" }.Contains(unit))
            .WithMessage("Unidade inválida. Use: g, kg, mg, mL, L, UN ou %");

        RuleFor(x => x.ComponentType)
            .NotEmpty()
            .WithMessage("Tipo do componente é obrigatório")
            .Must(type => new[] {
                "ATIVO", "EXCIPIENTE", "VEICULO", "CONSERVANTE"
            }.Contains(type.ToUpper()))
            .WithMessage("Tipo de componente inválido");

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(500)
            .WithMessage("Instruções especiais não podem exceder 500 caracteres");
    }
}