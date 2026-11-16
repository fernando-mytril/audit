using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mytril.MaterialId.Catalogo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalogo");

            migrationBuilder.CreateTable(
                name: "fabricantes",
                schema: "catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    fonte = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fabricantes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itens",
                schema: "catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fabricante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    familia = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    acabamento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    espessura_mm = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    diametro_mm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    largura_mm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    altura_mm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    aba_mm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    bitola = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tipo_peso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor_peso = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    comprimento_padrao_mm = table.Column<decimal>(type: "numeric(8,1)", precision: 8, scale: 1, nullable: true),
                    pecas_por_amarrado = table.Column<int>(type: "integer", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    requer_revisao = table.Column<bool>(type: "boolean", nullable: false),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itens", x => x.id);
                    table.ForeignKey(
                        name: "fk_itens_fabricantes_fabricante_id",
                        column: x => x.fabricante_id,
                        principalSchema: "catalogo",
                        principalTable: "fabricantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fabricantes_nome",
                schema: "catalogo",
                table: "fabricantes",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_itens_fabricante_id_familia",
                schema: "catalogo",
                table: "itens",
                columns: new[] { "fabricante_id", "familia" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens",
                schema: "catalogo");

            migrationBuilder.DropTable(
                name: "fabricantes",
                schema: "catalogo");
        }
    }
}
