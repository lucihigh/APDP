using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SIMS.Models;
using Xunit;

namespace SIMS.Tests;

public class EnrollmentValidationTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var ctx = new ValidationContext(model, null, null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, ctx, results, true);
        return results;
    }

    [Fact]
    public void ValidEnrollment_PassesValidation()
    {
        var model = new Enrollment
        {
            StudentId = 1,
            CourseId = 2,
            Semester = "2025S1",
            Grade = "A"
        };

        var results = Validate(model);
        Assert.Empty(results);
    }

    [Fact]
    public void GradeTooLong_FailsValidation()
    {
        var model = new Enrollment
        {
            StudentId = 1,
            CourseId = 2,
            Semester = "2025S1",
            Grade = "ABCDEZ" // > 5 chars
        };

        var results = Validate(model);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Enrollment.Grade)));
    }

    [Fact]
    public void SemesterTooLong_FailsValidation()
    {
        var model = new Enrollment
        {
            StudentId = 1,
            CourseId = 2,
            Semester = new string('X', 25),
            Grade = "B"
        };

        var results = Validate(model);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Enrollment.Semester)));
    }

    [Fact]
    public void MissingStudentId_AllowsDefaultButSemanticsCanBeChecked()
    {
        var model = new Enrollment
        {
            CourseId = 2,
            Semester = "2025S1",
            Grade = "A"
        };

        var results = Validate(model);
        Assert.Empty(results); // Required on non-nullable int is always satisfied; document assumption
        Assert.Equal(0, model.StudentId);
    }

    [Fact]
    public void NullGrade_IsAllowed()
    {
        var model = new Enrollment
        {
            StudentId = 1,
            CourseId = 2,
            Semester = "2025S1",
            Grade = null
        };

        var results = Validate(model);
        Assert.Empty(results);
    }
}
