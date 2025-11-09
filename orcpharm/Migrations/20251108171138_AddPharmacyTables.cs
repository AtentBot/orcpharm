using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <inheritdoc />
    public partial class AddPharmacyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Establishments_AccessLevel_AccessLevelId",
                table: "Establishments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessLevel",
                table: "AccessLevel");

            migrationBuilder.RenameTable(
                name: "AccessLevel",
                newName: "AccessLevels");

            migrationBuilder.RenameIndex(
                name: "IX_AccessLevel_Code",
                table: "AccessLevels",
                newName: "IX_AccessLevels_Code");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Establishments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Establishments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "AccessLevels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AccessLevels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AccessLevels",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AccessLevels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessLevels",
                table: "AccessLevels",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Establishments_AccessLevels_AccessLevelId",
                table: "Establishments",
                column: "AccessLevelId",
                principalTable: "AccessLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Establishments_AccessLevels_AccessLevelId",
                table: "Establishments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessLevels",
                table: "AccessLevels");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "AccessLevels");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AccessLevels");

            migrationBuilder.RenameTable(
                name: "AccessLevels",
                newName: "AccessLevel");

            migrationBuilder.RenameIndex(
                name: "IX_AccessLevels_Code",
                table: "AccessLevel",
                newName: "IX_AccessLevel_Code");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Establishments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Establishments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "AccessLevel",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AccessLevel",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessLevel",
                table: "AccessLevel",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Establishments_AccessLevel_AccessLevelId",
                table: "Establishments",
                column: "AccessLevelId",
                principalTable: "AccessLevel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
