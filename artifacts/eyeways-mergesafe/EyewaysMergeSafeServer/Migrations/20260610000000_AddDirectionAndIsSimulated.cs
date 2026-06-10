using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyewaysMergeSafeServer.Migrations;

public partial class AddDirectionAndIsSimulated : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Direction",
            table: "VehicleEvents",
            type: "TEXT",
            maxLength: 2,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsSimulated",
            table: "VehicleEvents",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Direction",    table: "VehicleEvents");
        migrationBuilder.DropColumn(name: "IsSimulated",  table: "VehicleEvents");
    }
}
