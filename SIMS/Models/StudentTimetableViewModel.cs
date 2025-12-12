using System.Collections.Generic;

namespace SIMS.Models;

public class StudentTimetableViewModel
{
    public List<StudentTimetableCourseViewModel> Courses { get; set; } = new();

    public static IReadOnlyList<StudentTimetableDayViewModel> Days { get; } = new List<StudentTimetableDayViewModel>
    {
        new(1, "Thứ 2"),
        new(2, "Thứ 3"),
        new(3, "Thứ 4"),
        new(4, "Thứ 5"),
        new(5, "Thứ 6"),
        new(6, "Thứ 7"),
        new(0, "Chủ nhật")
    };

    public static IReadOnlyList<StudentTimetableSlotViewModel> Slots { get; } = new List<StudentTimetableSlotViewModel>
    {
        new(1, "07:00", "09:00"),
        new(2, "09:00", "11:00"),
        new(3, "12:00", "14:00"),
        new(4, "14:00", "16:00"),
        new(5, "16:00", "18:00"),
        new(6, "18:00", "20:00")
    };
}

public record StudentTimetableDayViewModel(int Value, string Label);

public record StudentTimetableSlotViewModel(int Slot, string Start, string End)
{
    public string Label => $"Ca {Slot}";
    public string TimeRange => $"{Start} - {End}";
}

public class StudentTimetableCourseViewModel
{
    public int CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<StudentTimetableSessionViewModel> Sessions { get; set; } = new();
}

public class StudentTimetableSessionViewModel
{
    public int DayOfWeek { get; set; }
    public int SessionSlot { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Location { get; set; }
}

