using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using GamePanel.Models;

namespace GamePanel.Controllers
{
    public class UsersController : Controller
    {
        ModelDataContext _context = new ModelDataContext(System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString);

        public IFormsAuthenticationService FormsService { get; set; }
        public IMembershipService MembershipService { get; set; }

        protected override void Initialize(RequestContext requestContext)
        {
            if (FormsService == null) { FormsService = new FormsAuthenticationService(); }
            if (MembershipService == null) { MembershipService = new AccountMembershipService(); }

            base.Initialize(requestContext);
        }

        [Authorize(Roles="Admin")]
        public ActionResult Index()
        {
            ViewData.Model = _context.Users.ToList();

            return View();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = Guid.NewGuid();

                // Attempt to register the user
                MembershipCreateStatus createStatus = MembershipService.CreateUser(model.UserName, model.Password, model.Email, userId);

                if (createStatus == MembershipCreateStatus.Success)
                {
                    _context.Users.InsertOnSubmit(new User()
                    {
                        Id = userId,
                        Credits = 0
                    });

                    _context.SubmitChanges();

                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", AccountValidation.ErrorCodeToString(createStatus));
                }
            }

            return View(model);
        }
 
                                               
        [Authorize(Roles = "Admin")]
        public bool Credits(Guid id, int credits)
        {
            _context.Users.Single(p => p.Id == id).Credits = credits;
            _context.SubmitChanges();

            return true;
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Delete(Guid id)
        {
            var user = _context.Users.Single(p => p.Id == id);
            MembershipService.DeleteUser(user.PhysicalUser.UserName);

            _context.Users.DeleteOnSubmit(user);
            _context.SubmitChanges();

            return RedirectToAction("Index");
        }

        // **************************************
        // URL: /Account/LogOn
        // **************************************

        public ActionResult LogOn()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (MembershipService.ValidateUser(model.UserName, model.Password))
                {
                    FormsService.SignIn(model.UserName, model.RememberMe);
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Servers");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // **************************************
        // URL: /Account/LogOff
        // **************************************

        public ActionResult LogOff()
        {
            FormsService.SignOut();

            return RedirectToAction("Index", "Servers");
        }

        // **************************************
        // URL: /Account/ChangePassword
        // **************************************

        [Authorize]
        public ActionResult ChangePassword()
        {
            ViewBag.PasswordLength = MembershipService.MinPasswordLength;
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                if (MembershipService.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword))
                {
                    return RedirectToAction("Index", "Servers");
                }
                else
                {
                    ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.PasswordLength = MembershipService.MinPasswordLength;
            return View(model);
        }

        // **************************************
        // URL: /Account/ChangePasswordSuccess
        // **************************************

        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }

    }
}
