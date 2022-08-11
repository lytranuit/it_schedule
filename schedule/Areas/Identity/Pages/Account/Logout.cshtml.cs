// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace it.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<UserModel> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        private UserManager<UserModel> UserManager;
        protected readonly ItContext _context;
        public LogoutModel(SignInManager<UserModel> signInManager, ILogger<LogoutModel> logger, ItContext context, UserManager<UserModel> UserMgr)
        {
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            UserManager = UserMgr;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            /// Audittrail
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            var audit = new AuditTrailsModel();
            audit.UserId = user_id;
            audit.Type = AuditType.Logout.ToString();
            audit.DateTime = DateTime.Now;
            _context.Add(audit);
            _context.SaveChanges();
            ////
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}
