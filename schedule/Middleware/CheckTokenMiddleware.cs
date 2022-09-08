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
            //Console.WriteLine("CheckTokebMiddleware: " + islogin);
            if (islogin)
            {
                await _next(httpContext);
            }
            else
            {
                string Token = _httpContextAccessor.HttpContext.Request.Cookies["Auth-Token"];
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
                    }
                }
                Console.WriteLine("CheckTokebMiddleware: " + Token);
                await _next(httpContext);
            }

        }
    }
}
