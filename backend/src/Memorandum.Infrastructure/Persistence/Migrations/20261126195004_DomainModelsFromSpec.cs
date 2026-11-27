using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Memorandum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DomainModelsFromSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_sections");

            migrationBuilder.DropIndex(
                name: "ix_documents_project_id_is_archived",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "ix_document_imports_document_id",
                table: "document_imports");

            migrationBuilder.DropColumn(
                name: "check_error_count",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "check_errors_json",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "check_warning_count",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "is_archived",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "schema_code",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "xml_sha256",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "xml_size_bytes",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "xml_valid",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "original_file_key",
                table: "document_imports");

            migrationBuilder.DropColumn(
                name: "original_file_sha256",
                table: "document_imports");

            migrationBuilder.DropColumn(
                name: "original_file_size_bytes",
                table: "document_imports");

            migrationBuilder.RenameColumn(
                name: "checked_at",
                table: "documents",
                newName: "deleted_at");

            migrationBuilder.AddColumn<bool>(
                name: "is_service_admin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "compile_message",
                table: "schema_versions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "compiled_at",
                table: "schema_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "entry_point",
                table: "schema_versions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_namespace",
                table: "schema_versions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "object_kind",
                table: "projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "oktmo_code",
                table: "projects",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "oktmo_name",
                table: "projects",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "region_code",
                table: "projects",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_type",
                table: "projects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_foreign",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "nrs_nopriz",
                table: "organizations",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "self_regulatory_organization",
                table: "organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "documents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "data_json",
                table: "documents",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "object_address",
                table: "documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "object_kind",
                table: "documents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "object_name",
                table: "documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "oktmo_code",
                table: "documents",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "oktmo_name",
                table: "documents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "organization_id",
                table: "documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "region_code",
                table: "documents",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "schema_version_id",
                table: "documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "status_before_archiving",
                table: "documents",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_type",
                table: "documents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "document_versions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "warnings_json",
                table: "document_imports",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "document_imports",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<DateTime>(
                name: "imported_at",
                table: "document_imports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "original_file",
                table: "document_imports",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "document_imports",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "app_errors",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    level = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    stack_trace = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    organization_id = table.Column<long>(type: "bigint", nullable: true),
                    repeat_count = table.Column<int>(type: "integer", nullable: false),
                    last_occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_errors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<long>(type: "bigint", nullable: true),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    organization_id = table.Column<long>(type: "bigint", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dictionaries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    parent_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dictionaries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_checks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<long>(type: "bigint", nullable: false),
                    schema_version_id = table.Column<long>(type: "bigint", nullable: false),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ok = table.Column<bool>(type: "boolean", nullable: false),
                    error_count = table.Column<int>(type: "integer", nullable: false),
                    warning_count = table.Column<int>(type: "integer", nullable: false),
                    issues_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_checks", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_checks_documents",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_document_checks_schema_versions",
                        column: x => x.schema_version_id,
                        principalTable: "schema_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_participants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    party_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    full_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    snils = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    nrs_nopriz = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    organization_id = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_participants_documents",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_document_participants_organizations",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_document_participants_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "schema_package_files",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    schema_version_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content = table.Column<byte[]>(type: "bytea", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schema_package_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_schema_package_files_schema_versions",
                        column: x => x.schema_version_id,
                        principalTable: "schema_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "xml_artifacts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<long>(type: "bigint", nullable: false),
                    check_id = table.Column<long>(type: "bigint", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xml_artifacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_xml_artifacts_document_checks",
                        column: x => x.check_id,
                        principalTable: "document_checks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_xml_artifacts_documents",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_documents_organization_id_deleted_at",
                table: "documents",
                columns: new[] { "organization_id", "deleted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_project_id_status",
                table: "documents",
                columns: new[] { "project_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_responsible_user_id",
                table: "documents",
                column: "responsible_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_schema_version_id",
                table: "documents",
                column: "schema_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_imports_document_id_imported_at",
                table: "document_imports",
                columns: new[] { "document_id", "imported_at" });

            migrationBuilder.CreateIndex(
                name: "ix_app_errors_code_last_occurred_at",
                table: "app_errors",
                columns: new[] { "code", "last_occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_entity_entity_id_created_at",
                table: "audit_entries",
                columns: new[] { "entity", "entity_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_organization_id_created_at",
                table: "audit_entries",
                columns: new[] { "organization_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_dictionaries_kind_is_active_sort_order",
                table: "dictionaries",
                columns: new[] { "kind", "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_dictionaries_kind_parent_code",
                table: "dictionaries",
                columns: new[] { "kind", "parent_code" });

            migrationBuilder.CreateIndex(
                name: "ux_dictionaries_kind_code",
                table: "dictionaries",
                columns: new[] { "kind", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_checks_document_id_checked_at",
                table: "document_checks",
                columns: new[] { "document_id", "checked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_document_checks_schema_version_id",
                table: "document_checks",
                column: "schema_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_participants_document_id",
                table: "document_participants",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_participants_organization_id",
                table: "document_participants",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_participants_user_id",
                table: "document_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_document_participants_document_id_kind",
                table: "document_participants",
                columns: new[] { "document_id", "kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_schema_package_files_schema_version_id_path",
                table: "schema_package_files",
                columns: new[] { "schema_version_id", "path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_xml_artifacts_check_id",
                table: "xml_artifacts",
                column: "check_id");

            migrationBuilder.CreateIndex(
                name: "ix_xml_artifacts_document_id_generated_at",
                table: "xml_artifacts",
                columns: new[] { "document_id", "generated_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_documents_organizations",
                table: "documents",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_documents_responsible_user",
                table: "documents",
                column: "responsible_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_documents_schema_versions",
                table: "documents",
                column: "schema_version_id",
                principalTable: "schema_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_documents_organizations",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "fk_documents_responsible_user",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "fk_documents_schema_versions",
                table: "documents");

            migrationBuilder.DropTable(
                name: "app_errors");

            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "dictionaries");

            migrationBuilder.DropTable(
                name: "document_participants");

            migrationBuilder.DropTable(
                name: "schema_package_files");

            migrationBuilder.DropTable(
                name: "xml_artifacts");

            migrationBuilder.DropTable(
                name: "document_checks");

            migrationBuilder.DropIndex(
                name: "ix_documents_organization_id_deleted_at",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "ix_documents_project_id_status",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_responsible_user_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_schema_version_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "ix_document_imports_document_id_imported_at",
                table: "document_imports");

            migrationBuilder.DropColumn(
                name: "is_service_admin",
                table: "users");

            migrationBuilder.DropColumn(
                name: "compile_message",
                table: "schema_versions");

            migrationBuilder.DropColumn(
                name: "compiled_at",
                table: "schema_versions");

            migrationBuilder.DropColumn(
                name: "entry_point",
                table: "schema_versions");

            migrationBuilder.DropColumn(
                name: "target_namespace",
                table: "schema_versions");

            migrationBuilder.DropColumn(
                name: "object_kind",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "oktmo_code",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "oktmo_name",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "region_code",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "work_type",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "is_foreign",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "nrs_nopriz",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "self_regulatory_organization",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "data_json",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "object_address",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "object_kind",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "object_name",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "oktmo_code",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "oktmo_name",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "region_code",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "schema_version_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "status_before_archiving",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "work_type",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "imported_at",
                table: "document_imports");

            migrationBuilder.DropColumn(
                name: "original_file",
                table: "document_imports");

            migrationBuilder.DropColumn(
                name: "status",
                table: "document_imports");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "documents",
                newName: "checked_at");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "check_error_count",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "check_errors_json",
                table: "documents",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "check_warning_count",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "schema_code",
                table: "documents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "xml_sha256",
                table: "documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "xml_size_bytes",
                table: "documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "xml_valid",
                table: "documents",
                type: "boolean",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "document_versions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "warnings_json",
                table: "document_imports",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "file_name",
                table: "document_imports",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_file_key",
                table: "document_imports",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_file_sha256",
                table: "document_imports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "original_file_size_bytes",
                table: "document_imports",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "document_sections",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_json = table.Column<string>(type: "jsonb", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_sections_documents",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_documents_project_id_is_archived",
                table: "documents",
                columns: new[] { "project_id", "is_archived" });

            migrationBuilder.CreateIndex(
                name: "ix_document_imports_document_id",
                table: "document_imports",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ux_document_sections_document_id_code",
                table: "document_sections",
                columns: new[] { "document_id", "code" },
                unique: true);
        }
    }
}
