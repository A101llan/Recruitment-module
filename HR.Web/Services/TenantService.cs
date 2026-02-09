using System;
using System.Linq;
using System.Web;
using HR.Web.Data;
using HR.Web.Models;

namespace HR.Web.Services
{
    public class TenantService
    {
        private readonly UnitOfWork _uow;

        public TenantService()
        {
            _uow = new UnitOfWork();
        }

        public TenantService(UnitOfWork uow)
        {
            _uow = uow;
        }

        /// <summary>
        /// Get the current user's CompanyId from their authentication context
        /// </summary>
        public int? GetCurrentUserCompanyId()
        {
            // If impersonating, return the impersonated company ID
            if (IsImpersonating())
            {
                var session = HttpContext.Current.Session;
                return session != null ? (int?)session["ImpersonatedCompanyId"] : null;
            }

            if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated)
                return null;

            var username = HttpContext.Current.User.Identity.Name;
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == username);
            
            return user != null ? user.CompanyId : null;
        }

        /// <summary>
        /// Check if the current user is a SuperAdmin
        /// </summary>
        public bool IsSuperAdmin()
        {
            // If impersonating, the user is technically acting as a tenant, so we return false
            // for general role checks to ensure they are filtered correctly.
            if (IsImpersonating())
            {
                return false;
            }

            return IsActualSuperAdmin();
        }

        public bool IsActualSuperAdmin()
        {
            if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated)
                return false;

            var username = HttpContext.Current.User.Identity.Name;
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == username);
            
            // A user is a SuperAdmin if they have the explicit role OR if they are a global user (no CompanyId) with an Admin role
            return user != null && (user.Role == "SuperAdmin" || (!user.CompanyId.HasValue && user.Role == "Admin"));
        }

        /// <summary>
        /// Get the current user's role
        /// </summary>
        public string GetCurrentUserRole()
        {
            if (IsImpersonating())
            {
                return "Admin"; // Impersonate as Admin
            }

            if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated)
                return null;

            var username = HttpContext.Current.User.Identity.Name;
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == username);
            
            return user != null ? user.Role : null;
        }

        public bool IsImpersonating()
        {
            return HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session["ImpersonatedCompanyId"] != null;
        }

        public int? GetImpersonatedCompanyId()
        {
            return (HttpContext.Current != null && HttpContext.Current.Session != null) ? (int?)HttpContext.Current.Session["ImpersonatedCompanyId"] : null;
        }

        public string GetImpersonationReason()
        {
            return (HttpContext.Current != null && HttpContext.Current.Session != null) ? (string)HttpContext.Current.Session["ImpersonationReason"] : null;
        }

        /// <summary>
        /// Filter a queryable by tenant (CompanyId) unless user is SuperAdmin
        /// </summary>
        public IQueryable<T> ApplyTenantFilter<T>(IQueryable<T> query) where T : class, ITenantEntity
        {
            if (IsSuperAdmin())
                return query; // SuperAdmin sees all

            var companyId = GetCurrentUserCompanyId();
            if (!companyId.HasValue)
                return query.Where(e => false); // No company = no data

            return query.Where(e => e.CompanyId == companyId.Value);
        }

        /// <summary>
        /// Check if a company's license is active
        /// </summary>
        public bool IsCompanyLicenseActive(int companyId)
        {
            var company = _uow.Companies.Get(companyId);
            if (company == null || !company.IsActive)
                return false;

            if (company.LicenseExpiryDate.HasValue && company.LicenseExpiryDate.Value < DateTime.Now)
                return false;

            return true;
        }

        /// <summary>
        /// Check if the current user's company license is active
        /// </summary>
        public bool IsCurrentCompanyLicenseActive()
        {
            var companyId = GetCurrentUserCompanyId();
            if (!companyId.HasValue)
                return IsSuperAdmin(); // SuperAdmin always active

            return IsCompanyLicenseActive(companyId.Value);
        }

        /// <summary>
        /// Get company by ID (SuperAdmin only)
        /// </summary>
        public Company GetCompany(int id)
        {
            if (!IsSuperAdmin())
                throw new UnauthorizedAccessException("Only SuperAdmin can access company details.");

            return _uow.Companies.Get(id);
        }

        /// <summary>
        /// Get all companies (SuperAdmin only)
        /// </summary>
        public IQueryable<Company> GetAllCompanies()
        {
            if (!IsSuperAdmin())
                throw new UnauthorizedAccessException("Only SuperAdmin can list companies.");

            return _uow.Context.Companies.AsQueryable();
        }

        /// <summary>
        /// Create a new company (SuperAdmin only)
        /// </summary>
        public Company CreateCompany(string name, string slug, DateTime? licenseExpiry)
        {
            if (!IsSuperAdmin())
                throw new UnauthorizedAccessException("Only SuperAdmin can create companies.");

            var company = new Company
            {
                Name = name,
                Slug = slug,
                IsActive = true,
                LicenseExpiryDate = licenseExpiry ?? DateTime.Now.AddYears(1),
                CreatedDate = DateTime.Now
            };

            _uow.Companies.Add(company);
            _uow.Complete();

            return company;
        }

        /// <summary>
        /// Update company license (SuperAdmin only)
        /// </summary>
        public void UpdateCompanyLicense(int companyId, DateTime? newExpiryDate, bool? isActive)
        {
            if (!IsSuperAdmin())
                throw new UnauthorizedAccessException("Only SuperAdmin can update licenses.");

            var company = _uow.Companies.Get(companyId);
            if (company == null)
                throw new ArgumentException("Company not found.");

            if (newExpiryDate.HasValue)
                company.LicenseExpiryDate = newExpiryDate.Value;

            if (isActive.HasValue)
                company.IsActive = isActive.Value;

            _uow.Companies.Update(company);
            _uow.Complete();
        }

        /// <summary>
        /// Deactivate a company (SuperAdmin only)
        /// </summary>
        public void DeactivateCompany(int companyId)
        {
            UpdateCompanyLicense(companyId, null, false);
        }

        /// <summary>
        /// Activate a company (SuperAdmin only)
        /// </summary>
        public void ActivateCompany(int companyId)
        {
            UpdateCompanyLicense(companyId, null, true);
        }
    }
}
