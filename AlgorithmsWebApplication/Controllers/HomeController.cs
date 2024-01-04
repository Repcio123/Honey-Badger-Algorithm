using HoneyBagder.MiscInterfaces;
using HoneyBagder.StateReader;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        IEnumerable<double[]> incrementParameters(double[] startValues, double[] steps, double[] maxes)
        {
            double[] currentValues = new double[startValues.Length];
            Array.Copy(startValues, currentValues, startValues.Length);

            yield return currentValues;

            while (true)
            {
                // Find first lower than max from the left
                int i = 0;
                for (; i < currentValues.Length; i++)
                {
                    if (currentValues[i] < maxes[i])
                    {
                        break;
                    }
                }

                // If all parameters higher than max, break
                if (i == currentValues.Length)
                {
                    break;
                }

                // else zero all the previous parameters
                for (int j = i - 1; j >= 0; j--)
                {
                    currentValues[j] = startValues[j];
                }

                // and increment the current one
                currentValues[i] += steps[i];

                // yield current parameters state
                yield return currentValues;
            }
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

            Dictionary<string, Dictionary<string, double>> passedInitialParameters = new Dictionary<string, Dictionary<string, double>> { };
            foreach (string key in Request.Form.Keys)
            {
                string[] keyIndices = key.Split('-');
                if (keyIndices.Length == 2)
                {
                    string parameterName = keyIndices[0];
                    string parameterPropertyName = keyIndices[1];
                    if (!passedInitialParameters.ContainsKey(parameterName))
                    {
                        passedInitialParameters.Add(parameterName, new Dictionary<string, double> { });
                    }
                    string parameterValueString = Request.Form[key];
                    passedInitialParameters[parameterName].Add(parameterPropertyName, Convert.ToDouble(parameterValueString.Replace('.', ',')));
                }
            }

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

            double[] parameterStartingValues = passedInitialParameters.Select((parameter) => parameter.Value["lowerBound"]).ToArray();
            double[] parameterStepSisters= passedInitialParameters.Select((parameter) => parameter.Value["step"]).ToArray();
            double[] parameterMaxValues = passedInitialParameters.Select((parameter) => parameter.Value["upperBound"]).ToArray();

            object? xBestMax = null;
            double fBestMax = double.PositiveInfinity;
            double[] bestParameterValues = new double[parameterStartingValues.Length];
            foreach (var parameterValues in incrementParameters(parameterStartingValues, parameterStepSisters, parameterMaxValues)) {
                object? instance = Activator.CreateInstance(type);
                type.GetMethod("Attach").Invoke(instance, new object[] { writerObserver });
                type.GetMethod("Solve")?.Invoke(instance, new object[]{ delgt, testDomain, parameterValues });
                var xBest = type.GetProperty("XBest")?.GetValue(instance);
                double fBest = Convert.ToDouble(type.GetProperty("FBest")?.GetValue(instance));

                if (Math.Abs(fBestMax) > Math.Abs(fBest))
                {
                    xBestMax = xBest;
                    fBestMax = fBest;
                    Array.Copy(parameterValues, bestParameterValues, parameterValues.Length);
                }
                type.GetMethod("Detach").Invoke(instance, new object[] { writerObserver });
                var tmp = type.GetMethod("get_Reader").Invoke(instance, new object[] {});
                (tmp as DefaultStateReader).LoadFromFileStateOfAlgorithm(_hostingEnvironment.ContentRootPath);
            }
            return Ok(new { xBestMax, fBestMax, bestParameterValues });
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