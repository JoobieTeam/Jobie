using Joobie.Data;
using Joobie.Infrastructure;
using Joobie.Models.JobModels;
using Joobie.Utility;
using Joobie.ViewModels;
using LinqKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Joobie.Controllers
{


    public class JobsController : Controller
    {
        
        private readonly ApplicationDbContext _context;

        private const string _cVFilePath = "/Joobie/Joobie/wwwroot/cVs";
        private readonly SearchStringSession _searchStringSession;
        private readonly UserManager<IdentityUser> _userManager;
        private static byte _pageSize = 20; 

        public JobsController(ApplicationDbContext context,
            SearchStringSession searchStringSession, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _searchStringSession = searchStringSession;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(SearchSettingViewModel searchSettingViewModel,int page=1)
        {
           
            if (searchSettingViewModel.WorkingHour.Length == 0 && searchSettingViewModel.TypesOfContracts.Length == 0 &&
                searchSettingViewModel.Categories.Length == 0 && searchSettingViewModel.SearchString == "" && searchSettingViewModel.CitySearchString == "")
            {
                if (_searchStringSession.searchSetting == null)
                {
                    searchSettingViewModel = await GetSearchSettingViewModel();
                    _searchStringSession.SetSearch(searchSettingViewModel);
                }
            }
            else
            {
                _searchStringSession.SetSearch(searchSettingViewModel);
            }

            searchSettingViewModel = _searchStringSession.searchSetting;

            var jobs = await GetSortedAndFilteredJobListAsync(searchSettingViewModel);
            int totalJobs = jobs.Count();

            jobs = jobs.OrderBy(c => c.AddedDate)
                       .Skip((page - 1) * _pageSize)
                       .Take(_pageSize)
                       .ToList();

            ViewData["searchSettingViewModel"] = searchSettingViewModel;

            var viewModel = new ListViewModel<Job>
            {
                Items = jobs,
                Total = totalJobs,
                PagingInfo = new PagingInfo
                {
                    CurrentPage = page,
                    ItemsPerPage = _pageSize,
                    TotalItems = totalJobs
                }
            };

            if (User.IsInRole(Strings.AdminUser) || User.IsInRole(Strings.ModeratorUser))
            {           
                return View(viewModel);
            }
         
            return View("ReadOnlyList", viewModel);
        }


        public IActionResult ResetSearch()
        {
            _searchStringSession.SetSearch(null);
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Details([FromRouteAttribute] long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Job
                .Include(j => j.ApplicationUser)
                .Where(j => j.ApplicationUser.Name != null)
                .Include(j => j.Category)
                .Include(j => j.TypeOfContract)
                .Include(j => j.WorkingHours)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (job == null)
            {
                return NotFound();
            }
            var currentUserId = _userManager.GetUserId(User); 

            if(currentUserId != null)
            {
                ViewData["userId"] = currentUserId;
            }

            return View(job);
        }

        [Authorize(Roles = Strings.AdminUser + "," + Strings.ModeratorUser + "," + Strings.CompanyUser)]
        public IActionResult Create()
        {
            ViewData["Employees"] = new SelectList(_context.ApplicationUser.Where(j => j.Name != null), "Id", "Name");
            ViewData["Categories"] = new SelectList(_context.Category, "Id", "Name");
            ViewData["TypesOfContracts"] = new SelectList(_context.TypeOfContract, "Id", "Name");
            ViewData["WorkingHours"] = new SelectList(_context.WorkingHours, "Id", "Name");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Strings.AdminUser + "," + Strings.ModeratorUser + "," + Strings.CompanyUser)]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Localization,AddedDate,ExpirationDate,Salary,CategoryId,TypeOfContractId,WorkingHoursId,UserId")] Job job)
        {
            if (User.IsInRole(Strings.CompanyUser))
            {
                var claimsIdentity = (ClaimsIdentity)this.User.Identity;
                var claim = claimsIdentity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                var userId = claim.Value;
                job.UserId = userId;
            }

            if (ModelState.IsValid)
            {
                _context.Add(job);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["Employees"] = new SelectList(_context.ApplicationUser.Where(j => j.Name != null), "Id", "Name");
            ViewData["Categories"] = new SelectList(_context.Category, "Id", "Name");
            ViewData["TypesOfContracts"] = new SelectList(_context.TypeOfContract, "Id", "Name");
            ViewData["WorkingHours"] = new SelectList(_context.WorkingHours, "Id", "Name");
            return View(nameof(Create),job);
        }

        // GET: Jobs/Edit/5
        [Authorize(Roles = Strings.AdminUser + "," + Strings.ModeratorUser + "," + Strings.CompanyUser)]
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Job.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            if (User.IsInRole(Strings.CompanyUser) && job.UserId != currentUserId)
            {
                ViewData["Title"] = "Brak dostępu";
                return View("~/Views/Shared/AccessDenied.cshtml");
            }

            ViewData["Employees"] = new SelectList(_context.ApplicationUser.Where(j => j.Name != null), "Id", "Name");
            ViewData["Categories"] = new SelectList(_context.Category, "Id", "Name");
            ViewData["TypesOfContracts"] = new SelectList(_context.TypeOfContract, "Id", "Name");
            ViewData["WorkingHours"] = new SelectList(_context.WorkingHours, "Id", "Name");
            return View(job);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Strings.AdminUser + "," + Strings.ModeratorUser + "," + Strings.CompanyUser)]
        public async Task<IActionResult> Edit(long id, [Bind("Id,Name,Description,Localization,AddedDate,ExpirationDate,Salary,CategoryId,TypeOfContractId,WorkingHoursId,UserId")] Job job)
        {
            if (id != job.Id)
            {
                return NotFound();
            }


            var currentUserId = _userManager.GetUserId(User);
            if (User.IsInRole(Strings.CompanyUser)) { 
            if (job.UserId != currentUserId)
            {
                ViewData["Title"] = "Brak dostępu";
                return View("~/Views/Shared/AccessDenied.cshtml");
            }
        }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(job);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobExists(job.Id))
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
            ViewData["Employees"] = new SelectList(_context.ApplicationUser.Where(j => j.Name != null), "Id", "Name");
            ViewData["Categories"] = new SelectList(_context.Category, "Id", "Name");
            ViewData["TypesOfContracts"] = new SelectList(_context.TypeOfContract, "Id", "Name");
            ViewData["WorkingHours"] = new SelectList(_context.WorkingHours, "Id", "Name");
            return View(job);
        }

        // GET: Jobs/Delete/5
        [Authorize(Roles = Strings.AdminUser + "," + Strings.ModeratorUser)]
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Job
                .Include(j => j.ApplicationUser)
                .Include(j => j.Category)
                .Include(j => j.TypeOfContract)
                .Include(j => j.WorkingHours)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        // POST: Jobs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Strings.AdminUser + "," + Strings.ModeratorUser)]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var job = await _context.Job.FindAsync(id);
            _context.Job.Remove(job);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool JobExists(long id)
        {
            return _context.Job.Any(e => e.Id == id);
        }

        [Authorize(Roles = Strings.EmployeeUser)]
        public async Task<IActionResult> Apply(long Id)
        {
            var job = await _context.Job.Include(j => j.Category)
                .Include(j => j.TypeOfContract)
                .Include(j => j.WorkingHours)
                .Include(j => j.ApplicationUser)
                .Where(j => j.Id == Id)
                .FirstOrDefaultAsync();
            CVJobApplicationUser cVJobApplicationUser = new CVJobApplicationUser
            {
                JobInMiddleTable = job,
                JobInMiddleTableId = Id
            };
            return View(cVJobApplicationUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Strings.EmployeeUser)]
        public async Task<IActionResult> Apply(CVJobApplicationUser cVJobApplicationUser)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            string userId = claim.Value;
            cVJobApplicationUser.EmployeeUserId = userId;
            var uniqueName = "";
            bool saveImageSuccess = true;
            if (!ModelState.IsValid)
            {
                return View("Apply", cVJobApplicationUser);
            }
            var cVJobApplicationUserInDb = await _context.CVJobApplicationUser.FirstOrDefaultAsync(c => c.EmployeeUserId == userId && c.JobInMiddleTableId == cVJobApplicationUser.JobInMiddleTableId);
            if (cVJobApplicationUserInDb != null)
            {
                uniqueName = cVJobApplicationUserInDb.CvName;
                cVJobApplicationUserInDb.JobInMiddleTable = cVJobApplicationUser.JobInMiddleTable;
                cVJobApplicationUserInDb.EmployeeUser = cVJobApplicationUser.EmployeeUser;

                if (Request.Form.Files.Any())
                    saveImageSuccess = await SaveCvToDirectory(uniqueName);
                if (saveImageSuccess == false)
                    return View("Error");

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }


            uniqueName = await GetUniqueFileName();
            cVJobApplicationUser.CvName = Path.GetFileNameWithoutExtension(uniqueName)
                + Path.GetExtension(cVJobApplicationUser.Cv.FileName);
            saveImageSuccess = await SaveCvToDirectory(cVJobApplicationUser.CvName);
            if (saveImageSuccess == false)
                return View("Error");
            await _context.CVJobApplicationUser.AddAsync(cVJobApplicationUser);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<Job>> GetSortedAndFilteredJobListAsync(SearchSettingViewModel searchSettingViewModel)
        {
            var predicate = PredicateBuilder.New<Job>();
            predicate.DefaultExpression = j => true;
            if (!string.IsNullOrEmpty(searchSettingViewModel.SearchString))
            {
                predicate = predicate.And(j => j.Name.ToLower().Contains(searchSettingViewModel.SearchString.Trim().ToLower()));
            }
            if (!string.IsNullOrEmpty(searchSettingViewModel.CitySearchString))
            {
                predicate = predicate.And(j => j.Localization.Contains(searchSettingViewModel.CitySearchString));
            }
            if (searchSettingViewModel.Categories.Any(c => c.Selected == true))
            {
                List<int> catIds = new List<int>();
                for (int i = 0; i < searchSettingViewModel.Categories.Length; i++)
                {
                    if (searchSettingViewModel.Categories[i].Selected == true)
                        catIds.Add(searchSettingViewModel.Categories[i].Id);
                }
                if (catIds.Any())
                    predicate = predicate.And(j => catIds.Contains(j.CategoryId));
            }
            if (searchSettingViewModel.TypesOfContracts.Any(c => c.Selected == true))
            {
                List<int> typesIds = new List<int>();
                for (int i = 0; i < searchSettingViewModel.TypesOfContracts.Length; i++)
                {
                    if (searchSettingViewModel.TypesOfContracts[i].Selected == true)
                        typesIds.Add(searchSettingViewModel.TypesOfContracts[i].Id);
                }
                if (typesIds.Any())
                    predicate = predicate.And(j => typesIds.Contains(j.TypeOfContractId));
            }
            if (searchSettingViewModel.WorkingHour.Any(c => c.Selected == true))
            {
                List<int> workingHoursIds = new List<int>();
                for (int i = 0; i < searchSettingViewModel.WorkingHour.Length; i++)
                {
                    if (searchSettingViewModel.WorkingHour[i].Selected == true)
                        workingHoursIds.Add(searchSettingViewModel.WorkingHour[i].Id);
                }
                if (workingHoursIds.Any())
                    predicate = predicate.And(j => workingHoursIds.Contains(j.WorkingHoursId));
            }
            predicate = predicate.And(j => j.ApplicationUser.Name != null);

            var jobs = await _context.Job.Where(predicate)
                                 .Include(j => j.Category)
                                 .Include(j => j.TypeOfContract)
                                 .Include(j => j.WorkingHours)
                                 .Include(j => j.ApplicationUser)
                                 .ToListAsync();
            return jobs;
        }

        private async Task<SearchSettingViewModel> GetSearchSettingViewModel()
        {
            var categories = await _context.Category.ToListAsync();
            var workingHours = await _context.WorkingHours.ToListAsync();
            var typesOfContracts = await _context.TypeOfContract.ToListAsync();

            var searchSettingViewModel = new SearchSettingViewModel
            {
                Categories = new Filter[categories.Count],
                TypesOfContracts = new Filter[typesOfContracts.Count],
                WorkingHour = new Filter[workingHours.Count]
            };

            for (int i = 0; i < categories.Count; i++)
            {
                searchSettingViewModel.Categories[i] =
                    new Filter { Id = categories[i].Id, Name = categories[i].Name, Selected = false };
            }
            for (int i = 0; i < workingHours.Count; i++)
            {
                searchSettingViewModel.WorkingHour[i] =
                    new Filter { Id = workingHours[i].Id, Name = workingHours[i].Name, Selected = false };
            }
            for (int i = 0; i < typesOfContracts.Count; i++)
            {
                searchSettingViewModel.TypesOfContracts[i] =
                    new Filter { Id = typesOfContracts[i].Id, Name = typesOfContracts[i].Name, Selected = false };
            }

            return searchSettingViewModel;
        }

        private async Task<bool> SaveCvToDirectory(string fileName)
        {
            IFormFile file = Request.Form.Files.First();
            string pathSrc = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()));
            pathSrc += _cVFilePath;
            using (var stream = new FileStream(Path.Combine(pathSrc, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return true;
        }


        private async Task<string> GetUniqueFileName()
        {
            string fileName = "";
            await Task.Run(() =>
            {
                fileName = Path.GetRandomFileName();
                string path = Path.Combine("~/wwwroot/cVs", fileName);
                while (System.IO.File.Exists(path))
                {
                    fileName = Path.GetRandomFileName();
                    path = Path.Combine("~/wwwroot/cVs", fileName);
                }
            });

            return fileName;
        }
    }
}
