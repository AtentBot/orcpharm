namespace Models.Pharmacy.StepData;

// ===================================================================
// STEP DATA - SEPARAÇÃO (Etapa 0)
// ===================================================================

public class SeparacaoStepData
{
    public List<ItemSeparado> Items { get; set; } = new();
    public string? AreaSeparacao { get; set; }
    public DateTime? DataSeparacao { get; set; }
    public bool TodosItensConferidos { get; set; }
}

public class ItemSeparado
{
    public Guid RawMaterialId { get; set; }
    public Guid BatchId { get; set; }
    public string LoteInsumo { get; set; } = default!;
    public decimal QuantidadeNecessaria { get; set; }
    public decimal QuantidadeSeparada { get; set; }
    public string Unidade { get; set; } = default!;
    public string? LocalArmazenagem { get; set; }
    public bool RequerRefrigeracao { get; set; }
    public bool Controlado { get; set; }
}

// ===================================================================
// STEP DATA - APROVAÇÃO (Etapa 6)
// ===================================================================

public class AprovacaoStepData
{
    public Guid FarmaceuticoId { get; set; }
    public string NomeFarmaceutico { get; set; } = default!;
    public string CRF { get; set; } = default!;
    public bool InspecaoVisualOk { get; set; }
    public bool DocumentacaoCompleta { get; set; }
    public bool RotulagemCorreta { get; set; }
    public bool EmbalagemIntegra { get; set; }
    public bool Aprovado { get; set; }
    public string? MotivoRejeicao { get; set; }
    public string? AssinaturaDigital { get; set; }
    public DateTime DataAprovacao { get; set; }
}

// ===================================================================
// STEP DATA - EXPEDIÇÃO (Etapa 7)
// ===================================================================

public class ExpedicaoStepData
{
    public string MetodoEntrega { get; set; } = default!;
    public string? CodigoRastreio { get; set; }
    public string? NomeEntregador { get; set; }
    public string? TelefoneEntregador { get; set; }
    public string? EnderecoEntrega { get; set; }
    public DateTime? PrevisaoEntrega { get; set; }
    public string? NomeRecebedor { get; set; }
    public string? DocumentoRecebedor { get; set; }
    public bool ClienteNotificado { get; set; }
    public string? MetodoNotificacao { get; set; }
    public DateTime? DataEntregaEfetiva { get; set; }
    public string? AssinaturaRecebedor { get; set; }
    public bool EntregaConfirmada { get; set; }
}
