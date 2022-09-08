using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;

namespace schedule.Middleware
{
    public class CheckTokenMiddleware
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        // Lưu middlewware tiếp theo trong Pipeline
        private readonly RequestDelegate _next;
        public CheckTokenMiddleware(RequestDelegate next, IHttpContextAccessor httpContextAccessor)
        {
            _next = next;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task Invoke(HttpContext httpContext, ItContext _context, TokenContext _contextToken, SignInManager<UserModel> _signInManager)
        {

            bool islogin = httpContext.User.Identity.IsAuthenticated;
            string Token = _httpContextAccessor.HttpContext.Request.Cookies["Auth-Token"];
            var path = (string)_httpContextAccessor.HttpContext.Request.Path;
            var except = new List<string>()
            {
                "/Identity/Account/AccessDenied",
                "/Identity/Account/Logout",
                "/Home/RemoveSignIn"
            };
            foreach (var item in except)
            {
                if (path.ToLower().Contains(item.ToLower()))
                {
                    islogin = true;
                    break;
                }
            }
            //Console.WriteLine("Path: " + path);
            //Console.WriteLine("CheckLogin: " + islogin);
            if (islogin)
            {
                await _next(httpContext);
            }
            else
            {
                //Console.WriteLine("CheckTokebMiddleware: " + Token);
                if (Token != null)
                {
                    var find = _contextToken.TokenModel.Where(d => d.token == Token && d.vaild_to > DateTime.Now && d.deleted_at == null).FirstOrDefault();
                    if (find != null)
                    {
                        var email = find.email;
                        var user = _context.UserModel.Where(d => d.Email == email).FirstOrDefault();
                        if (user != null)
                        {
                            await _signInManager.SignInAsync(user, true);
                        }
                        else
                        {

                            httpContext.Response.Redirect("/Identity/Account/AccessDenied");
                        }
                    }
                }
                await _next(httpContext);
            }

        }
    }
}
