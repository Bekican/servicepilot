using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "knowledge_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    checksum_sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    document_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    access_scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    processing_attempt_count = table.Column<int>(type: "integer", nullable: false),
                    processing_started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_documents", x => x.id);
                    table.CheckConstraint("ck_knowledge_documents_size_bytes", "size_bytes > 0");
                    table.ForeignKey(
                        name: "FK_knowledge_documents_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_knowledge_documents_users_organization_id_uploaded_by_user_~",
                        columns: x => new { x.organization_id, x.uploaded_by_user_id },
                        principalTable: "users",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_documents_organization_id_uploaded_by_user_id",
                table: "knowledge_documents",
                columns: new[] { "organization_id", "uploaded_by_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_documents_pending",
                table: "knowledge_documents",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_knowledge_documents_organization_checksum",
                table: "knowledge_documents",
                columns: new[] { "organization_id", "checksum_sha256" },
                unique: true,
                filter: "status <> 'Deleted'");

            migrationBuilder.CreateIndex(
                name: "ux_knowledge_documents_storage_key",
                table: "knowledge_documents",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "knowledge_documents");
        }
    }
}