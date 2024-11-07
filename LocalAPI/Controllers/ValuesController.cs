using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;
namespace MyLocalApi.Controllers

{
    [ApiController]
    [Route("{*url}")]
    public class HelloController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public HelloController(IWebHostEnvironment env)
        {
            _env = env;
        }
        [HttpGet]
        public IActionResult Get()
        {
            var filePath = Path.Combine(_env.ContentRootPath, "C:/Users/Jonat/source/repos/AddressVasker/LocalAPI/Controllers/response.txt");
            string jsonContent = System.IO.File.ReadAllText(filePath);
            return Content(jsonContent, "application/json");
        }
    }
}
