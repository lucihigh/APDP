using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using Microsoft.AspNetCore.Authorization;
using SIMS.Services;
using Microsoft.AspNetCore.Identity;

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin,Faculty")]
    public class EnrollmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifier;
        private readonly UserManager<Microsoft.AspNetCore.Identity.IdentityUser> _userManager;

        public EnrollmentsController(ApplicationDbContext context, INotificationService notifier, UserManager<Microsoft.AspNetCore.Identity.IdentityUser> userManager)
        {
            _context = context;
            _notifier = notifier;
            _userManager = userManager;
        }

        // GET: Enrollments
        public async Task<IActionResult> Index(string? courseCode, string? semester, string? student)
        {
            var query = _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                .Include(e => e.Student)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(courseCode))
            {
                query = query.Where(e => e.Course!.Code.Contains(courseCode));
            }

            if (!string.IsNullOrWhiteSpace(semester))
            {
                query = query.Where(e => e.Semester != null && e.Semester.Contains(semester));
            }

            if (!string.IsNullOrWhiteSpace(student))
            {
                var term = student.Trim();
                query = query.Where(e =>
                    (e.Student!.Email != null && e.Student.Email.Contains(term)) ||
                    (e.Student.FirstName + " " + e.Student.LastName).Contains(term));
            }

            ViewBag.CourseCode = courseCode;
            ViewBag.Semester = semester;
            ViewBag.Student = student;
            ViewBag.CourseOptions = await _context.Courses
                .OrderBy(c => c.Code)
                .Select(c => new SelectListItem { Value = c.Code, Text = $"{c.Code} - {c.Name}" })
                .ToListAsync();

            return View(await query.ToListAsync());
        }

        // GET: Enrollments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (enrollment == null)
            {
                return NotFound();
            }

            return View(enrollment);
        }

        // GET: Enrollments/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code");
            ViewData["StudentId"] = new SelectList(_context.Students.Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName }), "Id", "Name");
            return View();
        }

        // GET: Enrollments/BulkCreate
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkCreate()
        {
            var vm = new EnrollmentBulkCreateViewModel
            {
                Courses = await BuildCourseSelectListAsync(),
                Students = await BuildStudentSelectListAsync()
            };
            return View(vm);
        }

        // POST: Enrollments/BulkCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkCreate(EnrollmentBulkCreateViewModel model)
        {
            if (model.StudentIds == null || !model.StudentIds.Any())
            {
                ModelState.AddModelError(nameof(model.StudentIds), "Select at least one student");
            }

            var distinctStudentIds = model.StudentIds?.Distinct().ToList() ?? new List<int>();
            var validStudentIds = await _context.Students
                .Where(s => distinctStudentIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            if (distinctStudentIds.Any() && validStudentIds.Count != distinctStudentIds.Count)
            {
                ModelState.AddModelError(nameof(model.StudentIds), "One or more selected students were not found.");
            }

            if (!await _context.Courses.AnyAsync(c => c.Id == model.CourseId))
            {
                ModelState.AddModelError(nameof(model.CourseId), "Course not found.");
            }

            if (!ModelState.IsValid)
            {
                model.Courses = await BuildCourseSelectListAsync();
                model.Students = await BuildStudentSelectListAsync();
                return View(model);
            }

            var existingStudentIds = new HashSet<int>(await _context.Enrollments
                .Where(e => e.CourseId == model.CourseId && validStudentIds.Contains(e.StudentId))
                .Select(e => e.StudentId)
                .ToListAsync());

            var newEnrollments = validStudentIds
                .Where(id => !existingStudentIds.Contains(id))
                .Select(id => new Enrollment
                {
                    StudentId = id,
                    CourseId = model.CourseId,
                    Semester = model.Semester,
                    Grade = model.Grade
                })
                .ToList();

            if (newEnrollments.Any())
            {
                _context.Enrollments.AddRange(newEnrollments);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Created {newEnrollments.Count} enrollment(s).";
            }

            var skipped = validStudentIds.Count - newEnrollments.Count;
            if (skipped > 0)
            {
                TempData["Info"] = $"Skipped {skipped} already-enrolled student(s).";
            }
            else if (!newEnrollments.Any())
            {
                TempData["Info"] = "All selected students are already enrolled in this course.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Enrollments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,StudentId,CourseId,Semester,Grade")] Enrollment enrollment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(enrollment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code", enrollment.CourseId);
            ViewData["StudentId"] = new SelectList(_context.Students.Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName }), "Id", "Name", enrollment.StudentId);
            return View(enrollment);
        }

        // GET: Enrollments/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code", enrollment.CourseId);
            ViewData["StudentId"] = new SelectList(_context.Students.Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName }), "Id", "Name", enrollment.StudentId);
            return View(enrollment);
        }

        // POST: Enrollments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,CourseId,Semester,Grade")] Enrollment enrollment)
        {
            if (id != enrollment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(enrollment);
                    await _context.SaveChangesAsync();
                    // notify the student about grade updates
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == enrollment.StudentId);
                    if (student?.UserId != null && !string.IsNullOrWhiteSpace(enrollment.Grade))
                    {
                        await _notifier.NotifyUserAsync(student.UserId, $"Your grade for {(_context.Courses.Find(enrollment.CourseId)?.Code)} was updated to {enrollment.Grade}", $"/Students/MyGrades");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EnrollmentExists(enrollment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code", enrollment.CourseId);
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Email", enrollment.StudentId);
            return View(enrollment);
        }

        // GET: Enrollments/Grade/5
        public async Task<IActionResult> Grade(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (enrollment == null)
            {
                return NotFound();
            }

            return View(enrollment);
        }

        // POST: Enrollments/Grade/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(int id, [Bind("Id,Grade")] Enrollment input)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (enrollment == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(enrollment);
            }

            enrollment.Grade = input.Grade;
            _context.Update(enrollment);
            await _context.SaveChangesAsync();

            if (enrollment.Student?.UserId != null && !string.IsNullOrWhiteSpace(enrollment.Grade))
            {
                await _notifier.NotifyUserAsync(enrollment.Student.UserId, $"Your grade for {enrollment.Course?.Code} was updated to {enrollment.Grade}", "/Students/MyGrades");
            }

            TempData["Success"] = "Grade saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Enrollments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (enrollment == null)
            {
                return NotFound();
            }

            return View(enrollment);
        }

        // POST: Enrollments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EnrollmentExists(int id)
        {
            return _context.Enrollments.Any(e => e.Id == id);
        }

        private async Task<List<SelectListItem>> BuildCourseSelectListAsync()
        {
            return await _context.Courses
                .OrderBy(c => c.Code)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Code} - {c.Name}"
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> BuildStudentSelectListAsync()
        {
            return await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.FirstName} {s.LastName} ({s.Email})"
                })
                .ToListAsync();
        }
    }
}
