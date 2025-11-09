using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Establishments_EstablishmentId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_Employees_ApprovedByPharmacistId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_Employees_CheckedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_Employees_ManipulatedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_Employees_RequestedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Employees_AuthorizedByPharmacistId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Employees_SoldByEmployeeId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Employees_AuthorizedByEmployeeId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Employees_PerformedByEmployeeId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "EmployeeSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Employees",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Crf",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Email",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_EstablishmentId_IsActive",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CanApproveFormulas",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CanApproveQuality",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CanControlStock",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CanHandleControlledSubstances",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CanManipulate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CanSell",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CrfState",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecret",
                table: "Employees");

            migrationBuilder.RenameTable(
                name: "Employees",
                newName: "employees");

            migrationBuilder.RenameColumn(
                name: "RequiresTwoFactor",
                table: "employees",
                newName: "TwoFactorEnabled");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "employees",
                newName: "Street");

            migrationBuilder.RenameColumn(
                name: "LastLoginAt",
                table: "employees",
                newName: "PasswordLastChanged");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "employees",
                newName: "RequirePasswordChange");

            migrationBuilder.RenameColumn(
                name: "HiredDate",
                table: "employees",
                newName: "PasswordCreatedAt");

            migrationBuilder.RenameColumn(
                name: "CrfExpiryDate",
                table: "employees",
                newName: "LockedUntil");

            migrationBuilder.RenameColumn(
                name: "Crf",
                table: "employees",
                newName: "PisPasep");

            migrationBuilder.RenameIndex(
                name: "IX_Employees_Cpf",
                table: "employees",
                newName: "ix_employees_cpf");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "employees",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "TerminationDate",
                table: "employees",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                table: "employees",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountDigit",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountType",
                table: "employees",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankBranch",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "employees",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "employees",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Complement",
                table: "employees",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractType",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "CLT");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ctps",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CtpsIssueDate",
                table: "employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CtpsSeries",
                table: "employees",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CtpsUf",
                table: "employees",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "employees",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DependentsCount",
                table: "employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DriverLicense",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverLicenseCategory",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DriverLicenseExpiry",
                table: "employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactRelationship",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "HireDate",
                table: "employees",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<Guid>(
                name: "JobPositionId",
                table: "employees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "MaritalStatus",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MilitaryService",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "employees",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordAlgorithm",
                table: "employees",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfBirth",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "employees",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProbationEndDate",
                table: "employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RgIssueDate",
                table: "employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RgIssuer",
                table: "employees",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Salary",
                table: "employees",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SocialName",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "employees",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "employees",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Ativo");

            migrationBuilder.AddColumn<string>(
                name: "StatusNotes",
                table: "employees",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoterRegistration",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkShift",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AccessLevels",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);

            migrationBuilder.AddPrimaryKey(
                name: "PK_employees",
                table: "employees",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AccessProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HierarchyLevel = table.Column<int>(type: "integer", nullable: false),
                    CanManageEmployees = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageFinances = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageInventory = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageReports = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageSettings = table.Column<bool>(type: "boolean", nullable: false),
                    CanApproveOrders = table.Column<bool>(type: "boolean", nullable: false),
                    CanDeleteRecords = table.Column<bool>(type: "boolean", nullable: false),
                    CanExportData = table.Column<bool>(type: "boolean", nullable: false),
                    MaxTransactionValue = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    MaxDiscountPercent = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    MaxDailyTransactions = table.Column<int>(type: "integer", nullable: true),
                    SessionDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxConcurrentSessions = table.Column<int>(type: "integer", nullable: false),
                    RequireTwoFactor = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessProfiles_AccessLevels_AccessLevelId",
                        column: x => x.AccessLevelId,
                        principalTable: "AccessLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_benefits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BenefitType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BenefitName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MonthlyValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EmployeeContribution = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    EmployerContribution = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ProviderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CardNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeductFromSalary = table.Column<bool>(type: "boolean", nullable: false),
                    DeductionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DependentsIncluded = table.Column<int>(type: "integer", nullable: false),
                    DependentsNames = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_benefits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_benefits_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocumentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HasExpiry = table.Column<bool>(type: "boolean", nullable: false),
                    IsExpired = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pendente"),
                    StatusNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsConfidential = table.Column<bool>(type: "boolean", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ReplacesDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_documents_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OperatingSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequiresTwoFactor = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorVerified = table.Column<bool>(type: "boolean", nullable: false),
                    AccessCount = table.Column<int>(type: "integer", nullable: false),
                    SessionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RefreshToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_sessions_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Responsibilities = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    HierarchyLevel = table.Column<int>(type: "integer", nullable: false),
                    ReportsTo = table.Column<Guid>(type: "uuid", nullable: true),
                    RequiredEducation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequiredCertification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequiresCertification = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredExperience = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SuggestedSalaryMin = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    SuggestedSalaryMax = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    SalaryType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsSystemDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_positions_Establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_positions_job_positions_ReportsTo",
                        column: x => x.ReportsTo,
                        principalTable: "job_positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceAction = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Resource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Own"),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Low"),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresTwoFactor = table.Column<bool>(type: "boolean", nullable: false),
                    AuditLog = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DependsOn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsSystemPermission = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AccessProfileId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_permissions_AccessProfiles_AccessProfileId",
                        column: x => x.AccessProfileId,
                        principalTable: "AccessProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "employee_job_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SalaryAtTime = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PreviousSalary = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_job_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_job_history_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_job_history_job_positions_JobPositionId",
                        column: x => x.JobPositionId,
                        principalTable: "job_positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsGranted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CustomConditions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GrantedFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrantedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPermanent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrantReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_permissions_Establishments_EstablishmentId",
                        column: x => x.EstablishmentId,
                        principalTable: "Establishments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_permissions_job_positions_JobPositionId",
                        column: x => x.JobPositionId,
                        principalTable: "job_positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AccessLevels",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "owner", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acesso total ao sistema, incluindo configurações críticas e gestão financeira", true, "Proprietário", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "manager", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acesso de gerenciamento com permissões administrativas e de supervisão", true, "Gerente", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "employee", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acesso básico para funcionários operacionais", true, "Funcionário", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "user", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acesso limitado para usuários externos ou clientes", true, "Usuário", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "AccessProfileId", "Action", "AuditLog", "Category", "CreatedAt", "DependsOn", "Description", "DisplayName", "IsActive", "IsSystemPermission", "RequiresApproval", "RequiresTwoFactor", "Resource", "ResourceAction", "RiskLevel", "Scope", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), null, "create", true, "HR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite criar novos funcionários", "Criar Funcionários", true, true, false, false, "employees", "employees.create", "High", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000002"), null, "read", true, "HR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite visualizar dados de funcionários", "Visualizar Funcionários", true, true, false, false, "employees", "employees.read", "Low", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000003"), null, "update", true, "HR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite editar dados de funcionários", "Editar Funcionários", true, true, false, false, "employees", "employees.update", "Medium", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000004"), null, "delete", true, "HR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite deletar funcionários", "Deletar Funcionários", true, true, true, false, "employees", "employees.delete", "Critical", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000005"), null, "terminate", true, "HR", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite demitir funcionários", "Demitir Funcionários", true, true, true, false, "employees", "employees.terminate", "Critical", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000101"), null, "read", true, "Inventory", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite visualizar o estoque", "Visualizar Estoque", true, true, false, false, "inventory", "inventory.read", "Low", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000102"), null, "update", true, "Inventory", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite atualizar quantidades no estoque", "Atualizar Estoque", true, true, false, false, "inventory", "inventory.update", "Medium", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000201"), null, "create", true, "Sales", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite realizar vendas", "Realizar Vendas", true, true, false, false, "sales", "sales.create", "Medium", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000202"), null, "read", true, "Sales", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite visualizar vendas", "Visualizar Vendas", true, true, false, false, "sales", "sales.read", "Low", "Own", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000301"), null, "read", true, "Reports", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite visualizar relatórios", "Visualizar Relatórios", true, true, false, false, "reports", "reports.read", "Medium", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000302"), null, "export", true, "Reports", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite exportar relatórios", "Exportar Relatórios", true, true, false, false, "reports", "reports.export", "High", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000401"), null, "update", true, "Settings", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Permite alterar configurações do sistema", "Alterar Configurações", true, true, false, true, "settings", "settings.update", "Critical", "Establishment", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_employees_email",
                table: "employees",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "ix_employees_establishment_id",
                table: "employees",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "ix_employees_job_position_id",
                table: "employees",
                column: "JobPositionId");

            migrationBuilder.CreateIndex(
                name: "ix_employees_status",
                table: "employees",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AccessProfiles_AccessLevelId",
                table: "AccessProfiles",
                column: "AccessLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessProfiles_Code",
                table: "AccessProfiles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_benefits_BenefitType",
                table: "employee_benefits",
                column: "BenefitType");

            migrationBuilder.CreateIndex(
                name: "IX_employee_benefits_EmployeeId",
                table: "employee_benefits",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_benefits_IsActive",
                table: "employee_benefits",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_employee_documents_DocumentType",
                table: "employee_documents",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_employee_documents_EmployeeId",
                table: "employee_documents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_documents_ExpiryDate",
                table: "employee_documents",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_employee_job_history_EmployeeId",
                table: "employee_job_history",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_job_history_JobPositionId",
                table: "employee_job_history",
                column: "JobPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_job_history_StartDate",
                table: "employee_job_history",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_employee_sessions_EmployeeId",
                table: "employee_sessions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_sessions_ExpiresAt",
                table: "employee_sessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_employee_sessions_IsActive",
                table: "employee_sessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_employee_sessions_Token",
                table: "employee_sessions",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_positions_code",
                table: "job_positions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_positions_establishment_id",
                table: "job_positions",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_job_positions_ReportsTo",
                table: "job_positions",
                column: "ReportsTo");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_AccessProfileId",
                table: "permissions",
                column: "AccessProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Category",
                table: "permissions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Resource",
                table: "permissions",
                column: "Resource");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_ResourceAction",
                table: "permissions",
                column: "ResourceAction",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_EstablishmentId",
                table: "role_permissions",
                column: "EstablishmentId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_JobPositionId_PermissionId",
                table: "role_permissions",
                columns: new[] { "JobPositionId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_PermissionId",
                table: "role_permissions",
                column: "PermissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_employees_Establishments_EstablishmentId",
                table: "employees",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_employees_job_positions_JobPositionId",
                table: "employees",
                column: "JobPositionId",
                principalTable: "job_positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_ApprovedByPharmacistId",
                table: "ManipulationOrders",
                column: "ApprovedByPharmacistId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_CheckedByEmployeeId",
                table: "ManipulationOrders",
                column: "CheckedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_ManipulatedByEmployeeId",
                table: "ManipulationOrders",
                column: "ManipulatedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_employees_RequestedByEmployeeId",
                table: "ManipulationOrders",
                column: "RequestedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_employees_AuthorizedByPharmacistId",
                table: "Sales",
                column: "AuthorizedByPharmacistId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_employees_SoldByEmployeeId",
                table: "Sales",
                column: "SoldByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_employees_AuthorizedByEmployeeId",
                table: "StockMovements",
                column: "AuthorizedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_employees_PerformedByEmployeeId",
                table: "StockMovements",
                column: "PerformedByEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employees_Establishments_EstablishmentId",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "FK_employees_job_positions_JobPositionId",
                table: "employees");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_ApprovedByPharmacistId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_CheckedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_ManipulatedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ManipulationOrders_employees_RequestedByEmployeeId",
                table: "ManipulationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_employees_AuthorizedByPharmacistId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_employees_SoldByEmployeeId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_employees_AuthorizedByEmployeeId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_employees_PerformedByEmployeeId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "employee_benefits");

            migrationBuilder.DropTable(
                name: "employee_documents");

            migrationBuilder.DropTable(
                name: "employee_job_history");

            migrationBuilder.DropTable(
                name: "employee_sessions");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "job_positions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "AccessProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_employees",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_email",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_establishment_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_job_position_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_status",
                table: "employees");

            migrationBuilder.DeleteData(
                table: "AccessLevels",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "AccessLevels",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "AccessLevels",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "AccessLevels",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"));

            migrationBuilder.DropColumn(
                name: "BankAccount",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "BankAccountDigit",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "BankAccountType",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "BankBranch",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "City",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Complement",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Ctps",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "CtpsIssueDate",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "CtpsSeries",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "CtpsUf",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "DependentsCount",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "DriverLicense",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "DriverLicenseCategory",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "DriverLicenseExpiry",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactRelationship",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "JobPositionId",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "MilitaryService",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "PasswordAlgorithm",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "PlaceOfBirth",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "ProbationEndDate",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "RgIssueDate",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "RgIssuer",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Salary",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "SocialName",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "State",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "StatusNotes",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "VoterRegistration",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "WorkShift",
                table: "employees");

            migrationBuilder.RenameTable(
                name: "employees",
                newName: "Employees");

            migrationBuilder.RenameColumn(
                name: "TwoFactorEnabled",
                table: "Employees",
                newName: "RequiresTwoFactor");

            migrationBuilder.RenameColumn(
                name: "Street",
                table: "Employees",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "RequirePasswordChange",
                table: "Employees",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "PisPasep",
                table: "Employees",
                newName: "Crf");

            migrationBuilder.RenameColumn(
                name: "PasswordLastChanged",
                table: "Employees",
                newName: "LastLoginAt");

            migrationBuilder.RenameColumn(
                name: "PasswordCreatedAt",
                table: "Employees",
                newName: "HiredDate");

            migrationBuilder.RenameColumn(
                name: "LockedUntil",
                table: "Employees",
                newName: "CrfExpiryDate");

            migrationBuilder.RenameIndex(
                name: "ix_employees_cpf",
                table: "Employees",
                newName: "IX_Employees_Cpf");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TerminationDate",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanApproveFormulas",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanApproveQuality",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanControlStock",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanHandleControlledSubstances",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManipulate",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanSell",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CrfState",
                table: "Employees",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecret",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AccessLevels",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Employees",
                table: "Employees",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "EmployeeSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSessions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Crf",
                table: "Employees",
                column: "Crf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EstablishmentId_IsActive",
                table: "Employees",
                columns: new[] { "EstablishmentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSessions_EmployeeId_IsActive",
                table: "EmployeeSessions",
                columns: new[] { "EmployeeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSessions_ExpiresAt",
                table: "EmployeeSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSessions_Token",
                table: "EmployeeSessions",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Establishments_EstablishmentId",
                table: "Employees",
                column: "EstablishmentId",
                principalTable: "Establishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_Employees_ApprovedByPharmacistId",
                table: "ManipulationOrders",
                column: "ApprovedByPharmacistId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_Employees_CheckedByEmployeeId",
                table: "ManipulationOrders",
                column: "CheckedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_Employees_ManipulatedByEmployeeId",
                table: "ManipulationOrders",
                column: "ManipulatedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManipulationOrders_Employees_RequestedByEmployeeId",
                table: "ManipulationOrders",
                column: "RequestedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Employees_AuthorizedByPharmacistId",
                table: "Sales",
                column: "AuthorizedByPharmacistId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Employees_SoldByEmployeeId",
                table: "Sales",
                column: "SoldByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Employees_AuthorizedByEmployeeId",
                table: "StockMovements",
                column: "AuthorizedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Employees_PerformedByEmployeeId",
                table: "StockMovements",
                column: "PerformedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
