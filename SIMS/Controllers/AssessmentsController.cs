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

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin,Faculty")]
    public class AssessmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifier;

        public AssessmentsController(ApplicationDbContext context, INotificationService notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        // GET: Assessments
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Assessments.Include(a => a.Course);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Assessments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (assessment == null)
            {
                return NotFound();
            }

            return View(assessment);
        }

        // GET: Assessments/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code");
            return View();
        }

        // POST: Assessments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CourseId,Title,Description,DueDate")] Assessment assessment, IFormFile? attachment)
        {
            if (ModelState.IsValid)
            {
                if (attachment != null && attachment.Length > 0)
                {
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    Directory.CreateDirectory(uploads);
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(attachment.FileName)}";
                    var path = Path.Combine(uploads, fileName);
                    using (var stream = System.IO.File.Create(path))
                    {
                        await attachment.CopyToAsync(stream);
                    }
                    assessment.AttachmentPath = $"/uploads/{fileName}";
                }

                _context.Add(assessment);
                await _context.SaveChangesAsync();
                var userIds = await _context.Enrollments
                    .Where(e => e.CourseId == assessment.CourseId)
                    .Join(_context.Students, e => e.StudentId, s => s.Id, (e, s) => s.UserId)
                    .Where(uid => uid != null)!
                    .Select(uid => uid!)
                    .ToListAsync();
                await _notifier.NotifyUsersAsync(userIds, $"New assessment: {assessment.Title}", $"/Assessments/Details/{assessment.Id}");
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code", assessment.CourseId);
            return View(assessment);
        }

        // GET: Assessments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assessment = await _context.Assessments.FindAsync(id);
            if (assessment == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code", assessment.CourseId);
            return View(assessment);
        }

        // POST: Assessments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CourseId,Title,Description,DueDate,AttachmentPath")] Assessment assessment)
        {
            if (id != assessment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(assessment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssessmentExists(assessment.Id))
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Code", assessment.CourseId);
            return View(assessment);
        }

        // GET: Assessments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (assessment == null)
            {
                return NotFound();
            }

            return View(assessment);
        }

        // POST: Assessments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assessment = await _context.Assessments.FindAsync(id);
            if (assessment != null)
            {
                _context.Assessments.Remove(assessment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AssessmentExists(int id)
        {
            return _context.Assessments.Any(e => e.Id == id);
        }
    }
}
