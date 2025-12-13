using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin,Faculty")]
    public class ClassSessionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string DuplicateSessionMessage = "A session for this course/day/slot already exists during the selected date range.";

        public ClassSessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ClassSessions
        public async Task<IActionResult> Index()
        {
            var items = await _context.ClassSessions
                .Include(c => c.Course)
                .AsNoTracking()
                .OrderBy(c => c.Course!.Code)
                .ThenBy(c => c.DayOfWeek == 0 ? 7 : c.DayOfWeek)
                .ThenBy(c => c.SessionSlot)
                .ThenBy(c => c.StartTime)
                .ToListAsync();
            return View(items);
        }

        // GET: ClassSessions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classSession = await _context.ClassSessions
                .Include(c => c.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classSession == null)
            {
                return NotFound();
            }

            return View(classSession);
        }

        // GET: ClassSessions/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses.OrderBy(c => c.Code), "Id", "Code");
            ViewData["DayOfWeek"] = new SelectList(GetDayOptions(), "Value", "Text", 1);
            ViewData["SessionSlot"] = new SelectList(GetSlotOptions(), "Value", "Text", 1);
            return View();
        }

        // POST: ClassSessions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CourseId,DayOfWeek,SessionSlot,StartTime,EndTime,Location")] ClassSession classSession)
        {
            // Always create a new row; ignore any posted Id.
            classSession.Id = 0;

            // Backward compatibility: if an older UI didn't post SessionSlot, default to slot 1.
            if (classSession.SessionSlot < 1 || classSession.SessionSlot > 6)
            {
                classSession.SessionSlot = 1;
                ModelState.Remove(nameof(ClassSession.SessionSlot));
                TryValidateModel(classSession);
            }

            if (ModelState.IsValid)
            {
                if (classSession.EndTime < classSession.StartTime)
                {
                    ModelState.AddModelError(nameof(ClassSession.EndTime), "End date must be on or after start date.");
                }
                else
                {
                    var overlapExists = await HasOverlappingSessionAsync(classSession);
                    if (overlapExists)
                    {
                        ModelState.AddModelError(string.Empty, DuplicateSessionMessage);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(classSession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses.OrderBy(c => c.Code), "Id", "Code", classSession.CourseId);
            ViewData["DayOfWeek"] = new SelectList(GetDayOptions(), "Value", "Text", classSession.DayOfWeek);
            ViewData["SessionSlot"] = new SelectList(GetSlotOptions(), "Value", "Text", classSession.SessionSlot);
            return View(classSession);
        }

        // GET: ClassSessions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classSession = await _context.ClassSessions.FindAsync(id);
            if (classSession == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses.OrderBy(c => c.Code), "Id", "Code", classSession.CourseId);
            ViewData["DayOfWeek"] = new SelectList(GetDayOptions(), "Value", "Text", classSession.DayOfWeek);
            ViewData["SessionSlot"] = new SelectList(GetSlotOptions(), "Value", "Text", classSession.SessionSlot);
            return View(classSession);
        }

        // POST: ClassSessions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseId,DayOfWeek,SessionSlot,StartTime,EndTime,Location")] ClassSession classSession)
        {
            if (id != classSession.Id)
            {
                return NotFound();
            }

            // Backward compatibility: if an older UI didn't post SessionSlot, default to slot 1.
            if (classSession.SessionSlot < 1 || classSession.SessionSlot > 6)
            {
                classSession.SessionSlot = 1;
                ModelState.Remove(nameof(ClassSession.SessionSlot));
                TryValidateModel(classSession);
            }

            if (ModelState.IsValid)
            {
                if (classSession.EndTime < classSession.StartTime)
                {
                    ModelState.AddModelError(nameof(ClassSession.EndTime), "End date must be on or after start date.");
                }
                else
                {
                    var overlapExists = await HasOverlappingSessionAsync(classSession, excludeId: classSession.Id);
                    if (overlapExists)
                    {
                        ModelState.AddModelError(string.Empty, DuplicateSessionMessage);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(classSession);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassSessionExists(classSession.Id))
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
            ViewData["CourseId"] = new SelectList(_context.Courses.OrderBy(c => c.Code), "Id", "Code", classSession.CourseId);
            ViewData["DayOfWeek"] = new SelectList(GetDayOptions(), "Value", "Text", classSession.DayOfWeek);
            ViewData["SessionSlot"] = new SelectList(GetSlotOptions(), "Value", "Text", classSession.SessionSlot);
            return View(classSession);
        }

        // GET: ClassSessions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classSession = await _context.ClassSessions
                .Include(c => c.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classSession == null)
            {
                return NotFound();
            }

            return View(classSession);
        }

        // POST: ClassSessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classSession = await _context.ClassSessions.FindAsync(id);
            if (classSession != null)
            {
                _context.ClassSessions.Remove(classSession);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClassSessionExists(int id)
        {
            return _context.ClassSessions.Any(e => e.Id == id);
        }

        private Task<bool> HasOverlappingSessionAsync(ClassSession classSession, int? excludeId = null)
        {
            return _context.ClassSessions
                .AsNoTracking()
                .AnyAsync(cs =>
                    (!excludeId.HasValue || cs.Id != excludeId.Value) &&
                    cs.CourseId == classSession.CourseId &&
                    cs.DayOfWeek == classSession.DayOfWeek &&
                    cs.SessionSlot == classSession.SessionSlot &&
                    cs.StartTime <= classSession.EndTime &&
                    cs.EndTime >= classSession.StartTime);
        }

        private static IEnumerable<object> GetDayOptions()
        {
            return new[]
            {
                new { Value = 1, Text = "Monday" },
                new { Value = 2, Text = "Tuesday" },
                new { Value = 3, Text = "Wednesday" },
                new { Value = 4, Text = "Thursday" },
                new { Value = 5, Text = "Friday" },
                new { Value = 6, Text = "Saturday" },
                new { Value = 0, Text = "Sunday" }
            };
        }

        private static IEnumerable<object> GetSlotOptions()
        {
            return new[]
            {
                new { Value = 1, Text = "Slot 1 (07:00 - 09:00)" },
                new { Value = 2, Text = "Slot 2 (09:00 - 11:00)" },
                new { Value = 3, Text = "Slot 3 (12:00 - 14:00)" },
                new { Value = 4, Text = "Slot 4 (14:00 - 16:00)" },
                new { Value = 5, Text = "Slot 5 (16:00 - 18:00)" },
                new { Value = 6, Text = "Slot 6 (18:00 - 20:00)" }
            };
        }
    }
}
