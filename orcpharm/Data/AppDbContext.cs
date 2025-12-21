using Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Core;
using Models.Employees;
using Models.Pharmacy;
using Models.Security;
using Models.Purchasing;
using Models.Auth;
using Models.Fiscal;
using Models.Billing;

namespace Data;

/// <summary>
/// DbContext principal do OrcPharm - Sistema de Gestão para Farmácias de Manipulação
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // ════════════════════════════════════════════════════════════════════════
    // CORE & INFRAESTRUTURA
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Establishment> Establishments => Set<Establishment>();
    public DbSet<AccessLevel> AccessLevels => Set<AccessLevel>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<ClientOnboarding> ClientOnboardings { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // SAAS - BILLING & SUBSCRIPTIONS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Planos de assinatura disponíveis (Básico, Profissional, Enterprise)
    /// </summary>
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;

    /// <summary>
    /// Assinaturas ativas/canceladas dos estabelecimentos
    /// </summary>
    public DbSet<Subscription> Subscriptions { get; set; } = null!;

    /// <summary>
    /// Faturas de cobrança da assinatura SaaS (Farmácia → OrcPharm via Stripe)
    /// </summary>
    public DbSet<SubscriptionInvoice> SubscriptionInvoices { get; set; } = null!;

    /// <summary>
    /// Métodos de pagamento cadastrados (cartões de crédito)
    /// </summary>
    public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;

    /// <summary>
    /// Administradores do sistema SaaS
    /// </summary>
    public DbSet<SaasAdmin> SaasAdmins { get; set; } = null!;

    /// <summary>
    /// Sessões dos administradores SaaS
    /// </summary>
    public DbSet<SaasAdminSession> SaasAdminSessions { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // FUNCIONÁRIOS & RH
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeSession> EmployeeSessions => Set<EmployeeSession>();
    public DbSet<JobPosition> JobPositions => Set<JobPosition>();
    public DbSet<EmployeeJobHistory> EmployeeJobHistories => Set<EmployeeJobHistory>();
    public DbSet<EmployeeBenefit> EmployeeBenefits => Set<EmployeeBenefit>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();

    // ════════════════════════════════════════════════════════════════════════
    // SEGURANÇA & PERMISSÕES
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AccessProfile> AccessProfiles => Set<AccessProfile>();

    // ════════════════════════════════════════════════════════════════════════
    // AUTENTICAÇÃO & SEGURANÇA
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
    public DbSet<TwoFactorToken> TwoFactorAuths { get; set; } = null!;
    public DbSet<LoginAttempt> LoginAttempts { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // FARMÁCIA - ESTOQUE & MATÉRIAS-PRIMAS
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<ControlledSubstanceMovement> ControlledSubstanceMovements => Set<ControlledSubstanceMovement>();
    public DbSet<ControlledSubstanceBalance> ControlledSubstanceBalances => Set<ControlledSubstanceBalance>();

    // ════════════════════════════════════════════════════════════════════════
    // FARMÁCIA - FÓRMULAS & MANIPULAÇÃO
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Formula> Formulas => Set<Formula>();
    public DbSet<FormulaComponent> FormulaComponents => Set<FormulaComponent>();
    public DbSet<ManipulationOrder> ManipulationOrders => Set<ManipulationOrder>();
    public DbSet<ManipulationStep> ManipulationSteps { get; set; } = null!;
    public DbSet<ManipulationPhoto> ManipulationPhotos { get; set; } = null!;
    public DbSet<ManipulationLoss> ManipulationLosses { get; set; } = null!;
    public DbSet<ManipulationLeftover> ManipulationLeftovers { get; set; } = null!;
    public DbSet<DualVerification> DualVerifications { get; set; } = null!;
    public DbSet<ProductionRecord> ProductionRecords { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // FORNECEDORES & COMPRAS
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierContact> SupplierContacts { get; set; } = null!;
    public DbSet<SupplierCertificate> SupplierCertificates { get; set; } = null!;
    public DbSet<SupplierEvaluation> SupplierEvaluations { get; set; } = null!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;
    public DbSet<BatchReceiving> BatchReceivings { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // CLIENTES & PRESCRIÇÕES
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionFile> PrescriptionFiles { get; set; } = null!;
    public DbSet<SpecialPrescriptionControl> SpecialPrescriptionControls => Set<SpecialPrescriptionControl>();

    // ════════════════════════════════════════════════════════════════════════
    // VENDAS & PAGAMENTOS
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();

    // ════════════════════════════════════════════════════════════════════════
    // FISCAL - NOTAS FISCAIS (NF-e/NFC-e)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Notas Fiscais emitidas pela farmácia (Cliente → Farmácia via SEFAZ)
    /// </summary>
    public DbSet<FiscalInvoice> FiscalInvoices { get; set; } = null!;

    /// <summary>
    /// Itens das Notas Fiscais com detalhes de tributação
    /// </summary>
    public DbSet<FiscalInvoiceItem> FiscalInvoiceItems { get; set; } = null!;

    /// <summary>
    /// Configurações fiscais por estabelecimento (certificado, séries, CSC)
    /// </summary>
    public DbSet<FiscalConfig> FiscalConfigs { get; set; } = null!;

    /// <summary>
    /// Fila de contingência para notas não transmitidas
    /// </summary>
    public DbSet<FiscalQueue> FiscalQueues { get; set; } = null!;

    /// <summary>
    /// Log de eventos fiscais (emissão, cancelamento, erros)
    /// </summary>
    public DbSet<FiscalLog> FiscalLogs { get; set; } = null!;

    /// <summary>
    /// Inutilização de numeração fiscal
    /// </summary>
    public DbSet<FiscalNumberGap> FiscalNumberGaps { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // CAIXA & FINANCEIRO
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<CashRegister> CashRegisters { get; set; } = null!;
    public DbSet<CashMovement> CashMovements { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // RÓTULOS ANVISA
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<LabelTemplate> LabelTemplates => Set<LabelTemplate>();
    public DbSet<GeneratedLabel> GeneratedLabels => Set<GeneratedLabel>();
    public DbSet<LabelPrintLog> LabelPrintLogs => Set<LabelPrintLog>();

    // ════════════════════════════════════════════════════════════════════════
    // CONFIGURAÇÕES
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<CompanySettings> CompanySettings { get; set; } = null!;
    public DbSet<PrescriptionQuote> PrescriptionQuotes { get; set; } = null!;
    public DbSet<ManipulationOrderComponent> ManipulationOrderComponents { get; set; } = null!;
    public DbSet<SaasAdminPasswordReset> SaasAdminPasswordResets { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // PORTAL DO CLIENTE
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<CustomerAuth> CustomerAuths { get; set; } = null!;
    public DbSet<CustomerSession> CustomerSessions { get; set; } = null!;
    public DbSet<EstablishmentQRCode> EstablishmentQRCodes { get; set; } = null!;

    // ════════════════════════════════════════════════════════════════════════
    // CATÁLOGO & E-COMMERCE
    // ════════════════════════════════════════════════════════════════════════

    public DbSet<CatalogCategory> CatalogCategories { get; set; } = null!;
    public DbSet<CatalogProduct> CatalogProducts { get; set; } = null!;
    public DbSet<CustomerCart> CustomerCarts { get; set; } = null!;
    public DbSet<CustomerCartItem> CustomerCartItems { get; set; } = null!;
    public DbSet<OnlineOrder> OnlineOrders { get; set; } = null!;
    public DbSet<OnlineOrderItem> OnlineOrderItems { get; set; } = null!;


    // ════════════════════════════════════════════════════════════════════════
    // CONFIGURAÇÃO DO MODELO
    // ════════════════════════════════════════════════════════════════════════

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ──────────────────────────────────────────────────────────────────
        // APLICAR CONFIGURAÇÕES VIA FLUENT API
        // ──────────────────────────────────────────────────────────────────

        ApplyEntityConfigurations(modelBuilder);

        // ──────────────────────────────────────────────────────────────────
        // CONFIGURAÇÕES ESPECÍFICAS
        // ──────────────────────────────────────────────────────────────────

        ConfigureManipulationWorkflow(modelBuilder);
        ConfigureManipulationEntities(modelBuilder);
        ConfigureLabelEntities(modelBuilder);
        ConfigureSubscriptionInvoices(modelBuilder);
        ConfigureFiscalInvoices(modelBuilder);
        ConfigureFiscalEntities(modelBuilder); // NOVO - Entidades fiscais complementares
        ConfigureSaasAdminEntities(modelBuilder);
        ConfigurePortalClienteEntities(modelBuilder);
        ConfigureCatalogoEntities(modelBuilder);

        // ──────────────────────────────────────────────────────────────────
        // SEED DE DADOS INICIAIS
        // ──────────────────────────────────────────────────────────────────

        SeedInitialData(modelBuilder);
    }

    // ════════════════════════════════════════════════════════════════════════
    // MÉTODOS DE CONFIGURAÇÃO
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Aplica todas as configurações de entidades via IEntityTypeConfiguration
    /// </summary>
    private void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new JobPositionConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeJobHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeSessionConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeBenefitConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierContactConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierCertificateConfiguration());
        modelBuilder.ApplyConfiguration(new SupplierEvaluationConfiguration());
    }

    /// <summary>
    /// Configura entidades de manipulação (Losses, Leftovers, Verification, Production)
    /// </summary>
    private void ConfigureManipulationEntities(ModelBuilder modelBuilder)
    {

        // ──────────────────────────────────────────────────────────────────
        // MANIPULATION LOSS
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<ManipulationLoss>(entity =>
        {
            entity.ToTable("manipulation_losses");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.ManipulationOrder)
                .WithMany()
                .HasForeignKey(e => e.ManipulationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RawMaterial)
                .WithMany()
                .HasForeignKey(e => e.RawMaterialId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RegisteredByEmployee)
                .WithMany()
                .HasForeignKey(e => e.RegisteredByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ──────────────────────────────────────────────────────────────────
        // MANIPULATION LEFTOVER
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<ManipulationLeftover>(entity =>
        {
            entity.ToTable("manipulation_leftovers");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.ManipulationOrder)
                .WithMany()
                .HasForeignKey(e => e.ManipulationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RawMaterial)
                .WithMany()
                .HasForeignKey(e => e.RawMaterialId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RegisteredByEmployee)
                .WithMany()
                .HasForeignKey(e => e.RegisteredByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ──────────────────────────────────────────────────────────────────
        // DUAL VERIFICATION
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<DualVerification>(entity =>
        {
            entity.ToTable("dual_verifications");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.ManipulationOrder)
                .WithMany()
                .HasForeignKey(e => e.ManipulationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FirstVerifier)
                .WithMany()
                .HasForeignKey(e => e.FirstVerifierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SecondVerifier)
                .WithMany()
                .HasForeignKey(e => e.SecondVerifierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ──────────────────────────────────────────────────────────────────
        // PRODUCTION RECORD
        // ──────────────────────────────────────────────────────────────────

        // ProductionRecord
        modelBuilder.Entity<ProductionRecord>(entity =>
        {
            entity.ToTable("production_records");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ManipulationOrderId).IsUnique();

            entity.HasOne(e => e.ManipulationOrder)
                .WithMany()
                .HasForeignKey(e => e.ManipulationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ProducedByEmployee)
                .WithMany()
                .HasForeignKey(e => e.ProducedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.VerifiedByEmployee)
                .WithMany()
                .HasForeignKey(e => e.VerifiedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ApprovedByPharmacist)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByPharmacistId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ──────────────────────────────────────────────────────────────────
        // STOCK MOVEMENT (Employee relationships)
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasOne(sm => sm.PerformedByEmployee)
                .WithMany(e => e.StockMovements)
                .HasForeignKey(sm => sm.PerformedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sm => sm.AuthorizedByEmployee)
                .WithMany()
                .HasForeignKey(sm => sm.AuthorizedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePortalClienteEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // CUSTOMER AUTH
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CustomerAuth>(entity =>
        {
            entity.ToTable("CustomerAuths");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Phone).IsUnique();
            entity.HasIndex(e => e.Cpf).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.IsVerified);

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // CUSTOMER SESSION
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CustomerSession>(entity =>
        {
            entity.ToTable("CustomerSessions");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ExpiresAt);

            entity.HasOne(e => e.CustomerAuth)
                  .WithMany(a => a.Sessions)
                  .HasForeignKey(e => e.CustomerAuthId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CurrentEstablishment)
                  .WithMany()
                  .HasForeignKey(e => e.CurrentEstablishmentId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastActivityAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });

        // ──────────────────────────────────────────────────────────────────
        // ESTABLISHMENT QR CODE
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<EstablishmentQRCode>(entity =>
        {
            entity.ToTable("EstablishmentQRCodes");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByEmployee)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.ScanCount)
                .HasDefaultValue(0);
        });
    }

    /// <summary>
    /// Configura entidades do Catálogo e E-commerce
    /// </summary>
    private void ConfigureCatalogoEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // CATALOG CATEGORY
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CatalogCategory>(entity =>
        {
            entity.ToTable("CatalogCategories");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // CATALOG PRODUCT
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CatalogProduct>(entity =>
        {
            entity.ToTable("CatalogProducts");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // CUSTOMER CART
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CustomerCart>(entity =>
        {
            entity.ToTable("CustomerCarts");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.CustomerId, e.EstablishmentId }).IsUnique();

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // CUSTOMER CART ITEM
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<CustomerCartItem>(entity =>
        {
            entity.ToTable("CustomerCartItems");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CartId);

            entity.HasOne(e => e.Cart)
                  .WithMany(c => c.Items)
                  .HasForeignKey(e => e.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // ONLINE ORDER
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<OnlineOrder>(entity =>
        {
            entity.ToTable("OnlineOrders");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.EstablishmentId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Establishment)
                  .WithMany()
                  .HasForeignKey(e => e.EstablishmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // ONLINE ORDER ITEM
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<OnlineOrderItem>(entity =>
        {
            entity.ToTable("OnlineOrderItems");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.OrderId);

            entity.HasOne(e => e.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Configura workflow de manipulação (Steps, Photos)
    /// </summary>
    private void ConfigureManipulationWorkflow(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // MANIPULATION STEP
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<ManipulationStep>()
            .HasOne(s => s.ManipulationOrder)
            .WithMany()
            .HasForeignKey(s => s.ManipulationOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ManipulationStep>()
            .HasOne(s => s.PerformedByEmployee)
            .WithMany()
            .HasForeignKey(s => s.PerformedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManipulationStep>()
            .HasOne(s => s.CheckedByEmployee)
            .WithMany()
            .HasForeignKey(s => s.CheckedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Coluna JSON para StepData
        modelBuilder.Entity<ManipulationStep>()
            .Property(s => s.StepData)
            .HasColumnType("jsonb");

        // ──────────────────────────────────────────────────────────────────
        // MANIPULATION PHOTO
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<ManipulationPhoto>()
            .HasOne(p => p.ManipulationOrder)
            .WithMany()
            .HasForeignKey(p => p.ManipulationOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ManipulationPhoto>()
            .HasOne(p => p.ManipulationStep)
            .WithMany(s => s.Photos)
            .HasForeignKey(p => p.ManipulationStepId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ManipulationPhoto>()
            .HasOne(p => p.CapturedByEmployee)
            .WithMany()
            .HasForeignKey(p => p.CapturedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Configura entidades de rótulos ANVISA
    /// </summary>
    private void ConfigureLabelEntities(ModelBuilder modelBuilder)
    {
        // Implementação existente de labels...
        // (mantém a implementação atual se houver)
    }

    /// <summary>
    /// Configura SubscriptionInvoices (Faturas SaaS - Farmácia → OrcPharm)
    /// </summary>
    private void ConfigureSubscriptionInvoices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriptionInvoice>(entity =>
        {
            entity.ToTable("subscription_invoices");
            entity.HasKey(e => e.Id);

            entity.HasKey(e => e.Id);
            entity.ToTable("subscription_invoices");

            // ──────────────────────────────────────────────────────────────
            // RELACIONAMENTOS
            // ──────────────────────────────────────────────────────────────

            entity.HasOne(e => e.Subscription)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // ──────────────────────────────────────────────────────────────
            // ÍNDICES PARA PERFORMANCE
            // ──────────────────────────────────────────────────────────────

            entity.HasIndex(e => e.SubscriptionId)
                .HasDatabaseName("idx_subscription_invoices_subscription_id");

            entity.HasIndex(e => e.StripeInvoiceId)
                .HasDatabaseName("idx_subscription_invoices_stripe_invoice_id")
                .IsUnique()
                .HasFilter("stripe_invoice_id IS NOT NULL");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_subscription_invoices_status");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_subscription_invoices_created_at");

            entity.HasIndex(e => new { e.Status, e.DueDate })
                .HasDatabaseName("idx_subscription_invoices_status_due_date");

            // ──────────────────────────────────────────────────────────────
            // VALORES PADRÃO
            // ──────────────────────────────────────────────────────────────

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Status)
                .HasDefaultValue("PENDING");
        });
    }

    /// <summary>
    /// Configura FiscalInvoices (Notas Fiscais - Cliente → Farmácia)
    /// </summary>
    private void ConfigureFiscalInvoices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FiscalInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("fiscal_invoices");

            // ──────────────────────────────────────────────────────────────
            // RELACIONAMENTOS
            // ──────────────────────────────────────────────────────────────

            entity.HasOne(e => e.Sale)
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Establishment)
                .WithMany()
                .HasForeignKey(e => e.EstablishmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacionamento com itens
            entity.HasMany(e => e.Items)
                .WithOne(i => i.FiscalInvoice)
                .HasForeignKey(i => i.FiscalInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // ──────────────────────────────────────────────────────────────
            // ÍNDICES PARA PERFORMANCE
            // ──────────────────────────────────────────────────────────────

            entity.HasIndex(e => e.EstablishmentId)
                .HasDatabaseName("idx_fiscal_invoices_establishment_id");

            entity.HasIndex(e => e.SaleId)
                .HasDatabaseName("idx_fiscal_invoices_sale_id");

            entity.HasIndex(e => e.InvoiceKey)
                .HasDatabaseName("idx_fiscal_invoices_invoice_key")
                .IsUnique()
                .HasFilter("invoice_key IS NOT NULL");

            entity.HasIndex(e => new { e.EstablishmentId, e.InvoiceNumber, e.Series })
                .HasDatabaseName("idx_fiscal_invoices_number_series")
                .IsUnique();

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_fiscal_invoices_status");

            entity.HasIndex(e => e.IssueDate)
                .HasDatabaseName("idx_fiscal_invoices_issue_date");

            entity.HasIndex(e => new { e.EstablishmentId, e.IssueDate })
                .HasDatabaseName("idx_fiscal_invoices_establishment_issue_date");

            // ──────────────────────────────────────────────────────────────
            // VALORES PADRÃO
            // ──────────────────────────────────────────────────────────────

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Status)
                .HasDefaultValue("PENDENTE");

            entity.Property(e => e.InvoiceType)
                .HasDefaultValue("NFCE");
        });
    }

    /// <summary>
    /// Configura entidades fiscais complementares (Config, Items, Queue, Logs, Gaps)
    /// NOVO - Adicionado para módulo fiscal completo
    /// </summary>
    private void ConfigureFiscalEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // FISCAL INVOICE ITEM
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<FiscalInvoiceItem>(entity =>
        {
            entity.ToTable("fiscal_invoice_items");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.FiscalInvoiceId)
                .HasDatabaseName("idx_fiscal_invoice_items_invoice");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");
        });

        // ──────────────────────────────────────────────────────────────────
        // FISCAL CONFIG
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<FiscalConfig>(entity =>
        {
            entity.ToTable("fiscal_configs");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId)
                .IsUnique()
                .HasDatabaseName("idx_fiscal_configs_establishment");

            entity.HasOne(e => e.Establishment)
                .WithMany()
                .HasForeignKey(e => e.EstablishmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.Environment)
                .HasDefaultValue("HOMOLOGACAO");

            entity.Property(e => e.TaxRegime)
                .HasDefaultValue("SIMPLES_NACIONAL");
        });

        // ──────────────────────────────────────────────────────────────────
        // FISCAL QUEUE
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<FiscalQueue>(entity =>
        {
            entity.ToTable("fiscal_queue");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId)
                .HasDatabaseName("idx_fiscal_queue_establishment");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_fiscal_queue_status");

            entity.HasIndex(e => e.NextAttempt)
                .HasDatabaseName("idx_fiscal_queue_next");

            entity.HasOne(e => e.Sale)
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FiscalInvoice)
                .WithMany()
                .HasForeignKey(e => e.FiscalInvoiceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Status)
                .HasDefaultValue("PENDENTE");

            entity.Property(e => e.Attempts)
                .HasDefaultValue(0);

            entity.Property(e => e.MaxAttempts)
                .HasDefaultValue(3);
        });

        // ──────────────────────────────────────────────────────────────────
        // FISCAL LOG
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<FiscalLog>(entity =>
        {
            entity.ToTable("fiscal_logs");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId)
                .HasDatabaseName("idx_fiscal_logs_establishment");

            entity.HasIndex(e => e.FiscalInvoiceId)
                .HasDatabaseName("idx_fiscal_logs_invoice");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_fiscal_logs_date");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ──────────────────────────────────────────────────────────────────
        // FISCAL NUMBER GAP (Inutilização)
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<FiscalNumberGap>(entity =>
        {
            entity.ToTable("fiscal_number_gaps");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EstablishmentId)
                .HasDatabaseName("idx_fiscal_gaps_establishment");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Status)
                .HasDefaultValue("PENDENTE");
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // SEED DE DADOS INICIAIS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Seed de dados iniciais (AccessLevels e Permissions)
    /// </summary>
    private void SeedInitialData(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // SEED DE NÍVEIS DE ACESSO
        // ──────────────────────────────────────────────────────────────────

        var ownerAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var managerAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var employeeAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var userAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000004");

        modelBuilder.Entity<AccessLevel>().HasData(
            new AccessLevel
            {
                Id = ownerAccessLevelId,
                Code = "owner",
                Name = "Proprietário",
                Description = "Acesso total ao sistema, incluindo configurações críticas e gestão financeira",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccessLevel
            {
                Id = managerAccessLevelId,
                Code = "manager",
                Name = "Gerente",
                Description = "Acesso de gerenciamento com permissões administrativas e de supervisão",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccessLevel
            {
                Id = employeeAccessLevelId,
                Code = "employee",
                Name = "Funcionário",
                Description = "Acesso básico para funcionários operacionais",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccessLevel
            {
                Id = userAccessLevelId,
                Code = "user",
                Name = "Usuário",
                Description = "Acesso limitado para usuários externos ou clientes",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // ──────────────────────────────────────────────────────────────────
        // SEED DE PERMISSÕES
        // ──────────────────────────────────────────────────────────────────

        SeedPermissions(modelBuilder);
    }

    /// <summary>
    /// Seed de permissões do sistema
    /// </summary>
    private void SeedPermissions(ModelBuilder modelBuilder)
    {
        var permissions = new[]
        {
            // ══════════════════════════════════════════════════════════════
            // INVENTORY PERMISSIONS
            // ══════════════════════════════════════════════════════════════
            
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000101"),
                ResourceAction = "inventory.read",
                Resource = "inventory",
                Action = "read",
                Category = "Inventory",
                DisplayName = "Visualizar Estoque",
                Description = "Permite visualizar informações de estoque",
                Scope = "Own",
                RiskLevel = "Low",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000102"),
                ResourceAction = "inventory.write",
                Resource = "inventory",
                Action = "write",
                Category = "Inventory",
                DisplayName = "Editar Estoque",
                Description = "Permite adicionar e editar itens de estoque",
                Scope = "Establishment",
                RiskLevel = "Medium",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },

            // ══════════════════════════════════════════════════════════════
            // SALES PERMISSIONS
            // ══════════════════════════════════════════════════════════════
            
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000201"),
                ResourceAction = "sales.create",
                Resource = "sales",
                Action = "create",
                Category = "Sales",
                DisplayName = "Criar Vendas",
                Description = "Permite criar novas vendas",
                Scope = "Own",
                RiskLevel = "Medium",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000202"),
                ResourceAction = "sales.read",
                Resource = "sales",
                Action = "read",
                Category = "Sales",
                DisplayName = "Visualizar Vendas",
                Description = "Permite visualizar vendas",
                Scope = "Own",
                RiskLevel = "Low",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },

            // ══════════════════════════════════════════════════════════════
            // REPORTS PERMISSIONS
            // ══════════════════════════════════════════════════════════════
            
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000301"),
                ResourceAction = "reports.read",
                Resource = "reports",
                Action = "read",
                Category = "Reports",
                DisplayName = "Visualizar Relatórios",
                Description = "Permite visualizar relatórios",
                Scope = "Establishment",
                RiskLevel = "Medium",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000302"),
                ResourceAction = "reports.export",
                Resource = "reports",
                Action = "export",
                Category = "Reports",
                DisplayName = "Exportar Relatórios",
                Description = "Permite exportar relatórios",
                Scope = "Establishment",
                RiskLevel = "High",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },

            // ══════════════════════════════════════════════════════════════
            // SETTINGS PERMISSIONS
            // ══════════════════════════════════════════════════════════════
            
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000401"),
                ResourceAction = "settings.update",
                Resource = "settings",
                Action = "update",
                Category = "Settings",
                DisplayName = "Alterar Configurações",
                Description = "Permite alterar configurações do sistema",
                Scope = "Establishment",
                RiskLevel = "Critical",
                RequiresTwoFactor = true,
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        modelBuilder.Entity<Permission>().HasData(permissions);
    }

    private void ConfigureSaasAdminEntities(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────────
        // SAAS ADMIN
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<SaasAdmin>(entity =>
        {
            entity.ToTable("saas_admins");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });

        // ──────────────────────────────────────────────────────────────────
        // SAAS ADMIN SESSION
        // ──────────────────────────────────────────────────────────────────

        modelBuilder.Entity<SaasAdminSession>(entity =>
        {
            entity.ToTable("saas_admin_sessions");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.SaasAdminId);

            entity.HasOne(e => e.SaasAdmin)
                .WithMany(a => a.Sessions)
                .HasForeignKey(e => e.SaasAdminId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastActivityAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<SaasAdminPasswordReset>(entity =>
        {
            entity.ToTable("saas_admin_password_resets");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.SaasAdminId)
                .HasColumnName("saas_admin_id")
                .IsRequired();

            entity.Property(e => e.Token)
                .HasColumnName("token")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(45);

            entity.Property(e => e.UserAgent)
                .HasColumnName("user_agent")
                .HasMaxLength(500);

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at")
                .IsRequired();

            entity.Property(e => e.UsedAt)
                .HasColumnName("used_at");

            entity.Property(e => e.IsUsed)
                .HasColumnName("is_used")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            // Relacionamento
            entity.HasOne(e => e.SaasAdmin)
                .WithMany()
                .HasForeignKey(e => e.SaasAdminId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.SaasAdminId);
            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}
