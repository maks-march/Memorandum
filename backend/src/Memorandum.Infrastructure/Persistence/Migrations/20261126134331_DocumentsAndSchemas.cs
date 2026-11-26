using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Memorandum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DocumentsAndSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_document_sections_project_documents",
                table: "document_sections");

            migrationBuilder.DropTable(
                name: "project_documents");

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    schema_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    responsible_user_id = table.Column<long>(type: "bigint", nullable: true),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    check_error_count = table.Column<int>(type: "integer", nullable: false),
                    check_warning_count = table.Column<int>(type: "integer", nullable: false),
                    check_errors_json = table.Column<string>(type: "jsonb", nullable: true),
                    xml_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    xml_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    xml_valid = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_documents_projects",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schema_versions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schema_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_imports",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<long>(type: "bigint", nullable: false),
                    detected_schema_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    original_file_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    original_file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    original_file_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    imported_fields = table.Column<int>(type: "integer", nullable: false),
                    warnings_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_imports", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_imports_documents",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_versions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<long>(type: "bigint", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    data_json = table.Column<string>(type: "jsonb", nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_auto = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_versions_documents",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schema_fields",
                columns: table => new
                {
                    field_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    schema_version_id = table.Column<long>(type: "bigint", nullable: false),
                    section = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    dictionary = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    hint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    field_group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xml_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schema_fields", x => new { x.schema_version_id, x.field_id });
                    table.ForeignKey(
                        name: "FK_schema_fields_schema_versions_schema_version_id",
                        column: x => x.schema_version_id,
                        principalTable: "schema_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schema_files",
                columns: table => new
                {
                    path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    schema_version_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    stored_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schema_files", x => new { x.schema_version_id, x.path });
                    table.ForeignKey(
                        name: "FK_schema_files_schema_versions_schema_version_id",
                        column: x => x.schema_version_id,
                        principalTable: "schema_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_document_imports_document_id",
                table: "document_imports",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ux_document_versions_document_id_number",
                table: "document_versions",
                columns: new[] { "document_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documents_project_id_is_archived",
                table: "documents",
                columns: new[] { "project_id", "is_archived" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_status_updated_at",
                table: "documents",
                columns: new[] { "status", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ux_schema_versions_code",
                table: "schema_versions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_schema_versions_is_default",
                table: "schema_versions",
                column: "is_default",
                unique: true,
                filter: "is_default");

            migrationBuilder.AddForeignKey(
                name: "fk_document_sections_documents",
                table: "document_sections",
                column: "document_id",
                principalTable: "documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_document_sections_documents",
                table: "document_sections");

            migrationBuilder.DropTable(
                name: "document_imports");

            migrationBuilder.DropTable(
                name: "document_versions");

            migrationBuilder.DropTable(
                name: "schema_fields");

            migrationBuilder.DropTable(
                name: "schema_files");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "schema_versions");

            migrationBuilder.CreateTable(
                name: "project_documents",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    schema_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_documents_projects",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_project_documents_project_id_is_archived",
                table: "project_documents",
                columns: new[] { "project_id", "is_archived" });

            migrationBuilder.AddForeignKey(
                name: "fk_document_sections_project_documents",
                table: "document_sections",
                column: "document_id",
                principalTable: "project_documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
