using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteGuardEdge.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "detection_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VideoSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FrameTimestamp = table.Column<TimeSpan>(type: "time", nullable: true),
                    PPE_Detected = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PPE_Missing = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComplianceStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceScore = table.Column<float>(type: "real", nullable: false),
                    BoundingBoxCoordinates = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotPath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detection_logs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detection_logs");
        }
    }
}
