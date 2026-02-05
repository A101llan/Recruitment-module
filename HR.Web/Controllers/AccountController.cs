using System;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IEmailService _emailService = new EmailService();

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

            // Verify the password against stored password hash
            bool isValidPassword = false;
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
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
                (user.PasswordChangeExpiry.HasValue && user.PasswordChangeExpiry.Value < DateTime.Now);

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

                var message = "For security reasons, you must change your password before continuing.";
                
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
                var checkAuthCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                bool wasForcedChange = false;
                
                if (checkAuthCookie != null)
                {
                    var ticket = FormsAuthentication.Decrypt(checkAuthCookie.Value);
                    if (ticket != null && ticket.UserData.Contains("RequirePasswordChange"))
                    {
                        wasForcedChange = true;
                        // Issue new regular auth cookie
                        var newUserRole = string.IsNullOrWhiteSpace(user.Role) ? "Client" : user.Role;
                        var newTicket = new FormsAuthenticationTicket(
                            1,
                            username,
                            DateTime.Now,
                            DateTime.Now.AddHours(8),
                            false,
                            newUserRole);
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
                }
                else
                {
                    TempData["SuccessMessage"] = "Your password has been successfully updated!";
                }
                
                // Redirect based on user role for both forced and voluntary changes
                var userRole = string.IsNullOrWhiteSpace(user.Role) ? "Client" : user.Role;
                if (string.Equals(userRole, "Client", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Positions");
                }
                else if (string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    // Bypass AdminController entirely - go directly to a known working action
                    return RedirectToAction("Index", "Dashboard");
                }
                return RedirectToAction("Index", "Dashboard");
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
            // Show application message if coming from application attempt
            if (TempData["ApplicationMessage"] != null)
            {
                ViewBag.ApplicationMessage = TempData["ApplicationMessage"].ToString();
                ViewBag.ReturnUrl = TempData["ReturnUrl"]?.ToString();
            }
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Register(Models.RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Additional email validation for "completeness" (ensure domain has a dot)
            if (!model.Email.Contains("@") || !model.Email.Split('@').Last().Contains("."))
            {
                ModelState.AddModelError("Email", "Please enter a valid and complete email address.");
                return View(model);
            }

            // Case-insensitive check for existing username
            var existingUser = _uow.Users.GetAll().FirstOrDefault(u => 
                u.UserName.Equals(model.UserName, StringComparison.OrdinalIgnoreCase));
            
            if (existingUser != null)
            {
                ModelState.AddModelError("UserName", "This username is already taken.");
                return View(model);
            }

            // Case-insensitive check for existing email
            var existingEmail = _uow.Users.GetAll().FirstOrDefault(u => 
                u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
            
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
                return View(model);
            }

            // Check for password confirmation matches
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
                return View(model);
            }

            // Strict password strength validation
            if (!HR.Web.Helpers.PasswordHelper.IsPasswordStrong(model.Password))
            {
                ModelState.AddModelError("Password", HR.Web.Helpers.PasswordHelper.GetPasswordStrengthMessage());
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

                // Check if there's a return URL (from application attempt)
                var returnUrl = Request.Form["ReturnUrl"];
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Positions");
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

        // Forgot Password Actions
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Find user by email
                    var user = _uow.Users.GetAll().FirstOrDefault(u => u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
                    
                    if (user != null)
                    {
                        // Generate secure token
                        var token = GenerateSecureToken();
                        var expiryDate = DateTime.UtcNow.AddHours(24); // 24 hour expiry

                        // Invalidate any existing tokens for this user
                        var existingTokens = _uow.PasswordResets.GetAll().Where(t => t.UserId == user.Id && !t.IsUsed);
                        foreach (var existingToken in existingTokens)
                        {
                            existingToken.IsUsed = true;
                        }

                        // Create new password reset token
                        var passwordReset = new PasswordReset
                        {
                            UserId = user.Id,
                            Token = token,
                            ExpiryDate = expiryDate,
                            CreatedDate = DateTime.UtcNow
                        };

                        _uow.PasswordResets.Add(passwordReset);
                        _uow.Complete();

                        // Generate reset link
                        var resetUrl = Url.Action("ResetPassword", "Account", new { token = token }, Request.Url.Scheme);
                        
                        // Debug: Log what we're doing
                        System.Diagnostics.Debug.WriteLine("Sending password reset email to: " + user.Email);
                        System.Diagnostics.Debug.WriteLine("Reset URL: " + resetUrl);
                        
                        // Send email
                        if (_emailService != null)
                        {
                            await _emailService.SendPasswordResetEmailAsync(user.Email, resetUrl);
                            System.Diagnostics.Debug.WriteLine("Email sent successfully");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Email service is null");
                            throw new Exception("Email service is not initialized");
                        }

                        // Log the password reset request
                        _auditService.LogAction(user.UserName, "PASSWORD_RESET_REQUEST", "Account", user.Id.ToString(), 
                            $"Password reset requested for email: {user.Email}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No user found with email: " + model.Email);
                    }

                    // Always show success message to prevent email enumeration attacks
                    ViewBag.SuccessMessage = "If an account with that email exists, a password reset link has been sent.";
                    return View(model);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null) errorMessage += " Inner Error: " + ex.InnerException.Message;
                    
                    _auditService.LogAction("SYSTEM", "PASSWORD_RESET_ERROR", "Account", "", 
                        $"Password reset failed: {errorMessage}");
                    var fullMessage = "An error occurred while processing your request: " + errorMessage;
                    ModelState.AddModelError("", fullMessage);
                    ViewBag.ErrorMessage = fullMessage;
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.ErrorMessage = "Invalid password reset token.";
                return View();
            }

            // Validate token
            var passwordReset = _uow.PasswordResets.GetAll()
                .FirstOrDefault(t => t.Token == token && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow);

            if (passwordReset == null)
            {
                ViewBag.ErrorMessage = "This password reset link is invalid or has expired.";
                return View();
            }

            var model = new ResetPasswordViewModel { Token = token };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Validate token again
                    var passwordReset = _uow.PasswordResets.GetAll()
                        .FirstOrDefault(t => t.Token == model.Token && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow);

                    if (passwordReset == null)
                    {
                        ViewBag.ErrorMessage = "This password reset link is invalid or has expired.";
                        return View(model);
                    }

                    // Get user
                    var user = _uow.Users.Get(passwordReset.UserId);
                    if (user == null)
                    {
                        ViewBag.ErrorMessage = "User not found.";
                        return View(model);
                    }

                    // Validate password strength
                    if (!PasswordHelper.IsPasswordStrong(model.NewPassword))
                    {
                        ModelState.AddModelError("", PasswordHelper.GetPasswordStrengthMessage());
                        return View(model);
                    }

                    // Update password
                    user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
                    user.RequirePasswordChange = false;
                    user.LastPasswordChange = DateTime.UtcNow;
                    user.PasswordChangeExpiry = null;

                    // Mark token as used
                    passwordReset.IsUsed = true;

                    _uow.Complete();

                    // Log successful password reset
                    _auditService.LogAction(user.UserName, "PASSWORD_RESET_SUCCESS", "Account", user.Id.ToString(), 
                        "Password was successfully reset");

                    ViewBag.SuccessMessage = "Your password has been successfully reset. You can now login with your new password.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    _auditService.LogAction("SYSTEM", "PASSWORD_RESET_ERROR", "Account", "", 
                        $"Password reset completion failed: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while resetting your password. Please try again.");
                }
            }

            return View(model);
        }

        private string GenerateSecureToken()
        {
            // Generate a cryptographically secure token
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                // Return a clean alphanumeric string
                return Convert.ToBase64String(bytes)
                    .Replace("/", "")
                    .Replace("+", "")
                    .Replace("=", "")
                    .Substring(0, 32);
            }
        }
    }
}
