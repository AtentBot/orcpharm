namespace Models.Pharmacy.StepData;

// ===================================================================
// STEP DATA - Classes para armazenar dados JSON de cada etapa
// ===================================================================

/// <summary>
/// Dados da etapa de Pesagem
/// </summary>
public class PesagemStepData
{
    public List<ComponentPesado> Components { get; set; } = new();
    public string? BalancaId { get; set; }
    public string? BalancaNome { get; set; }
    public decimal? AmbienteTemperatura { get; set; }
    public decimal? AmbienteUmidade { get; set; }
}

public class ComponentPesado
{
    public Guid RawMaterialId { get; set; }
    public Guid BatchId { get; set; }
    public decimal QuantidadeEsperada { get; set; }
    public decimal QuantidadePesada { get; set; }
    public string Unidade { get; set; } = default!;
    public string? LoteInsumo { get; set; }
}

/// <summary>
/// Dados da etapa de Mistura
/// </summary>
public class MisturaStepData
{
    public string MetodoMistura { get; set; } = default!;
    public string? EquipamentoUtilizado { get; set; }
    public int? TempoMistura { get; set; }
    public string? VelocidadeMistura { get; set; }
    public string? Observacoes { get; set; }
}

/// <summary>
/// Dados da etapa de Envase
/// </summary>
public class EnvaseStepData
{
    public string TipoEmbalagem { get; set; } = default!;
    public decimal QuantidadeEnvasada { get; set; }
    public string NumeroLote { get; set; } = default!;
    public DateTime DataFabricacao { get; set; }
    public DateTime DataValidade { get; set; }
    public string? Observacoes { get; set; }
}

/// <summary>
/// Dados da etapa de Rotulagem
/// </summary>
public class RotulagemStepData
{
    public string NumeroLote { get; set; } = default!;
    public DateTime DataFabricacao { get; set; }
    public DateTime DataValidade { get; set; }
    public string? InformacoesAdicionais { get; set; }
    public string? CodigoBarras { get; set; }
}

/// <summary>
/// Dados da etapa de Conferência
/// </summary>
public class ConferenciaStepData
{
    public string AspectosVisuais { get; set; } = default!;
    public bool EmbalagemIntegra { get; set; }
    public bool RotuloCorreto { get; set; }
    public bool QuantidadeCorreta { get; set; }
    public bool DocumentacaoCompleta { get; set; }
    public string? Observacoes { get; set; }
    public bool AprovadoPorFarmaceutico { get; set; }
    public string? FarmaceuticoResponsavel { get; set; }
    public string? CRF { get; set; }
}