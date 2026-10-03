using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SRT.Core.Database.Postgresql.ErrorLog.Migrations
{
    /// <inheritdoc />
    public partial class InitialErrorReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorReports",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    serviceID = table.Column<int>(type: "integer", nullable: false),
                    serviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Message1 = table.Column<string>(type: "text", nullable: true),
                    Message2 = table.Column<string>(type: "text", nullable: true),
                    Message3 = table.Column<string>(type: "text", nullable: true),
                    Message4 = table.Column<string>(type: "text", nullable: true),
                    Message5 = table.Column<string>(type: "text", nullable: true),
                    StackTrace1 = table.Column<string>(type: "text", nullable: true),
                    StackTrace2 = table.Column<string>(type: "text", nullable: true),
                    StackTrace3 = table.Column<string>(type: "text", nullable: true),
                    StackTrace4 = table.Column<string>(type: "text", nullable: true),
                    StackTrace5 = table.Column<string>(type: "text", nullable: true),
                    layer1 = table.Column<string>(type: "text", nullable: true),
                    layer2 = table.Column<string>(type: "text", nullable: true),
                    layer3 = table.Column<string>(type: "text", nullable: true),
                    layer4 = table.Column<string>(type: "text", nullable: true),
                    layer5 = table.Column<string>(type: "text", nullable: true),
                    layer6 = table.Column<string>(type: "text", nullable: true),
                    layer7 = table.Column<string>(type: "text", nullable: true),
                    layer8 = table.Column<string>(type: "text", nullable: true),
                    layer9 = table.Column<string>(type: "text", nullable: true),
                    layer10 = table.Column<string>(type: "text", nullable: true),
                    layer11 = table.Column<string>(type: "text", nullable: true),
                    layer12 = table.Column<string>(type: "text", nullable: true),
                    layer13 = table.Column<string>(type: "text", nullable: true),
                    layer14 = table.Column<string>(type: "text", nullable: true),
                    layer15 = table.Column<string>(type: "text", nullable: true),
                    layer16 = table.Column<string>(type: "text", nullable: true),
                    layer17 = table.Column<string>(type: "text", nullable: true),
                    layer18 = table.Column<string>(type: "text", nullable: true),
                    layer19 = table.Column<string>(type: "text", nullable: true),
                    layer20 = table.Column<string>(type: "text", nullable: true),
                    ErrorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Service = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ExceptionType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Target = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DetailJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorReports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorReports_category",
                table: "ErrorReports",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorReports_Category_date",
                table: "ErrorReports",
                columns: new[] { "Category", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorReports_CorrelationId",
                table: "ErrorReports",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorReports_ErrorId",
                table: "ErrorReports",
                column: "ErrorId",
                unique: true,
                filter: "\"ErrorId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorReports_serviceID",
                table: "ErrorReports",
                column: "serviceID");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorReports_serviceName",
                table: "ErrorReports",
                column: "serviceName");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorReports_Target",
                table: "ErrorReports",
                column: "Target");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorReports");
        }
    }
}
