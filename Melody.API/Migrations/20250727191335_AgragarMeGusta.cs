using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Melody.API.Migrations
{
    /// <inheritdoc />
    public partial class AgragarMeGusta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeGustas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CancionId = table.Column<int>(type: "int", nullable: false),
                    FechaAgregado = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeGustas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeGustas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeGustas_Canciones_CancionId",
                        column: x => x.CancionId,
                        principalTable: "Canciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                  name: "SuscripcionMiembros",
                  columns: table => new
                  {
                      Id = table.Column<int>(type: "int", nullable: false)
                          .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                      SuscripcionId = table.Column<int>(type: "int", nullable: false),
                      UsuarioId = table.Column<int>(type: "int", nullable: false),
                      FechaUnion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                      EsActivo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                      Rol = table.Column<string>(type: "longtext", nullable: true)
                          .Annotation("MySql:CharSet", "utf8mb4")
                  },
                  constraints: table =>
                  {
                      table.PrimaryKey("PK_SuscripcionMiembros", x => x.Id);
                      table.ForeignKey(
                          name: "FK_SuscripcionMiembros_AspNetUsers_UsuarioId",
                          column: x => x.UsuarioId,
                          principalTable: "AspNetUsers",
                          principalColumn: "Id",
                          onDelete: ReferentialAction.Cascade);
                      table.ForeignKey(
                          name: "FK_SuscripcionMiembros_Suscripciones_SuscripcionId",
                          column: x => x.SuscripcionId,
                          principalTable: "Suscripciones",
                          principalColumn: "Id",
                          onDelete: ReferentialAction.Cascade);
                  })
                  .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MeGustas_CancionId",
                table: "MeGustas",
                column: "CancionId");

            migrationBuilder.CreateIndex(
                name: "IX_MeGustas_UsuarioId",
                table: "MeGustas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SuscripcionMiembros_SuscripcionId",
                table: "SuscripcionMiembros",
                column: "SuscripcionId");

            migrationBuilder.CreateIndex(
                name: "IX_SuscripcionMiembros_UsuarioId",
                table: "SuscripcionMiembros",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeGustas");

            migrationBuilder.DropTable(
                name: "SuscripcionMiembros");
        }
    }
}
