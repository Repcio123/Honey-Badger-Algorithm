using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Runtime.Loader;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Properties;

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
        [Route("/home/params")]
        [HttpGet]
        public async Task<IActionResult> Postg([FromQuery] string algorithmFileName)
        {
            string pathToAssembly = Path.Combine(_hostingEnvironment.ContentRootPath, "algorithms", algorithmFileName);
            var alc = new AssemblyLoadContext("g", true);
            Assembly assembly = alc.LoadFromAssemblyPath(pathToAssembly);

            Type? type = assembly.GetExportedTypes().FirstOrDefault(Type => Type.Name == "OptimizationAlgorithm");

            alc.Unload();
            
            object? instance = Activator.CreateInstance(type);
            var paramsInfo = type.GetProperty("ParamsInfo")?.GetValue(instance);
            return Ok(new { paramsInfo });
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
                Tuple.Create<double, double>(0, 5)
            };

            object? invokerInstance = Activator.CreateInstance(typeWithFitnessFunction);
            MethodInfo? invokerFunction = typeWithFitnessFunction.GetMethod("fitnessFunction");
            Delegate delgt = Delegate.CreateDelegate(delegateType, invokerInstance, invokerFunction);
            object? instance = Activator.CreateInstance(type);
            type.GetMethod("Solve")?.Invoke(instance, new object[]{
                delgt,
                testDomain,
                new double[] { 0.5, 1 }
            });
            var xBest = type.GetProperty("XBest")?.GetValue(instance);
            var fBest = type.GetProperty("FBest")?.GetValue(instance);
            var xBestArray = type.GetProperty("XBest")?.GetValue(instance) as double[];
            if (xBestArray != null)
            {
                var xBestString = string.Join(",\n ", xBestArray.Select(x => x.ToString("G17")));



                string pdfFilePath = Path.Combine(_hostingEnvironment.ContentRootPath, "output.pdf");
                using (var writer = new PdfWriter(pdfFilePath))
                {
                    using (var pdf = new PdfDocument(writer))
                    {
                        var document = new Document(pdf);
                        document.Add(new Paragraph("Optimization Results")
                  .SetTextAlignment(TextAlignment.CENTER)
                  .SetFontSize(20));

                        // Dodaj informacje o algorytmie i funkcji
                        document.Add(new Paragraph($"Algorithm: {algName}, Function: {funName}")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(12)
                            .SetFontColor(DeviceGray.BLACK));

                        // Dodaj sekcjê z wynikami
                        document.Add(new Paragraph("Results:")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(16));

                        // Dodaj informacje o XBest i FBest
                        document.Add(new Paragraph($"XBest: {xBestString}")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(14));

                        document.Add(new Paragraph($"FBest: {fBest}")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(14));

                        // Dodaj linie oddzielaj¹ce
                        document.Add(new LineSeparator(new SolidLine())
                            .SetMarginTop(10)
                            .SetMarginBottom(10));

                        // Dodaj stopkê
                        document.Add(new Paragraph("Generated by Honey Optimalization Interface")
                            .SetTextAlignment(TextAlignment.RIGHT)
                            .SetFontSize(10)
                            .SetFontColor(DeviceGray.GRAY));

                        // Zakoñcz dokument
                        document.Close();
                    }
                }

            }
            return Ok(new { xBest, fBest });
        }
        [Route("/home/runs")]
        [HttpPost]
        public async Task<IActionResult> RunM([FromForm] List<string> algNames, [FromForm] List<string> funNames)
        {
            object?[] tmp = new object[algNames.Count + funNames.Count];
            int i = 0;
            foreach (string alg in algNames) 
            {
                foreach (string fun in funNames)
                {
                    var result = await Run(alg, fun);
                    var cast = result as OkObjectResult;
                    var val = cast.Value;
                    tmp[i] = val;
                    i++;
                }
            }
            return Ok(new { tmp });
        }
    }
}