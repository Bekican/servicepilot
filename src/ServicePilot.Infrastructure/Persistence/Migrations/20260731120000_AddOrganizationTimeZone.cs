using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using ServicePilot.Infrastructure.Persistence;

#nullable disable

namespace ServicePilot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ServicePilotDbContext))]
[Migration("20260731120000_AddOrganizationTimeZone")]
public partial class AddOrganizationTimeZone : Migration
{
    protected override void Up(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "time_zone_id",
            table: "organizations",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "UTC");
    }

    protected override void Down(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "time_zone_id",
            table: "organizations");
    }
}