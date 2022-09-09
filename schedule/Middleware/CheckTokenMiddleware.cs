using it.Areas.Admin.Models;
using it.Data;
using Microsoft.AspNetCore.Identity;
using System.Net.Http.Headers;

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
        public async Task Invoke(HttpContext httpContext, ItContext _context, TokenContext _contextToken, SignInManager<UserModel> _signInManager, IConfiguration _configuration)
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
                if (path.ToLower().StartsWith(item.ToLower()))
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
                    var client = new HttpClient();

                    client.DefaultRequestHeaders.Accept
                        .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
                    var url = _configuration["JWT:ValidIssuer"] + "/api/tokeninfo?token=" + Token;
                    var response = await client.GetAsync(url);
                    InfoResponse responseJson = await response.Content.ReadFromJsonAsync<InfoResponse>();
                    if (responseJson.success)
                    {
                        var email = responseJson.email;
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
    public class InfoResponse
    {
        public bool success { get; set; }
        public string? email { get; set; }
    }
}
