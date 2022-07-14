
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Collections;
using it.Data;
using it.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace it.Areas.Admin.Controllers
{

    public class MemberController : BaseController
    {
        private UserManager<UserModel> UserManager;
        private RoleManager<IdentityRole> RoleManager;
        [BindProperty]
        public InputModel Input { get; set; }
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string OldPassword { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }
        [TempData]
        public string StatusMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        public MemberController(ItContext context, UserManager<UserModel> UserMgr, RoleManager<IdentityRole> RoleMgr) : base(context)
        {
            UserManager = UserMgr;
            RoleManager = RoleMgr;
        }
        // GET: MemberController
        public async Task<IActionResult> Index()
        {

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string user_id = UserManager.GetUserId(currentUser); // Get user id:
            UserModel User = await UserManager.FindByIdAsync(user_id);
            return View(User);
        }

        // POST: MemberController/Edit
        [HttpPost]
        public async Task<IActionResult> Index(UserModel User)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string id = UserManager.GetUserId(currentUser); // Get user id:
            UserModel User_old = await UserManager.FindByIdAsync(id);
            User_old.FirstName = User.FirstName;
            User_old.LastName = User.LastName;
            User_old.FullName = User.FullName;
            User_old.image_sign = User.image_sign;
            User_old.image_url = User.image_url;
            User_old.position = User.position;

            IdentityResult result = await UserManager.UpdateAsync(User_old);
            if (result.Succeeded)
            {
                StatusMessage = "Cập nhật thành công!";

                return RedirectToAction(nameof(Index));
            }
            else
                return Ok(result);
        }



        // GET: MemberController
        public async Task<IActionResult> ChangePassword()
        {
            return View();
        }

        // POST: MemberController/Edit
        [HttpPost]
        public async Task<IActionResult> ChangePassword(InputModel Input)
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            string id = UserManager.GetUserId(currentUser); // Get user id:

            var user = await UserManager.GetUserAsync(currentUser);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await UserManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                ErrorMessage = "";
                foreach (var error in changePasswordResult.Errors)
                {
                    ErrorMessage += error.Description + "<br>";
                }
                return RedirectToAction(nameof(ChangePassword));
            }

            StatusMessage = "Mật khẩu đã được thay đổi";
            return RedirectToAction(nameof(ChangePassword));
        }

    }
}
