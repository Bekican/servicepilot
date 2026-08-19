using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicePilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeDocumentChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_knowledge_documents_organization_id_id",
                table: "knowledge_documents",
                columns: new[] { "organization_id", "id" });

            migrationBuilder.CreateTable(
                name: "knowledge_document_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chunk_index = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    embedding_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    embedding_dimensions = table.Column<int>(type: "integer", nullable: false),
                    embedding = table.Column<string>(type: "vector(1024)", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_document_chunks", x => x.id);
                    table.CheckConstraint(
                        "ck_knowledge_document_chunks_chunk_index",
                        "chunk_index >= 0");
                    table.CheckConstraint(
                        "ck_knowledge_document_chunks_page_number",
                        "page_number > 0");
                    table.CheckConstraint(
                        "ck_knowledge_document_chunks_content_length",
                        "char_length(content) BETWEEN 1 AND 1800");
                    table.CheckConstraint(
                        "ck_knowledge_document_chunks_dimensions",
                        "embedding_dimensions = 1024 AND vector_dims(embedding) = 1024");
                    table.ForeignKey(
                        name: "FK_knowledge_document_chunks_knowledge_documents",
                        columns: x => new
                        {
                            x.organization_id,
                            x.document_id
                        },
                        principalTable: "knowledge_documents",
                        principalColumns: new[]
                        {
                            "organization_id",
                            "id"
                        },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_document_chunks_organization_document_page",
                table: "knowledge_document_chunks",
                columns: new[]
                {
                    "organization_id",
                    "document_id",
                    "page_number"
                });

            migrationBuilder.CreateIndex(
                name: "ux_knowledge_document_chunks_organization_document_index",
                table: "knowledge_document_chunks",
                columns: new[]
                {
                    "organization_id",
                    "document_id",
                    "chunk_index"
                },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "knowledge_document_chunks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_knowledge_documents_organization_id_id",
                table: "knowledge_documents");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}