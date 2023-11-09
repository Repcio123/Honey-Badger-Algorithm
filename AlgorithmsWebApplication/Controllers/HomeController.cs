using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace AlgorithmsWebApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private IHostEnvironment _hostingEnvironment;

        public HomeController(ILogger<HomeController> logger, IHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            string uploads = Path.Combine(_hostingEnvironment.ContentRootPath, "algorithms");
            DirectoryInfo di = new DirectoryInfo(uploads);
            FileInfo[] files = di.GetFiles();
            return Ok(files.Select(file => file.Name));
        }
        [Route("/home/fun")]
        [HttpGet]
        public async Task<IActionResult> Getf()
        {
            string uploads = Path.Combine(_hostingEnvironment.ContentRootPath, "functions");
            DirectoryInfo di = new DirectoryInfo(uploads);
            FileInfo[] files = di.GetFiles();
            return Ok(files.Select(file => file.Name));
        }
        
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            IFormFile? file = Request.Form.Files.FirstOrDefault();
            string uploads = Path.Combine(_hostingEnvironment.ContentRootPath, "algorithms");
            if (file != null && file.Length > 0)
            {
                string filePath = Path.Combine(uploads, file.FileName);
                using (Stream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                return Redirect("/static/tempFront.html");
            }
            return BadRequest();
        }
        [Route("/home/fun")]
        [HttpPost]
        public async Task<IActionResult> Postf()
        {
            IFormFile? file = Request.Form.Files.FirstOrDefault();
            string uploads = Path.Combine(_hostingEnvironment.ContentRootPath, "functions");
            if (file != null && file.Length > 0)
            {
                string filePath = Path.Combine(uploads, file.FileName);
                using (Stream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                return Redirect("/static/tempFront.html");
            }
            return BadRequest();
        }
        public class ParamInfo
        {
            public string Name { get; set; } = null!;
            public string Description { get; set; } = null!;
            public double UpperBoundary { get; set; }
            public double LowerBoundary { get; set; }
        }

        //public delegate double fitnessFunction(double[] args);

        [Route("/home/run")]
        [HttpPost]
        public async Task<IActionResult> Run([FromForm] string algName, [FromForm] string funName)
        {
            string dll = Path.Combine(_hostingEnvironment.ContentRootPath, "algorithms", algName);
            Assembly assembly = Assembly.LoadFrom(dll);

            Type? type = assembly.GetExportedTypes().FirstOrDefault(Type => Type.Name == "OptimizationAlgorithm");

            string funDll = Path.Combine(_hostingEnvironment.ContentRootPath, "functions", funName);
            Assembly funAssembly = Assembly.LoadFrom(funDll);

            Type typeWithFitnessFunction = funAssembly.GetTypes().First(type => type.GetMethods().Any(m => m.Name == "fitnessFunction"));
            Type? delegateType = assembly.GetExportedTypes().FirstOrDefault(Type => Type.Name == "fitnessFunction");

            Tuple<double, double>[] testDomain = {
                Tuple.Create<double, double>(0, 5),
                Tuple.Create<double, double>(0, 5),
            };

            object? invokerInstance = Activator.CreateInstance(typeWithFitnessFunction);
            MethodInfo? invokerFunction = typeWithFitnessFunction.GetMethod("fitnessFunction");
            Delegate delgt = Delegate.CreateDelegate(delegateType, invokerInstance, invokerFunction);

            object? instance = Activator.CreateInstance(type);
            type.GetMethod("Solve")?.Invoke(instance, new object[] {
                delgt,
                testDomain,
                new double[] { 0.5, 1 }
            });
            var xBest = type.GetProperty("XBest")?.GetValue(instance);
            var fBest = type.GetProperty("FBest")?.GetValue(instance);
            return Ok(new { xBest, fBest });
        }
    }
}