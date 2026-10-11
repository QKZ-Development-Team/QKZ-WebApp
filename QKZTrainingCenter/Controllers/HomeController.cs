using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QKZTrainingCenter.Data;
using QKZTrainingCenter.Models;

namespace QKZTrainingCenter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TestDatabase([FromServices] QKZDbContext db)
        {
            var connected = await db.TestConnectionAsync();

            return Content(connected
                ? "Azure database connection successful."
                : "Azure database connection failed.");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}