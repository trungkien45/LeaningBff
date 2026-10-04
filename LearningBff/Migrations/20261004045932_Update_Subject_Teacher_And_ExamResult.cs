using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningBff.Migrations
{
    /// <inheritdoc />
    public partial class Update_Subject_Teacher_And_ExamResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExamTitle",
                table: "AppExamResults",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppSubjectTeachers",
                columns: table => new
                {
                    SubjectId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSubjectTeachers", x => new { x.SubjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_AppSubjectTeachers_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppSubjectTeachers_AppSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "AppSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSubjectTeachers_UserId",
                table: "AppSubjectTeachers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSubjectTeachers");

            migrationBuilder.DropColumn(
                name: "ExamTitle",
                table: "AppExamResults");
        }
    }
}
