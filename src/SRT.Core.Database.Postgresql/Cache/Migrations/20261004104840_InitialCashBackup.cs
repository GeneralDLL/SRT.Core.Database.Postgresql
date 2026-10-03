using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SRT.Core.Database.Postgresql.Cache.Migrations
{
    /// <inheritdoc />
    public partial class InitialCashBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashBackup",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    serviceID = table.Column<int>(type: "integer", nullable: false),
                    serviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    key = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    value1 = table.Column<string>(type: "character varying(3990)", maxLength: 3990, nullable: true),
                    value2 = table.Column<string>(type: "character varying(3990)", maxLength: 3990, nullable: true),
                    value3 = table.Column<string>(type: "text", nullable: true),
                    dateCreate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    dateExpire = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashBackup", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashBackup_key",
                table: "CashBackup",
                column: "key");

            migrationBuilder.CreateIndex(
                name: "IX_CashBackup_serviceID",
                table: "CashBackup",
                column: "serviceID");

            migrationBuilder.CreateIndex(
                name: "IX_CashBackup_serviceName",
                table: "CashBackup",
                column: "serviceName");

            migrationBuilder.CreateIndex(
                name: "UX_CashBackup_serviceID_category_key",
                table: "CashBackup",
                columns: new[] { "serviceID", "category", "key" },
                unique: true,
                filter: "\"category\" IS NOT NULL AND \"key\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashBackup");
        }
    }
}
