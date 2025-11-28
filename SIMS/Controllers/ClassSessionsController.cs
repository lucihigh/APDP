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

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin,Faculty")]
    public class ClassSessionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassSessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ClassSessions
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ClassSessions.Include(c => c.Course);
            return View(await applicationDbContext.ToListAsync());
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code");
            return View();
        }

        // POST: ClassSessions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CourseId,DayOfWeek,StartTime,EndTime,Location")] ClassSession classSession)
        {
            if (ModelState.IsValid)
            {
                _context.Add(classSession);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code", classSession.CourseId);
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code", classSession.CourseId);
            return View(classSession);
        }

        // POST: ClassSessions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseId,DayOfWeek,StartTime,EndTime,Location")] ClassSession classSession)
        {
            if (id != classSession.Id)
            {
                return NotFound();
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code", classSession.CourseId);
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
    }
}
