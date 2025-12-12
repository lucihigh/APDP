using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.Data;
using SIMS.Models;
using SIMS.Services;

namespace SIMS.Controllers
{
    [Authorize]
    public class FacultyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FacultyController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.FacultyProfiles
                .AsNoTracking()
                .Include(f => f.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(f =>
                    f.FirstName.Contains(term) ||
                    f.LastName.Contains(term) ||
                    f.Email.Contains(term) ||
                    (f.Department != null && f.Department.Contains(term)));
            }

            ViewData["q"] = q;
            return View(await query.ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,DateOfBirth,Email,Phone,Address,Department,Title")] FacultyProfile input)
        {
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var user = await _userManager.FindByEmailAsync(input.Email);
            const string role = "Faculty";
            IdentityResult createResult;
            string? generatedPassword = null;
            var passwordToUse = input.DateOfBirth.HasValue
                ? StudentPasswordGenerator.Generate(input.FirstName, input.LastName, input.DateOfBirth.Value)
                : "Faculty#12345";

            if (user == null)
            {
                var userId = await UserIdGenerator.GenerateForRoleAsync(_userManager, role);
                user = new IdentityUser
                {
                    Id = userId,
                    UserName = input.Email,
                    Email = input.Email,
                    EmailConfirmed = true
                };

                createResult = await _userManager.CreateAsync(user, passwordToUse);
                generatedPassword = passwordToUse;
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(input);
                }
            }
            else
            {
                createResult = IdentityResult.Success;
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            if (!UserIdGenerator.IsFormatted(user.Id, role))
            {
                user = await UserIdRepairService.RepairSingleStudentAsync(_context, _userManager, user, role);
            }

            var faculty = new FacultyProfile
            {
                FirstName = input.FirstName,
                LastName = input.LastName,
                DateOfBirth = input.DateOfBirth,
                Email = input.Email,
                Phone = input.Phone,
                Address = input.Address,
                Department = input.Department,
                Title = input.Title,
                UserId = user.Id
            };

            _context.FacultyProfiles.Add(faculty);
            await _context.SaveChangesAsync();

            TempData["Success"] = generatedPassword != null
                ? $"Faculty created. Email: {faculty.Email}, Password: {generatedPassword}"
                : $"Faculty profile linked to existing account {faculty.Email}.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.FacultyProfiles
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (faculty == null) return NotFound();
            return View(faculty);
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var faculty = await _context.FacultyProfiles
                .FirstOrDefaultAsync(f => f.UserId == user.Id);

            if (faculty == null) return NotFound();

            var vm = new FacultyManageViewModel
            {
                Profile = new FacultyProfileEditViewModel
                {
                    Id = faculty.Id,
                    FirstName = faculty.FirstName,
                    LastName = faculty.LastName,
                    DateOfBirth = faculty.DateOfBirth,
                    Email = faculty.Email,
                    Phone = faculty.Phone,
                    Address = faculty.Address,
                    Department = faculty.Department,
                    Title = faculty.Title
                },
                Password = new FacultyChangePasswordViewModel()
            };

            return View("Manage", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] FacultyProfileEditViewModel input)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var faculty = await _context.FacultyProfiles.FirstOrDefaultAsync(f => f.UserId == user.Id);
            if (faculty == null) return NotFound();

            // email is fixed for faculty; enforce original value to avoid accidental edits
            input.Email = faculty.Email;
            ModelState.Remove("Profile.Email");
            ModelState.Remove("Email");

            if (!ModelState.IsValid)
            {
                return View("Manage", BuildManageViewModel(faculty, input, new FacultyChangePasswordViewModel()));
            }

            faculty.FirstName = input.FirstName;
            faculty.LastName = input.LastName;
            faculty.DateOfBirth = input.DateOfBirth;
            faculty.Phone = input.Phone;
            faculty.Address = input.Address;
            faculty.Department = input.Department;
            faculty.Title = input.Title;

            _context.FacultyProfiles.Update(faculty);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Faculty")]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] FacultyChangePasswordViewModel input)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var faculty = await _context.FacultyProfiles.FirstOrDefaultAsync(f => f.UserId == user.Id);
            if (faculty == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View("Manage", BuildManageViewModel(faculty, null, input));
            }

            var result = await _userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("Manage", BuildManageViewModel(faculty, null, input));
            }

            TempData["Success"] = "Password updated.";
            return RedirectToAction(nameof(Manage));
        }

        private FacultyManageViewModel BuildManageViewModel(FacultyProfile faculty, FacultyProfileEditViewModel? profileInput, FacultyChangePasswordViewModel? passwordInput)
        {
            var profileVm = profileInput ?? new FacultyProfileEditViewModel
            {
                Id = faculty.Id,
                FirstName = faculty.FirstName,
                LastName = faculty.LastName,
                DateOfBirth = faculty.DateOfBirth,
                Email = faculty.Email,
                Phone = faculty.Phone,
                Address = faculty.Address,
                Department = faculty.Department,
                Title = faculty.Title
            };

            return new FacultyManageViewModel
            {
                Profile = profileVm,
                Password = passwordInput ?? new FacultyChangePasswordViewModel()
            };
        }
    }
}
