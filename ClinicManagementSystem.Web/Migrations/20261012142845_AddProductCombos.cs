using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCombos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCombo",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ComboComponent",
                columns: table => new
                {
                    ComboProductId = table.Column<long>(type: "bigint", nullable: false),
                    ComponentProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboComponent", x => new { x.ComboProductId, x.ComponentProductId });
                    table.CheckConstraint("CK_ComboComponent_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_ComboComponent_Products_ComboProductId",
                        column: x => x.ComboProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComboComponent_Products_ComponentProductId",
                        column: x => x.ComponentProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComboComponent_ComponentProductId",
                table: "ComboComponent",
                column: "ComponentProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComboComponent");

            migrationBuilder.DropColumn(
                name: "IsCombo",
                table: "Products");
        }
    }
}
