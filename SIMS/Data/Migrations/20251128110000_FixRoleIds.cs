using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SIMS.Data;

#nullable disable

namespace SIMS.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20251128110000_FixRoleIds")]
    public partial class FixRoleIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Admin role -> AD
                DECLARE @AdminId nvarchar(128) = 'AD';
                DECLARE @AdminName nvarchar(256) = 'Admin';
                DECLARE @AdminNorm nvarchar(256) = 'ADMIN';
                DECLARE @ExistingAdminId nvarchar(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE NormalizedName = @AdminNorm);

                IF @ExistingAdminId IS NOT NULL AND @ExistingAdminId <> @AdminId
                BEGIN
                    UPDATE AspNetRoles SET NormalizedName = CONCAT(@AdminNorm, '_OLD_', CONVERT(varchar(36), NEWID())) WHERE Id = @ExistingAdminId;
                END

                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Id = @AdminId)
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (@AdminId, @AdminName, @AdminNorm, NEWID());
                END
                ELSE
                BEGIN
                    UPDATE AspNetRoles SET Name = @AdminName, NormalizedName = @AdminNorm WHERE Id = @AdminId;
                END

                IF @ExistingAdminId IS NOT NULL AND @ExistingAdminId <> @AdminId
                BEGIN
                    UPDATE AspNetUserRoles SET RoleId = @AdminId WHERE RoleId = @ExistingAdminId;
                    UPDATE AspNetRoleClaims SET RoleId = @AdminId WHERE RoleId = @ExistingAdminId;
                    DELETE FROM AspNetRoles WHERE Id = @ExistingAdminId;
                END

                -- Faculty role -> GV
                DECLARE @FacultyId nvarchar(128) = 'GV';
                DECLARE @FacultyName nvarchar(256) = 'Faculty';
                DECLARE @FacultyNorm nvarchar(256) = 'FACULTY';
                DECLARE @ExistingFacultyId nvarchar(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE NormalizedName = @FacultyNorm);

                IF @ExistingFacultyId IS NOT NULL AND @ExistingFacultyId <> @FacultyId
                BEGIN
                    UPDATE AspNetRoles SET NormalizedName = CONCAT(@FacultyNorm, '_OLD_', CONVERT(varchar(36), NEWID())) WHERE Id = @ExistingFacultyId;
                END

                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Id = @FacultyId)
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (@FacultyId, @FacultyName, @FacultyNorm, NEWID());
                END
                ELSE
                BEGIN
                    UPDATE AspNetRoles SET Name = @FacultyName, NormalizedName = @FacultyNorm WHERE Id = @FacultyId;
                END

                IF @ExistingFacultyId IS NOT NULL AND @ExistingFacultyId <> @FacultyId
                BEGIN
                    UPDATE AspNetUserRoles SET RoleId = @FacultyId WHERE RoleId = @ExistingFacultyId;
                    UPDATE AspNetRoleClaims SET RoleId = @FacultyId WHERE RoleId = @ExistingFacultyId;
                    DELETE FROM AspNetRoles WHERE Id = @ExistingFacultyId;
                END

                -- Student role -> BH
                DECLARE @StudentId nvarchar(128) = 'BH';
                DECLARE @StudentName nvarchar(256) = 'Student';
                DECLARE @StudentNorm nvarchar(256) = 'STUDENT';
                DECLARE @ExistingStudentId nvarchar(128) = (SELECT TOP 1 Id FROM AspNetRoles WHERE NormalizedName = @StudentNorm);

                IF @ExistingStudentId IS NOT NULL AND @ExistingStudentId <> @StudentId
                BEGIN
                    UPDATE AspNetRoles SET NormalizedName = CONCAT(@StudentNorm, '_OLD_', CONVERT(varchar(36), NEWID())) WHERE Id = @ExistingStudentId;
                END

                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Id = @StudentId)
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (@StudentId, @StudentName, @StudentNorm, NEWID());
                END
                ELSE
                BEGIN
                    UPDATE AspNetRoles SET Name = @StudentName, NormalizedName = @StudentNorm WHERE Id = @StudentId;
                END

                IF @ExistingStudentId IS NOT NULL AND @ExistingStudentId <> @StudentId
                BEGIN
                    UPDATE AspNetUserRoles SET RoleId = @StudentId WHERE RoleId = @ExistingStudentId;
                    UPDATE AspNetRoleClaims SET RoleId = @StudentId WHERE RoleId = @ExistingStudentId;
                    DELETE FROM AspNetRoles WHERE Id = @ExistingStudentId;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty; fixed role IDs should remain stable.
        }

        /// <summary>
        /// Reuse the existing snapshot so EF tooling still has a target model for this migration.
        /// </summary>
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            Snapshot.Build(modelBuilder);
        }

        private sealed class Snapshot : ApplicationDbContextModelSnapshot
        {
            public static void Build(ModelBuilder modelBuilder) => new Snapshot().BuildModel(modelBuilder);
        }
    }
}
