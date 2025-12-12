using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace SIMS.Controllers
{
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Students
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Index(string? q)
        {
            var studentIds = new HashSet<string>(
                (await _userManager.GetUsersInRoleAsync("Student"))
                    .Select(u => u.Id),
                StringComparer.OrdinalIgnoreCase);

            var query = _context.Students
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.UserId != null && studentIds.Contains(s.UserId))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(s =>
                    s.FirstName.Contains(term) ||
                    s.LastName.Contains(term) ||
                    s.Email.Contains(term) ||
                    (s.Program != null && s.Program.Contains(term)));
            }
            ViewData["q"] = q;
            return View(await query.ToListAsync());
        }

        // GET: Students/Details/5
        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,DateOfBirth,Email,Phone,Address,Program,Year,GPA")] Student input)
        {
            if (input.DateOfBirth is null)
            {
                ModelState.AddModelError(nameof(Student.DateOfBirth), "Date of birth is required to create a login for the student.");
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            if (await _context.Students.AsNoTracking().AnyAsync(s => s.Email == input.Email))
            {
                ModelState.AddModelError(nameof(Student.Email), "A student with this email already exists.");
                return View(input);
            }

            var studentUser = await _userManager.FindByEmailAsync(input.Email);
            string? generatedPassword = null;
            const string role = "Student";

            if (studentUser == null)
            {
                var userId = await UserIdGenerator.GenerateForRoleAsync(_userManager, role);
                studentUser = new IdentityUser
                {
                    Id = userId,
                    UserName = input.Email,
                    Email = input.Email,
                    EmailConfirmed = true
                };

                generatedPassword = StudentPasswordGenerator.Generate(input.FirstName, input.LastName, input.DateOfBirth!.Value);
                var createResult = await _userManager.CreateAsync(studentUser, generatedPassword);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(input);
                }

                await _userManager.AddToRoleAsync(studentUser, role);
            }
            else
            {
                if (!UserIdGenerator.IsFormatted(studentUser.Id, role))
                {
                    studentUser = await UserIdRepairService.RepairSingleStudentAsync(_context, _userManager, studentUser, role);
                }
                if (!await _userManager.IsInRoleAsync(studentUser, role))
                {
                    await _userManager.AddToRoleAsync(studentUser, role);
                }
            }

            var student = new Student
            {
                FirstName = input.FirstName,
                LastName = input.LastName,
                DateOfBirth = input.DateOfBirth,
                Email = input.Email,
                Phone = input.Phone,
                Address = input.Address,
                Program = input.Program,
                Year = input.Year,
                GPA = input.GPA,
                UserId = studentUser.Id
            };

            _context.Add(student);
            await _context.SaveChangesAsync();

            if (generatedPassword != null)
            {
                TempData["Success"] =
                    $"Account created. Email: {student.Email}, Role: {role}, Password: {generatedPassword}";
            }
            else
            {
                TempData["Success"] =
                    $"Student profile created and linked to existing account {student.Email} (role: {role}).";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,DateOfBirth,Email,Phone,Address,Program,Year,GPA")] Student input)
        {
            if (id != input.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            student.FirstName = input.FirstName;
            student.LastName = input.LastName;
            student.DateOfBirth = input.DateOfBirth;
            student.Email = input.Email;
            student.Phone = input.Phone;
            student.Address = input.Address;
            student.Program = input.Program;
            student.Year = input.Year;
            student.GPA = input.GPA;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(student.Id))
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

        // GET: Students/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }

        [Authorize(Roles = "Student,Admin,Faculty")]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var student = await _context.Students
                .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (student == null)
            {
                return RedirectToAction(nameof(Create));
            }

            return View("Details", student);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UpdateProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user!.Id);
            if (student == null) return RedirectToAction(nameof(Create));
            return View(student);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([Bind("Id,FirstName,LastName,Phone,Address")] Student input)
        {
            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == input.Id && s.UserId == user!.Id);
            if (student == null) return NotFound();
            ModelState.Remove(nameof(Student.Email));
            ModelState.Remove(nameof(Student.Program));
            ModelState.Remove(nameof(Student.Year));
            ModelState.Remove(nameof(Student.DateOfBirth));

            DateOnly? parsedDob = student.DateOfBirth;
            var dobText = Request.Form["DateOfBirth"].ToString();
            if (!string.IsNullOrWhiteSpace(dobText))
            {
                var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy" };
                if (DateTime.TryParseExact(dobText, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    parsedDob = DateOnly.FromDateTime(dt);
                }
                else
                {
                    ModelState.AddModelError(nameof(Student.DateOfBirth), "Invalid date format.");
                }
            }
            else
            {
                parsedDob = null;
            }

            // Update student with incoming values before validation
            student.FirstName = input.FirstName;
            student.LastName = input.LastName;
            student.DateOfBirth = parsedDob;
            student.Phone = input.Phone;
            student.Address = input.Address;

            ModelState.Clear();
            if (!TryValidateModel(student))
            {
                TempData["Error"] = "Please fix the highlighted fields.";
                input.Program = student.Program;
                input.Year = student.Year;
                input.DateOfBirth = parsedDob;
                input.Email = student.Email;
                return View(input);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(MyProfile));
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyGrades()
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.Student!.UserId == user!.Id)
                .ToListAsync();
            return View(items);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyTimetable()
        {
            var user = await _userManager.GetUserAsync(User);
            var sessions = await _context.ClassSessions
                .Include(cs => cs.Course)
                .Where(cs => _context.Enrollments
                    .Include(e => e.Student)
                    .Any(e => e.CourseId == cs.CourseId && e.Student!.UserId == user!.Id))
                .OrderBy(cs => cs.DayOfWeek).ThenBy(cs => cs.StartTime)
                .ToListAsync();
            return View(sessions);
        }
    }
}
