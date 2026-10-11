using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QKZTrainingCenter.Data;

namespace QKZTrainingCenter.Controllers
{
    public class CoursesController : Controller
    {
        private readonly QKZDbContext _context;

        public CoursesController(QKZDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses
                .OrderBy(c => c.CourseID)
                .ToListAsync();

            return View(courses);
        }

        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return NotFound();
            }

            return course.CourseName switch
            {
                "Grade E" => View("GradeE"),
                "Grade D" => View("GradeD"),
                "Grade C" => View("GradeC"),
                "Grade B" => View("GradeB"),
                "Grade A" => View("GradeA"),
                _ => NotFound()
            };
        }
    }
}