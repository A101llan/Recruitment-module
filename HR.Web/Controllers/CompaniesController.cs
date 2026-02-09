using System;
using System.Linq;
using System.Web.Mvc;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;

namespace HR.Web.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class CompaniesController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        private readonly TenantService _tenantService;
        private readonly AuditService _auditService;

        public CompaniesController()
        {
            _tenantService = new TenantService(_uow);
            _auditService = new AuditService();
        }

        /// <summary>
        /// Companies Dashboard
        /// </summary>
        public ActionResult Index()
        {
            var companies = _uow.Companies.GetAll().ToList();
            
            var viewModel = new CompaniesDashboardViewModel
            {
                TotalCompanies = companies.Count,
                ActiveCompanies = companies.Count(c => c.IsActive),
                InactiveCompanies = companies.Count(c => !c.IsActive),
                ExpiringSoon = companies.Count(c => c.LicenseExpiryDate.HasValue && 
                    c.LicenseExpiryDate.Value <= DateTime.Now.AddDays(30) &&
                    c.LicenseExpiryDate.Value > DateTime.Now),
                ExpiredCompanies = companies.Count(c => c.LicenseExpiryDate.HasValue && 
                    c.LicenseExpiryDate.Value < DateTime.Now),
                Companies = companies.Select(c => new CompanySummaryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    IsActive = c.IsActive,
                    LicenseExpiryDate = c.LicenseExpiryDate,
                    CreatedDate = c.CreatedDate,
                    UserCount = _uow.Users.GetAll().Count(u => u.CompanyId == c.Id),
                    PositionCount = _uow.Positions.GetAll().Count(p => p.CompanyId == c.Id),
                    ApplicationCount = _uow.Applications.GetAll().Count(a => a.CompanyId == c.Id),
                    DaysUntilExpiry = c.LicenseExpiryDate.HasValue ? 
                        (int)(c.LicenseExpiryDate.Value - DateTime.Now).TotalDays : (int?)null
                }).ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Create new company
        /// </summary>
        public ActionResult CreateCompany()
        {
            return View(new CreateCompanyViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCompany(CreateCompanyViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Check if slug already exists
                var existingCompany = _uow.Companies.GetAll()
                    .FirstOrDefault(c => c.Slug.Equals(model.Slug, StringComparison.OrdinalIgnoreCase));
                
                if (existingCompany != null)
                {
                    ModelState.AddModelError("Slug", "This slug is already in use.");
                    return View(model);
                }

                var company = _tenantService.CreateCompany(
                    model.Name, 
                    model.Slug, 
                    model.LicenseExpiryDate ?? DateTime.Now.AddYears(1)
                );

                _auditService.LogAction(
                    User.Identity.Name,
                    "COMPANY_CREATED",
                    "Company",
                    company.Id.ToString(),
                    null,
                    new { CompanyName = company.Name, Slug = company.Slug }
                );

                TempData["SuccessMessage"] = string.Format("Company '{0}' created successfully.", company.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error creating company: " + ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// Edit company details
        /// </summary>
        public ActionResult EditCompany(int id)
        {
            var company = _uow.Companies.Get(id);
            if (company == null)
                return HttpNotFound();

            var viewModel = new EditCompanyViewModel
            {
                Id = company.Id,
                Name = company.Name,
                Slug = company.Slug,
                IsActive = company.IsActive,
                LicenseExpiryDate = company.LicenseExpiryDate
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCompany(EditCompanyViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var company = _uow.Companies.Get(model.Id);
                if (company == null)
                    return HttpNotFound();

                // Check for slug collision if it changed
                if (company.Slug != model.Slug)
                {
                    var existing = _uow.Companies.GetAll().FirstOrDefault(c => c.Slug == model.Slug && c.Id != model.Id);
                    if (existing != null)
                    {
                        ModelState.AddModelError("Slug", "This slug is already in use by another company.");
                        return View(model);
                    }
                }

                var oldValues = new { 
                    Name = company.Name, 
                    Slug = company.Slug,
                    IsActive = company.IsActive, 
                    LicenseExpiry = company.LicenseExpiryDate 
                };

                company.Name = model.Name;
                company.Slug = model.Slug;
                company.IsActive = model.IsActive;
                company.LicenseExpiryDate = model.LicenseExpiryDate;

                _uow.Companies.Update(company);
                _uow.Complete();

                _auditService.LogUpdate(
                    User.Identity.Name,
                    "Company",
                    company.Id.ToString(),
                    oldValues,
                    new { Name = company.Name, Slug = company.Slug, IsActive = company.IsActive, LicenseExpiry = company.LicenseExpiryDate }
                );

                TempData["SuccessMessage"] = string.Format("Company '{0}' updated successfully.", company.Name);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error updating company: " + ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// View company details
        /// </summary>
        public ActionResult CompanyDetails(int id)
        {
            var company = _uow.Companies.Get(id);
            if (company == null)
                return HttpNotFound();

            var viewModel = new CompanyDetailsViewModel
            {
                Company = company,
                Users = _uow.Users.GetAll().Where(u => u.CompanyId == id).ToList(),
                Positions = _uow.Positions.GetAll().Where(p => p.CompanyId == id).ToList(),
                Applications = _uow.Applications.GetAll().Where(a => a.CompanyId == id).ToList(),
                Departments = _uow.Departments.GetAll().Where(d => d.CompanyId == id).ToList(),
                LicenseTransactions = _uow.LicenseTransactions.GetAll()
                    .Where(lt => lt.CompanyId == id)
                    .OrderByDescending(lt => lt.TransactionDate)
                    .ToList(),
                RecentAuditLogs = _uow.AuditLogs.GetAll()
                    .Where(a => a.CompanyId == id)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(20)
                    .ToList(),
                PendingImpersonationRequests = _uow.ImpersonationRequests.GetAll()
                    .Where(r => r.CompanyId == id && r.Status == ImpersonationRequestStatus.Pending && r.RequestedBy == User.Identity.Name)
                    .OrderByDescending(r => r.RequestDate)
                    .ToList(),
                ActiveApprovedRequest = _uow.ImpersonationRequests.GetAll()
                    .FirstOrDefault(r => r.CompanyId == id && 
                                   r.RequestedBy == User.Identity.Name && 
                                   r.Status == ImpersonationRequestStatus.Approved &&
                                   (!r.ExpiryDate.HasValue || r.ExpiryDate > DateTime.Now)),
                ActiveRejectedRequest = _uow.ImpersonationRequests.GetAll()
                    .Where(r => r.CompanyId == id && 
                           r.RequestedBy == User.Identity.Name && 
                           r.Status == ImpersonationRequestStatus.Rejected &&
                           r.DecisionDate.HasValue &&
                           System.Data.Entity.DbFunctions.DiffHours(r.DecisionDate.Value, DateTime.Now) < 12)
                    .OrderByDescending(r => r.DecisionDate)
                    .FirstOrDefault(),
                CompanyAdmins = _uow.Users.GetAll()
                    .Where(u => u.CompanyId == id && u.Role == "Admin")
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestImpersonation(int companyId, string targetAdmin, string reason = null)
        {
            if (string.IsNullOrWhiteSpace(targetAdmin))
            {
                TempData["ErrorMessage"] = "A target administrator is required.";
                return RedirectToAction("CompanyDetails", new { id = companyId });
            }

            var request = new ImpersonationRequest
            {
                CompanyId = companyId,
                RequestedBy = User.Identity.Name,
                RequestedFrom = targetAdmin,
                RequestDate = DateTime.Now,
                Status = ImpersonationRequestStatus.Pending,
                Reason = reason,
                ExpiryDate = DateTime.Now.AddHours(24)
            };

            _uow.ImpersonationRequests.Add(request);
            _uow.Complete();

            _auditService.LogAction(
                User.Identity.Name,
                "IMPERSONATION_REQUESTED",
                "Companies",
                companyId.ToString(),
                null,
                new { TargetAdmin = targetAdmin, Reason = reason }
            );

            TempData["SuccessMessage"] = "Impersonation request sent to " + targetAdmin + ". Please wait for approval.";
            return RedirectToAction("CompanyDetails", new { id = companyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelImpersonationRequest(int requestId)
        {
            var request = _uow.ImpersonationRequests.Get(requestId);
            if (request == null || request.RequestedBy != User.Identity.Name) return HttpNotFound();

            if (request.Status == ImpersonationRequestStatus.Pending)
            {
                request.Status = ImpersonationRequestStatus.Cancelled;
                request.DecisionDate = DateTime.Now;
                _uow.ImpersonationRequests.Update(request);
                _uow.Complete();

                _auditService.LogAction(
                    User.Identity.Name,
                    "IMPERSONATION_CANCELLED",
                    "Companies",
                    request.CompanyId.HasValue ? request.CompanyId.Value.ToString() : null,
                    null,
                    new { RequestId = requestId }
                );

                TempData["SuccessMessage"] = "Impersonation request cancelled.";
            }

            return RedirectToAction("CompanyDetails", new { id = request.CompanyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Elevate(int requestId)
        {
            var request = _uow.ImpersonationRequests.Get(requestId);
            if (request == null || request.RequestedBy != User.Identity.Name) return HttpNotFound();

            if (request.Status != ImpersonationRequestStatus.Approved)
            {
                TempData["ErrorMessage"] = "This request has not been approved or has expired.";
                return RedirectToAction("CompanyDetails", new { id = request.CompanyId });
            }

            var company = _uow.Companies.Get(request.CompanyId.Value);
            if (company == null) return HttpNotFound();

            // Check expiry
            if (request.ExpiryDate.HasValue && request.ExpiryDate.Value < DateTime.Now)
            {
                request.Status = ImpersonationRequestStatus.Expired;
                _uow.ImpersonationRequests.Update(request);
                _uow.Complete();
                TempData["ErrorMessage"] = "This authorization has expired.";
                return RedirectToAction("CompanyDetails", new { id = request.CompanyId });
            }

            // Log the impersonation event
            _auditService.LogAction(
                User.Identity.Name,
                "IMPERSONATION_START",
                "Companies",
                request.CompanyId.ToString(),
                null,
                new { Reason = request.Reason, CompanyName = company.Name, ApprovedBy = request.RequestedFrom }
            );

            // Set session variables
            Session["ImpersonatedRequestId"] = request.Id;
            Session["ImpersonatedCompanyId"] = request.CompanyId;
            Session["ImpersonationReason"] = request.Reason ?? "Not specified";
            Session["ImpersonatedCompanyName"] = company.Name;

            // Mark request as Active
            request.Status = ImpersonationRequestStatus.Active;
            _uow.ImpersonationRequests.Update(request);
            _uow.Complete();

            TempData["SuccessMessage"] = string.Format("Now impersonating {0}. Session authorized by {1}.", company.Name, request.RequestedFrom);
            return RedirectToAction("Index", "Dashboard");
        }

        public ActionResult StopImpersonating()
        {
            int? companyId = null;
            if (_tenantService.IsImpersonating())
            {
                companyId = _tenantService.GetImpersonatedCompanyId();
                _auditService.LogAction(
                    User.Identity.Name,
                    "IMPERSONATION_STOP",
                    "Companies",
                    companyId.ToString(),
                    null,
                    null
                );

                // Ensure the specific request is expired so it can never be reused
                // Clean up any 'Active' or 'Approved' requests for this user/company to prevent auto-login loops
                if (companyId.HasValue)
                {
                    var relatedRequests = _uow.ImpersonationRequests.GetAll()
                        .Where(r => r.CompanyId == companyId.Value && 
                               r.RequestedBy == User.Identity.Name && 
                               (r.Status == ImpersonationRequestStatus.Active || r.Status == ImpersonationRequestStatus.Approved))
                        .ToList();
                    
                    foreach(var r in relatedRequests)
                    {
                        r.Status = ImpersonationRequestStatus.Expired;
                        _uow.ImpersonationRequests.Update(r);
                    }
                }

                _uow.Complete();

                Session.Remove("ImpersonatedRequestId");
                Session.Remove("ImpersonatedCompanyId");
                Session.Remove("ImpersonationReason");
                Session.Remove("ImpersonatedCompanyName");
                
                TempData["SuccessMessage"] = "Impersonation session closed. Access rights have been revoked.";
            }

            // Redirect back to company details so they can see they've returned to SuperAdmin role
            if (companyId.HasValue)
            {
                return RedirectToAction("CompanyDetails", new { id = companyId.Value });
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_uow != null)
                {
                    _uow.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }

    // View Models
    public class CompaniesDashboardViewModel
    {
        public int TotalCompanies { get; set; }
        public int ActiveCompanies { get; set; }
        public int InactiveCompanies { get; set; }
        public int ExpiringSoon { get; set; }
        public int ExpiredCompanies { get; set; }
        public System.Collections.Generic.List<CompanySummaryViewModel> Companies { get; set; }
    }

    public class CompanySummaryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int UserCount { get; set; }
        public int PositionCount { get; set; }
        public int ApplicationCount { get; set; }
        public int? DaysUntilExpiry { get; set; }
    }

    public class CreateCompanyViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Name { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(50)]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^[a-z0-9-]+$", 
            ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens.")]
        public string Slug { get; set; }

        public DateTime? LicenseExpiryDate { get; set; }
    }

    public class EditCompanyViewModel
    {
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Name { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(50)]
        public string Slug { get; set; }

        public bool IsActive { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
    }

    public class CompanyDetailsViewModel
    {
        public Company Company { get; set; }
        public System.Collections.Generic.List<User> Users { get; set; }
        public System.Collections.Generic.List<Position> Positions { get; set; }
        public System.Collections.Generic.List<Application> Applications { get; set; }
        public System.Collections.Generic.List<Department> Departments { get; set; }
        public System.Collections.Generic.List<LicenseTransaction> LicenseTransactions { get; set; }
        public System.Collections.Generic.List<AuditLog> RecentAuditLogs { get; set; }
        public System.Collections.Generic.List<ImpersonationRequest> PendingImpersonationRequests { get; set; }
        public ImpersonationRequest ActiveApprovedRequest { get; set; }
        public ImpersonationRequest ActiveRejectedRequest { get; set; }
        public System.Collections.Generic.List<User> CompanyAdmins { get; set; }
    }
}
