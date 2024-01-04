using HoneyBagder.MiscInterfaces;
using HoneyBagder.StateReader;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;

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

        object? getParameters(string algorithmFileName)
        {
            string pathToAssembly = Path.Combine(_hostingEnvironment.ContentRootPath, "algorithms", algorithmFileName);
            var alc = new AssemblyLoadContext("g", true);
            Assembly assembly = alc.LoadFromAssemblyPath(pathToAssembly);

            Type? type = assembly.GetExportedTypes().FirstOrDefault(Type => Type.Name == "OptimizationAlgorithm");

            alc.Unload();

            object? instance = Activator.CreateInstance(type);
            var paramsInfo = type.GetProperty("ParamsInfo")?.GetValue(instance);
            return paramsInfo;
        }

        [Route("/home/params")]
        [HttpGet]
        public async Task<IActionResult> Postg([FromQuery] string algorithmFileName)
        {
            var paramsInfo = getParameters(algorithmFileName);
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

        public interface Parameter
        {
            string Name { get; set; }
        }

        [Route("/home/run")]
        [HttpPost]
        public async Task<IActionResult> Run([FromForm] string algName, [FromForm] string funName)
        {
            WriterObserver writerObserver = new WriterObserver(_hostingEnvironment.ContentRootPath);
            string dll = Path.Combine(_hostingEnvironment.ContentRootPath, "algorithms", algName);
            Assembly assembly = Assembly.LoadFrom(dll);
            IEnumerable parameters = getParameters(algName) as IEnumerable;
            //First parameter-upperBound: 4
            //First parameter-lowerBound: 1
            //First parameter-step: 0
            //ihabenowillto - upperBound: 6
            //ihabenowillto - lowerBound: 1
            //ihabenowillto - step: 0
            //Third - upperBound: 6
            //Third - lowerBound: 1
            if (parameters == null)
            {
                throw new Exception("Coœ jest nie tak z parametrami algorytmu");
            }

            List<double> passedInitialParameters = new List<double> { };
            foreach (string key in Request.Form.Keys)
            {
                if (key.EndsWith("lowerBound"))
                {
                    passedInitialParameters.Add(Convert.ToDouble(Request.Form[key]));
                }
            }
            //foreach (Parameter parameter in parameters)
            //{
            //    typeof(Parameter).GetProperty($"{parameter.Name}-lowerBound").GetValue(parameter);
            //}

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
            type.GetMethod("Attach").Invoke(instance, new object[]
            {
                writerObserver
            });
            type.GetMethod("Solve")?.Invoke(instance, new object[]{
                delgt,
                testDomain,
                passedInitialParameters.ToArray()
            });
            type.GetMethod("Detach").Invoke(instance, new object[]
            {
                writerObserver
            });
            var tmp = type.GetMethod("get_Reader").Invoke(instance, new object[] 
            {
            });
            (tmp as DefaultStateReader).LoadFromFileStateOfAlgorithm(_hostingEnvironment.ContentRootPath);
            var xBest = type.GetProperty("XBest")?.GetValue(instance);
            var fBest = type.GetProperty("FBest")?.GetValue(instance);
            return Ok(new { xBest, fBest });
        }

        [Route("/home/runs")]
        [HttpPost]
        public async Task<IActionResult> RunM([FromForm] List<string> algNames, [FromForm] List<string> funNames)
        {
            object?[] tmp = new object[algNames.Count * funNames.Count];
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