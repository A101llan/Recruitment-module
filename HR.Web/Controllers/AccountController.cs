using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using HR.Web.Data;
using HR.Web.Models;

namespace HR.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UnitOfWork _uow = new UnitOfWork();

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
            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError("", "Username is required.");
                return View();
            }

            // Look up user by username only - role is determined automatically from database
            var user = _uow.Users.GetAll().FirstOrDefault(u => u.UserName == username);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username. Please check your credentials.");
                return View();
            }

            // Use the user's stored role from the database
            var userRole = string.IsNullOrWhiteSpace(user.Role) ? "Client" : user.Role;

            // Demo auth: accept any password for demo purposes and issue forms auth cookie with role in UserData
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
            FormsAuthentication.SignOut();
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
            if (model == null || string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Role) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View(model);
            }

            // Create User entity (do not persist password for demo to avoid schema changes)
            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                Role = model.Role
            };
            _uow.Users.Add(user);
            _uow.Complete();

            // Auto-login the newly registered user (accept any password for demo)
            var ticket = new FormsAuthenticationTicket(
                1,
                model.UserName,
                DateTime.Now,
                DateTime.Now.AddHours(8),
                false,
                model.Role);
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


