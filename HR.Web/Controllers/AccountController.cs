using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Helpers;
using HR.Web.Services;

namespace HR.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();
        private readonly SecurityService _securityService = new SecurityService();
        private readonly AuditService _auditService = new AuditService();

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Login(string username, string password, string role, string returnUrl)
        {
            var clientIP = Request.UserHostAddress;
            
            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError("", "Username is required.");
                _securityService.RecordLoginAttempt(username, clientIP, false, "Username required");
                _auditService.LogLogin(username, false, "Username required");
                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Password is required.");
                _securityService.RecordLoginAttempt(username, clientIP, false, "Password required");
                _auditService.LogLogin(username, false, "Password required");
                return View();
            }

            // Check if account is locked
            if (_securityService.IsAccountLocked(username))
            {
                var lockoutEndTime = _securityService.GetLockoutEndTime(username);
                var remainingTime = lockoutEndTime.HasValue 
                    ? lockoutEndTime.Value - DateTime.Now 
                    : TimeSpan.Zero;
                
                ModelState.AddModelError("", $"Account is locked. Please try again in {remainingTime.Minutes} minutes.");
                _securityService.RecordLoginAttempt(username, clientIP, false, "Account locked");
                _auditService.LogLogin(username, false, $"Account locked. Try again in {remainingTime.Minutes} minutes");
                return View();
            }

            // Look up user by username
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == username);
            if (user == null)
            {
                var remainingAttempts = _securityService.GetRemainingAttempts(username);
                ModelState.AddModelError("", $"Invalid username or password. {remainingAttempts} attempts remaining.");
                _securityService.RecordLoginAttempt(username, clientIP, false, "Invalid username");
                _auditService.LogLogin(username, false, "Invalid username");
                return View();
            }

            // Verify the password
            if (string.IsNullOrEmpty(user.PasswordHash) || !PasswordHelper.VerifyPassword(user.PasswordHash, password))
            {
                var remainingAttempts = _securityService.GetRemainingAttempts(username);
                var warningMessage = remainingAttempts > 1 
                    ? $"Invalid username or password. {remainingAttempts} attempts remaining."
                    : $"Invalid username or password. {remainingAttempts} attempt remaining before account lockout.";
                
                ModelState.AddModelError("", warningMessage);
                _securityService.RecordLoginAttempt(username, clientIP, false, "Invalid password");
                _auditService.LogLogin(username, false, "Invalid password");
                return View();
            }

            // Successful login - record attempt and clear failed attempts
            _securityService.RecordLoginAttempt(username, clientIP, true);
            _securityService.ClearFailedAttempts(username);
            
            // Log successful login
            _auditService.LogLogin(username, true);

            // Use the user's stored role from the database
            var userRole = string.IsNullOrWhiteSpace(user.Role) ? "Client" : user.Role;

            // Issue forms auth cookie with role in UserData
            var ticket = new FormsAuthenticationTicket(
                1,
                username,
                DateTime.Now,
                DateTime.Now.AddHours(8),
                false,
                userRole);
            var encrypted = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
            {
                HttpOnly = true
            };
            Response.Cookies.Add(cookie);

            if (Url.IsLocalUrl(returnUrl) && !string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            if (string.Equals(userRole, "Client", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Positions");
            }
            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize]
        public ActionResult Logout()
        {
            var username = User.Identity.Name;
            FormsAuthentication.SignOut();
            _auditService.LogLogout(username);
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Register(Models.RegisterViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View(model);
            }

            // Check if username already exists
            var existingUser = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == model.UserName);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Username already exists. Please choose a different username.");
                return View(model);
            }

            // Assign default role of "Client" for all new registrations
            var defaultRole = "Client";

            // Create User entity with hashed password
            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                Role = defaultRole,
                PasswordHash = PasswordHelper.HashPassword(model.Password)
            };
            _uow.Users.Add(user);
            _uow.Complete();

            // If registering as an applicant, also create Applicant record
            if (defaultRole == "Client")
            {
                var applicant = new Applicant
                {
                    FullName = model.UserName,
                    Email = model.Email,
                    Phone = model.Phone
                };
                _uow.Applicants.Add(applicant);
                _uow.Complete();
            }

            // Auto-login the newly registered user
            var ticket = new FormsAuthenticationTicket(
                1,
                model.UserName,
                DateTime.Now,
                DateTime.Now.AddHours(8),
                false,
                defaultRole);
            var encrypted = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
            {
                HttpOnly = true
            };
            Response.Cookies.Add(cookie);

            return RedirectToAction("Index", "Dashboard");
        }
    }
}


