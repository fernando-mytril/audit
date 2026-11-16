using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mytril.MaterialId.Conferencias.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "conferencias");

            migrationBuilder.CreateTable(
                name: "conferencias",
                schema: "conferencias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    operacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    contraparte = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criada_por = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conferencias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "analises",
                schema: "conferencias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    empresa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conferencia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequencia = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resultado_item_catalogo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resultado_quantidade = table.Column<int>(type: "integer", nullable: true),
                    resultado_comprimento_mm = table.Column<decimal>(type: "numeric(8,1)", precision: 8, scale: 1, nullable: true),
                    resultado_largura_mm = table.Column<decimal>(type: "numeric(8,1)", precision: 8, scale: 1, nullable: true),
                    resultado_peso_kg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    confianca = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    versao_modelo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    correcao_item_catalogo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correcao_quantidade = table.Column<int>(type: "integer", nullable: true),
                    correcao_comprimento_mm = table.Column<decimal>(type: "numeric(8,1)", precision: 8, scale: 1, nullable: true),
                    correcao_largura_mm = table.Column<decimal>(type: "numeric(8,1)", precision: 8, scale: 1, nullable: true),
                    correcao_peso_kg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    motivo_falha = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analises", x => x.id);
                    table.ForeignKey(
                        name: "fk_analises_conferencias_conferencia_id",
                        column: x => x.conferencia_id,
                        principalSchema: "conferencias",
                        principalTable: "conferencias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conferencia_transicoes",
                schema: "conferencias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    de = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    para = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    gatilho = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    usuario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    conferencia_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conferencia_transicoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_conferencia_transicoes_conferencias_conferencia_id",
                        column: x => x.conferencia_id,
                        principalSchema: "conferencias",
                        principalTable: "conferencias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "analise_transicoes",
                schema: "conferencias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    de = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    para = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    gatilho = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    usuario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    analise_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analise_transicoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_analise_transicoes_analises_analise_id",
                        column: x => x.analise_id,
                        principalSchema: "conferencias",
                        principalTable: "analises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analise_transicoes_analise_id",
                schema: "conferencias",
                table: "analise_transicoes",
                column: "analise_id");

            migrationBuilder.CreateIndex(
                name: "ix_analises_conferencia_id",
                schema: "conferencias",
                table: "analises",
                column: "conferencia_id");

            migrationBuilder.CreateIndex(
                name: "ix_analises_empresa_id_status",
                schema: "conferencias",
                table: "analises",
                columns: new[] { "empresa_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_conferencia_transicoes_conferencia_id",
                schema: "conferencias",
                table: "conferencia_transicoes",
                column: "conferencia_id");

            migrationBuilder.CreateIndex(
                name: "ix_conferencias_empresa_id_codigo",
                schema: "conferencias",
                table: "conferencias",
                columns: new[] { "empresa_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conferencias_empresa_id_status",
                schema: "conferencias",
                table: "conferencias",
                columns: new[] { "empresa_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analise_transicoes",
                schema: "conferencias");

            migrationBuilder.DropTable(
                name: "conferencia_transicoes",
                schema: "conferencias");

            migrationBuilder.DropTable(
                name: "analises",
                schema: "conferencias");

            migrationBuilder.DropTable(
                name: "conferencias",
                schema: "conferencias");
        }
    }
}
