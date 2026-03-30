using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AudioBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AudioRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationInSeconds = table.Column<int>(type: "integer", nullable: false),
                    RawAudioData = table.Column<byte[]>(type: "bytea", nullable: false),
                    FilteredAudioData = table.Column<byte[]>(type: "bytea", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioRecords");
        }
    }
}
