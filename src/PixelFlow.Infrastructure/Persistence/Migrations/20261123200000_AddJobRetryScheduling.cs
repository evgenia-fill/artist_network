using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PixelFlow.Infrastructure.Persistence.Migrations;

public partial class AddJobRetryScheduling : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "next_attempt_at",
            table: "jobs",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_jobs_status_next_attempt_at",
            table: "jobs",
            columns: new[] { "status", "next_attempt_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_jobs_status_next_attempt_at",
            table: "jobs");

        migrationBuilder.DropColumn(
            name: "next_attempt_at",
            table: "jobs");
    }
}
