using FluentValidation;
using DTOs;

namespace Validators;

public class CreateLabelTemplateValidator : AbstractValidator<CreateLabelTemplateDto>
{
    public CreateLabelTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres");

        RuleFor(x => x.TemplateType)
            .Must(x => x == "PADRAO" || x == "CONTROLADO" || x == "TARJA_PRETA" ||
                      x == "TARJA_VERMELHA" || x == "REFRIGERADO")
            .WithMessage("Tipo de template inválido");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Largura deve ser maior que zero")
            .LessThanOrEqualTo(300).WithMessage("Largura máxima é 300mm");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Altura deve ser maior que zero")
            .LessThanOrEqualTo(300).WithMessage("Altura máxima é 300mm");

        RuleFor(x => x.HtmlTemplate)
            .NotEmpty().WithMessage("Template HTML é obrigatório");
    }
}

public class GenerateLabelValidator : AbstractValidator<GenerateLabelDto>
{
    public GenerateLabelValidator()
    {
        RuleFor(x => x.ManipulationOrderId)
            .NotEmpty().WithMessage("Ordem de manipulação é obrigatória");
    }
}

public class PrintLabelValidator : AbstractValidator<PrintLabelDto>
{
    public PrintLabelValidator()
    {
        RuleFor(x => x.Copies)
            .GreaterThan(0).WithMessage("Número de cópias deve ser maior que zero")
            .LessThanOrEqualTo(10).WithMessage("Máximo de 10 cópias por vez");
    }
}
