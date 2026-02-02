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

            // Verify the password - allow default password for all users
            const string defaultPassword = "Temp123!";
            bool isDefaultPassword = password == defaultPassword;
            bool isValidPassword = false;
            
            if (isDefaultPassword)
            {
                // Allow login with default password for any user
                isValidPassword = true;
            }
            else if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                // Verify against stored password hash
                isValidPassword = PasswordHelper.VerifyPassword(user.PasswordHash, password);
            }

            if (!isValidPassword)
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

            // Check if user needs to change password
            bool requirePasswordChange = user.RequirePasswordChange || 
                (user.PasswordChangeExpiry.HasValue && user.PasswordChangeExpiry.Value < DateTime.Now) ||
                isDefaultPassword; // Force change if using default password

            if (requirePasswordChange)
            {
                // Issue temporary auth cookie for password change only
                var tempTicket = new FormsAuthenticationTicket(
                    1,
                    username,
                    DateTime.Now,
                    DateTime.Now.AddMinutes(30), // Shorter timeout for password change
                    false,
                    userRole + "|RequirePasswordChange");
                var tempEncrypted = FormsAuthentication.Encrypt(tempTicket);
                var tempCookie = new HttpCookie(FormsAuthentication.FormsCookieName, tempEncrypted)
                {
                    HttpOnly = true
                };
                Response.Cookies.Add(tempCookie);

                var message = isDefaultPassword 
                    ? "You are using a default password. For security reasons, you must change your password before continuing."
                    : "For security reasons, you must change your password before continuing.";
                
                TempData["PasswordChangeMessage"] = message;
                TempData["UserName"] = username;
                return RedirectToAction("ChangePassword", "Account");
            }

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
        public ActionResult ChangePassword()
        {
            // Clear anti-forgery tokens to prevent mismatch after login
            if (Request.Cookies["__RequestVerificationToken"] != null)
            {
                var cookie = new HttpCookie("__RequestVerificationToken", "");
                cookie.Expires = DateTime.Now.AddMonths(-20);
                Response.Cookies.Add(cookie);
            }

            // Check if user is required to change password
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket != null && ticket.UserData.Contains("RequirePasswordChange"))
                {
                    ViewBag.ForcePasswordChange = true;
                    ViewBag.Message = TempData["PasswordChangeMessage"] as string;
                }
            }

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var username = User.Identity.Name;
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == username);
            
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            // Check if this is a forced password change
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            bool isForcedChange = false;
            
            if (authCookie != null)
            {
                var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket != null && ticket.UserData.Contains("RequirePasswordChange"))
                {
                    isForcedChange = true;
                }
            }

            // For forced password changes, skip current password verification
            if (!isForcedChange)
            {
                // Verify current password
                if (!PasswordHelper.VerifyPassword(user.PasswordHash, model.CurrentPassword))
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    _auditService.LogAction(username, "PASSWORD_CHANGE_FAILED", "Account", user.Id.ToString(), 
                        "Current password verification failed");
                    return View(model);
                }
            }

            // Check if new password meets security requirements
            if (!PasswordHelper.IsPasswordStrong(model.NewPassword))
            {
                ModelState.AddModelError("", PasswordHelper.GetPasswordStrengthMessage());
                return View(model);
            }

            // Check if new password is different from current password
            if (PasswordHelper.VerifyPassword(user.PasswordHash, model.NewPassword))
            {
                ModelState.AddModelError("", "New password must be different from current password.");
                return View(model);
            }

            try
            {
                // Update password
                user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
                user.RequirePasswordChange = false;
                user.LastPasswordChange = DateTime.Now;
                user.PasswordChangeExpiry = null;
                
                _uow.Users.Update(user);
                _uow.Complete();

                // Log successful password change
                _auditService.LogAction(username, "PASSWORD_CHANGED", "Account", user.Id.ToString(), 
                    "Password successfully changed to meet security requirements");

                // Check if this was a forced password change
                var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                bool wasForcedChange = false;
                
                if (authCookie != null)
                {
                    var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                    if (ticket != null && ticket.UserData.Contains("RequirePasswordChange"))
                    {
                        wasForcedChange = true;
                        // Issue new regular auth cookie
                        var userRole = string.IsNullOrWhiteSpace(user.Role) ? "Client" : user.Role;
                        var newTicket = new FormsAuthenticationTicket(
                            1,
                            username,
                            DateTime.Now,
                            DateTime.Now.AddHours(8),
                            false,
                            userRole);
                        var newEncrypted = FormsAuthentication.Encrypt(newTicket);
                        var newCookie = new HttpCookie(FormsAuthentication.FormsCookieName, newEncrypted)
                        {
                            HttpOnly = true
                        };
                        Response.Cookies.Add(newCookie);
                    }
                }

                if (wasForcedChange)
                {
                    TempData["SuccessMessage"] = "Your password has been successfully updated! You can now access the system with your new secure password.";
                    
                    // Redirect based on user role
                    var userRole = string.IsNullOrWhiteSpace(user.Role) ? "Client" : user.Role;
                    if (string.Equals(userRole, "Client", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Positions");
                    }
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    ViewBag.SuccessMessage = "Your password has been successfully updated!";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while changing your password. Please try again.");
                _auditService.LogAction(username, "PASSWORD_CHANGE_ERROR", "Account", user.Id.ToString(), 
                    $"Password change failed: {ex.Message}");
                return View(model);
            }
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
        public ActionResult Index()
        {
            return View();
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

            // Enhanced password validation
            if (!HR.Web.Helpers.PasswordHelper.IsPasswordStrong(model.Password))
            {
                ModelState.AddModelError("", HR.Web.Helpers.PasswordHelper.GetPasswordStrengthMessage());
                return View(model);
            }

            // Check if password confirmation matches
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Password and confirmation password do not match.");
                return View(model);
            }

            // Check if username already exists
            var existingUser = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == model.UserName);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Username already exists. Please choose a different username.");
                return View(model);
            }

            // Check if email already exists
            var existingEmail = _uow.Users.GetAll().FirstOrDefault(u => u.Email == model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("", "Email address is already registered. Please use a different email.");
                return View(model);
            }

            // Assign default role of "Client" for all new registrations
            var defaultRole = "Client";

            try
            {
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

                // Log successful registration
                _auditService.LogAction(User.Identity.Name, "REGISTER", "Account", user.Id.ToString(), 
                    $"New user registered: {user.UserName} ({user.Email})");

                // Auto-login the newly registered user
                var ticket = new FormsAuthenticationTicket(
                    1,
                    model.UserName,
                    DateTime.Now,
                    DateTime.Now.AddMinutes(30),
                    false,
                    defaultRole,
                    FormsAuthentication.FormsCookiePath);

                var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                cookie.HttpOnly = true;
                Response.Cookies.Add(cookie);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Log the error
                _auditService.LogAction(User.Identity.Name, "REGISTER_ERROR", "Account", "", 
                    $"Registration failed: {ex.Message}");
                
                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(model);
            }
        }
    }
}
