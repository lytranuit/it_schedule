
using it.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using it.Data;

namespace it.Controllers
{

    public class HomeController : Controller
    {


        public HomeController()
        {

        }

        public IActionResult Index()
        {
            return Redirect("/Admin");
        }



    }

}
