using System;
using System.Linq;
using System.Web.Mvc;
// using System.Web.Optimization; // Bundling disabled for local run
using System.Web.Routing;
using System.Web.Security;
using System.Security.Principal;
using System.Web;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Helpers;

namespace HR.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            // BundleConfig.RegisterBundles(BundleTable.Bundles); // Commented out: no local Scripts directory; using CDN in _Layout

            // Ensure Razor view engine is registered
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine());

            // Disable automatic migrations to prevent schema conflicts
            System.Data.Entity.Database.SetInitializer<HR.Web.Data.HrContext>(null);

            // TEMPORARILY DISABLED SEED DATA TO PREVENT CRASHES
            // Simple seed for demo users, departments, and sample hiring data
            /*
            using (var uow = new UnitOfWork())
            {
                if (!uow.Departments.GetAll().Any())
                {
                    uow.Departments.Add(new Department { Name = "Engineering" });
                    uow.Departments.Add(new Department { Name = "HR" });
                }
                if (!uow.Users.GetAll().Any())
                {
                    uow.Users.Add(new User { 
                        UserName = "admin", 
                        Email = "admin@test.com", 
                        Role = "Admin",
                        PasswordHash = PasswordHelper.HashPassword("Admin@2024!Secure"),
                        RequirePasswordChange = false,
                        LastPasswordChange = DateTime.Now
                    });
                    uow.Users.Add(new User { 
                        UserName = "client", 
                        Email = "client@test.com", 
                        Role = "Client",
                        PasswordHash = PasswordHelper.HashPassword("Client@2024!Secure"),
                        RequirePasswordChange = false,
                        LastPasswordChange = DateTime.Now
                    });
                }
                else
                {
                    // Update existing users without password hashes or with weak passwords
                    var usersWithoutPassword = uow.Users.GetAll().Where(u => string.IsNullOrEmpty(u.PasswordHash)).ToList();
                    foreach (var user in usersWithoutPassword)
                    {
                        switch (user.UserName.ToLower())
                        {
                            case "admin":
                                user.PasswordHash = PasswordHelper.HashPassword("Admin@2024!Secure");
                                break;
                            case "client":
                                user.PasswordHash = PasswordHelper.HashPassword("Client@2024!Secure");
                                break;
                            default:
                                user.PasswordHash = PasswordHelper.HashPassword(PasswordHelper.GenerateSecureRandomPassword());
                                break;
                        }
                        user.RequirePasswordChange = false;
                        user.LastPasswordChange = DateTime.Now;
                        uow.Users.Update(user);
                    }

                    // Set password change requirement for users with old weak passwords
                    var usersWithWeakPasswords = uow.Users.GetAll().Where(u => 
                        !string.IsNullOrEmpty(u.PasswordHash) && 
                        !u.RequirePasswordChange &&
                        (u.PasswordHash.Contains("10000") || // Old iteration count
                         u.LastPasswordChange == null || // No password change record
                         u.PasswordHash.Length < 50) // Likely weak password
                    ).ToList();

                    foreach (var user in usersWithWeakPasswords)
                    {
                        user.RequirePasswordChange = true;
                        user.PasswordChangeExpiry = DateTime.Now.AddDays(7); // Give 7 days to change
                        uow.Users.Update(user);
                    }
                }
                if (!uow.Positions.GetAll().Any())
                {
                    var eng = uow.Departments.GetAll().FirstOrDefault(d => d.Name == "Engineering");
                    if (eng == null)
                    {
                        eng = new Department { Name = "Engineering" };
                        uow.Departments.Add(eng);
                        uow.Complete();
                    }
                    uow.Positions.Add(new Position
                    {
                        Title = "Software Engineer",
                        Description = "Build and maintain web apps.",
                        SalaryMin = 70000,
                        SalaryMax = 110000,
                        DepartmentId = eng.Id,
                        IsOpen = true,
                        PostedOn = DateTime.UtcNow.AddDays(-7),
                        Currency = "KES"
                    });
                    var hrDep = uow.Departments.GetAll().FirstOrDefault(d => d.Name == "HR");
                    if (hrDep != null)
                    {
                        uow.Positions.Add(new Position
                        {
                            Title = "HR Generalist",
                            Description = "Support hiring and onboarding.",
                            SalaryMin = 50000,
                            SalaryMax = 80000,
                            DepartmentId = hrDep.Id,
                            IsOpen = true,
                            PostedOn = DateTime.UtcNow.AddDays(-3),
                            Currency = "KES"
                        });
                    }
                }

                if (!uow.Applicants.GetAll().Any())
                {
                    uow.Applicants.Add(new Applicant { FullName = "Alice Johnson", Email = "alice@applicant.com", Phone = "555-1234" });
                    uow.Applicants.Add(new Applicant { FullName = "Bob Smith", Email = "bob@applicant.com", Phone = "555-5678" });
                }

                // Applications + downstream records only if we have positions/applicants
                if (!uow.Applications.GetAll().Any() && uow.Positions.GetAll().Any() && uow.Applicants.GetAll().Any())
                {
                    var pos = uow.Positions.GetAll().First();
                    var applicant = uow.Applicants.GetAll().First();
                    var app = new Application
                    {
                        ApplicantId = applicant.Id,
                        PositionId = pos.Id,
                        Status = "Interviewing",
                        AppliedOn = DateTime.UtcNow.AddDays(-2),
                        ResumePath = null
                    };
                    uow.Applications.Add(app);
                    uow.Complete(); // Save to get IDs

                    // Reload app with Id
                    var savedApp = uow.Applications.GetAll().First();
                    var interviewer = uow.Users.GetAll().First(u => u.Role == "HR");
                    uow.Interviews.Add(new Interview
                    {
                        ApplicationId = savedApp.Id,
                        InterviewerId = interviewer.Id,
                        ScheduledAt = DateTime.UtcNow.AddDays(1),
                        Mode = "Remote",
                        Notes = "Initial HR screen"
                    });
                    uow.Onboardings.Add(new Onboarding
                    {
                        ApplicationId = savedApp.Id,
                        StartDate = DateTime.UtcNow.AddDays(14),
                        Tasks = "Complete paperwork; provision laptop",
                        Status = "Pending"
                    });
                }
                uow.Complete();
            }
            */
        }

        protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
        {
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null || string.IsNullOrWhiteSpace(authCookie.Value))
            {
                return;
            }

            var ticket = FormsAuthentication.Decrypt(authCookie.Value);
            if (ticket == null)
            {
                return;
            }

            var identity = new GenericIdentity(ticket.Name, "Forms");
            var roles = string.IsNullOrWhiteSpace(ticket.UserData) ? new string[] { } : new[] { ticket.UserData };
            var principal = new GenericPrincipal(identity, roles);
            HttpContext.Current.User = principal;
            System.Threading.Thread.CurrentPrincipal = principal;
        }
    }
}


