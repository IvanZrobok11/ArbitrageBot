using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserConfigurations",
                columns: table => new
                {
                    TelegramUserId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Budget = table.Column<int>(type: "INTEGER", nullable: false),
                    MinChanceToBuy = table.Column<byte>(type: "INTEGER", nullable: false),
                    MinChangeToSell = table.Column<byte>(type: "INTEGER", nullable: false),
                    ExceptedProfit = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfigurations", x => x.TelegramUserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConfigurations");
        }
    }
}
