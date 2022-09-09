// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using it.Areas.Admin.Models;
using it.Data;
using NuGet.Protocol.Plugins;
using System.Net.Http.Headers;

namespace it.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<UserModel> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private UserManager<UserModel> UserManager;

        protected readonly ItContext _context;
        private readonly IConfiguration _configuration;

        public LoginModel(SignInManager<UserModel> signInManager, ILogger<LoginModel> logger, ItContext context, UserManager<UserModel> UserMgr, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            UserManager = UserMgr;
            _configuration = configuration;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Ghi nhớ?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Admin");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var client = new HttpClient();

                client.DefaultRequestHeaders.Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
                var values = new Dictionary<string, string>
                {
                    { "email", Input.Email },
                    { "password", Input.Password }
                };
                var content = new FormUrlEncodedContent(values);
                var url = _configuration["JWT:ValidIssuer"] + "/api/login";
                var response = await client.PostAsync(url, content);
                LoginResponse responseJson = await response.Content.ReadFromJsonAsync<LoginResponse>();
                var pass = Input.Password;
                if (responseJson.authed == true)
                {
                    pass = "!PMP_it123456";
                    var token_string = responseJson.token;
                    ////
                    Response.Cookies.Append(
                        _configuration["JWT:NameCookieAuth"],
                        token_string,
                        new CookieOptions()
                        {
                            Domain = _configuration["JWT:Domain"],
                            Expires = DateTime.Now.AddHours(Int64.Parse(_configuration["JWT:Expire"]))
                        }
                    );
                }
                var result = await _signInManager.PasswordSignInAsync(Input.Email, pass, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    /// Audittrail
                    var user = _context.UserModel.Where(x => x.Email == Input.Email).FirstOrDefault();
                    var audit = new AuditTrailsModel();
                    audit.UserId = user.Id;
                    audit.Type = AuditType.Login.ToString();
                    audit.DateTime = DateTime.Now;
                    _context.Add(audit);
                    _context.SaveChanges();
                    ////
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    /// Audittrail
                    var audit = new AuditTrailsModel();
                    audit.Type = AuditType.LoginFailed.ToString();
                    audit.DateTime = DateTime.Now;
                    audit.NewValues = Input.Email;
                    _context.Add(audit);
                    _context.SaveChanges();
                    ////
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }

    public class LoginResponse
    {
        [Key]
        public bool authed { get; set; }

        public string? error { get; set; }
        public string? parameter { get; set; }

        public string? session { get; set; }
        public string? user { get; set; }
        public string? token { get; set; }
    }
}
