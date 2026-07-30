using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_users_organization_id_id",
                table: "users",
                columns: new[] { "organization_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_services_organization_id_id",
                table: "services",
                columns: new[] { "organization_id", "id" });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    technician_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.id);
                    table.CheckConstraint("ck_appointments_time_range", "end_at_utc > start_at_utc");
                    table.ForeignKey(
                        name: "FK_appointments_customers_organization_id_customer_id",
                        columns: x => new { x.organization_id, x.customer_id },
                        principalTable: "customers",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_appointments_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_appointments_services_organization_id_service_id",
                        columns: x => new { x.organization_id, x.service_id },
                        principalTable: "services",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_appointments_users_organization_id_technician_user_id",
                        columns: x => new { x.organization_id, x.technician_user_id },
                        principalTable: "users",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_organization_id_customer_id",
                table: "appointments",
                columns: new[] { "organization_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_organization_id_service_id",
                table: "appointments",
                columns: new[] { "organization_id", "service_id" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_organization_id_technician_user_id",
                table: "appointments",
                columns: new[] { "organization_id", "technician_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_organization_start",
                table: "appointments",
                columns: new[] { "organization_id", "start_at_utc" });

            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS btree_gist;

                ALTER TABLE appointments
                ADD CONSTRAINT ex_appointments_technician_overlap
                EXCLUDE USING gist (
                    organization_id WITH =,
                    technician_user_id WITH =,
                    tstzrange(
                        start_at_utc,
                        end_at_utc,
                        '[)'
                    ) WITH &&
                )
                WHERE (
                    technician_user_id IS NOT NULL
                    AND status IN (
                        'Scheduled',
                        'Confirmed',
                        'InProgress'
                    )
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_users_organization_id_id",
                table: "users");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_services_organization_id_id",
                table: "services");
        }
    }
}