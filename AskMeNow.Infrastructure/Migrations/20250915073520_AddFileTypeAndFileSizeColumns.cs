using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskMeNow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileTypeAndFileSizeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConversationId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, defaultValue: "en"),
                    FileType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConversationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sender = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Question = table.Column<string>(type: "TEXT", nullable: true),
                    Answer = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TurnNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Embeddings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChunkIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    TextChunk = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Vector = table.Column<byte[]>(type: "BLOB", nullable: false),
                    VectorDimensions = table.Column<int>(type: "INTEGER", nullable: false),
                    ModelVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "1.0"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Embeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Embeddings_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_TurnNumber",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "TurnNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ConversationId",
                table: "Conversations",
                column: "ConversationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_LastActivityAt",
                table: "Conversations",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FilePath",
                table: "Documents",
                column: "FilePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FileType",
                table: "Documents",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_LastModified",
                table: "Documents",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_DocumentId_ChunkIndex",
                table: "Embeddings",
                columns: new[] { "DocumentId", "ChunkIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Embeddings");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
