using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fintacharts.AssetTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnAssetPriceInstrumentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asset_prices_instrument_id",
                table: "asset_prices");

            migrationBuilder.CreateIndex(
                name: "ix_asset_prices_instrument_id",
                table: "asset_prices",
                column: "instrument_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asset_prices_instrument_id",
                table: "asset_prices");

            migrationBuilder.CreateIndex(
                name: "ix_asset_prices_instrument_id",
                table: "asset_prices",
                column: "instrument_id");
        }
    }
}
