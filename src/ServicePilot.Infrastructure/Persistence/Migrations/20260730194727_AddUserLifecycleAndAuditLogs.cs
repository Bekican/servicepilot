using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLifecycleAndAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    anonymized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_user_id",
                table: "audit_logs",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_organization_occurred_at",
                table: "audit_logs",
                columns: new[] { "organization_id", "occurred_at_utc" });

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION prevent_last_active_owner()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF OLD.role = 'Owner'
                       AND OLD.is_active = TRUE
                       AND (
                           NEW.role <> 'Owner'
                           OR NEW.is_active = FALSE
                       )
                    THEN
                        PERFORM 1
                        FROM organizations
                        WHERE id = OLD.organization_id
                        FOR UPDATE;

                        IF NOT EXISTS (
                            SELECT 1
                            FROM users
                            WHERE organization_id = OLD.organization_id
                              AND id <> OLD.id
                              AND role = 'Owner'
                              AND is_active = TRUE
                        )
                        THEN
                            RAISE EXCEPTION
                                'The last active Owner cannot be changed'
                                USING
                                    ERRCODE = '23514',
                                    CONSTRAINT = 'ck_users_last_active_owner';
                        END IF;
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER trg_users_last_active_owner
                BEFORE UPDATE OF role, is_active
                ON users
                FOR EACH ROW
                EXECUTE FUNCTION prevent_last_active_owner();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS
                    trg_users_last_active_owner
                    ON users;

                DROP FUNCTION IF EXISTS
                    prevent_last_active_owner();
                """);

            migrationBuilder.DropTable(
                name: "audit_logs");
        }
    }
}