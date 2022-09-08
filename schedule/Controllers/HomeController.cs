
using it.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using it.Data;

namespace it.Controllers
{

    public class HomeController : Controller
    {


        private readonly IConfiguration _configuration;
        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return Redirect("/Admin");
        }
        public IActionResult RemoveSignIn()
        {

            ////Remove Cookie
            Response.Cookies.Delete(_configuration["JWT:NameCookieAuth"], new CookieOptions()
            {
                Domain = _configuration["JWT:Domain"]
            });
            return Redirect("/Admin");
        }


    }

}
