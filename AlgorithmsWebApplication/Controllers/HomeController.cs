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

        public async Task<object> Run(string algName, string funName, Dictionary<string, Dictionary<string, double>> parameters)
        {
            WriterObserver writerObserver = new WriterObserver(_hostingEnvironment.ContentRootPath);
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

            double[] parameterStartingValues = parameters.Select((parameter) => parameter.Value["lowerBound"]).ToArray();
            double[] parameterStepValues = parameters.Select((parameter) => parameter.Value["step"]).ToArray();
            double[] parameterMaxValues = parameters.Select((parameter) => parameter.Value["upperBound"]).ToArray();

            object? xBestMax = null;
            double fBestMax = double.PositiveInfinity;
            double[] bestParameterValues = new double[parameterStartingValues.Length];
            foreach (var parameterValues in incrementParameters(parameterStartingValues, parameterStepValues, parameterMaxValues))
            {
                object? instance = Activator.CreateInstance(type);
                type.GetMethod("Attach").Invoke(instance, new object[] { writerObserver });
                type.GetMethod("Solve")?.Invoke(instance, new object[] { delgt, testDomain, parameterValues });
                var xBest = type.GetProperty("XBest")?.GetValue(instance);
                double fBest = Convert.ToDouble(type.GetProperty("FBest")?.GetValue(instance));

                if (Math.Abs(fBestMax) > Math.Abs(fBest))
                {
                    xBestMax = xBest;
                    fBestMax = fBest;
                    Array.Copy(parameterValues, bestParameterValues, parameterValues.Length);
                }
                type.GetMethod("Detach").Invoke(instance, new object[] { writerObserver });
                var tmp = type.GetMethod("get_Reader").Invoke(instance, new object[] { });
                (tmp as DefaultStateReader).LoadFromFileStateOfAlgorithm(_hostingEnvironment.ContentRootPath);
            }
            return new { xBestMax, fBestMax, bestParameterValues };
        }

        [Route("/home/run")]
        [HttpPost]
        public async Task<IActionResult> RunSingle([FromForm] string algName, [FromForm] string funName)
        {
            Dictionary<string, Dictionary<string, double>> parameters = new Dictionary<string, Dictionary<string, double>> { };
            foreach (string key in Request.Form.Keys)
            {
                string[] keyIndices = key.Split('-');
                if (keyIndices.Length == 2)
                {
                    string parameterName = keyIndices[0];
                    string parameterPropertyName = keyIndices[1];
                    if (!parameters.ContainsKey(parameterName))
                    {
                        parameters.Add(parameterName, new Dictionary<string, double> { });
                    }
                    string parameterValueString = Request.Form[key];
                    parameters[parameterName].Add(parameterPropertyName, Convert.ToDouble(parameterValueString.Replace('.', ',')));
                }
            }

            var result = Run(algName, funName, parameters);
            return Ok(result);
        }

        [Route("/home/runs")]
        [HttpPost]
        public async Task<IActionResult> RunMultiple([FromForm] List<string> algNames, [FromForm] List<string> funNames)
        {
            // co za rak
            Dictionary<string, Dictionary<string, Dictionary<string, double>>> algorithmsParameters = new Dictionary<string, Dictionary<string, Dictionary<string, double>>> { };
            foreach (string key in Request.Form.Keys)
            {
                string[] keyIndices = key.Split('-');
                if (keyIndices.Length == 3)
                {
                    string algName = keyIndices[0];

                    if (!algorithmsParameters.ContainsKey(algName))
                    {
                        algorithmsParameters.Add(algName, new Dictionary<string, Dictionary<string, double>> { });
                    }

                    string parameterName = keyIndices[1];
                    string parameterPropertyName = keyIndices[2];

                    if (!algorithmsParameters[algName].ContainsKey(parameterName))
                    {
                        algorithmsParameters[algName].Add(parameterName, new Dictionary<string, double> { });
                    }
                    string parameterValueString = Request.Form[key];
                    algorithmsParameters[algName][parameterName].Add(parameterPropertyName, Convert.ToDouble(parameterValueString.Replace('.', ',')));
                }
            }

            object?[] results = new object[algNames.Count * funNames.Count];
            int i = 0;
            foreach (string alg in algNames) 
            {
                foreach (string fun in funNames)
                {
                    results[i] = await Run(alg, fun, algorithmsParameters[alg]);
                    i++;
                }
            }
            return Ok(new { results });
        }
    }
}