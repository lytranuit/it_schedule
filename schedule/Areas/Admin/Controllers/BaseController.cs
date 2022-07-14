using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using it.Data;
namespace it.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "Administrator")]
    [Authorize]
    public class BaseController : Controller
    {
        protected readonly ItContext _context;

        public BaseController(ItContext context)
        {
            _context = context;
        }
    }
}
