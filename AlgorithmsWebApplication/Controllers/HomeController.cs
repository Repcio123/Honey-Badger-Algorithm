using HoneyBagder.MiscInterfaces;
using HoneyBagder.StateReader;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Runtime.Loader;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas.Draw;
using System.Xml.Linq;
using System.Diagnostics.Metrics;
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

        public class AlgorithmResultDTO
        {
            public double[] xBestMax { get; set; }
            public double fBestMax { get; set; }
            public string algName { get; set; }
            public string funName { get; set; }
            public int fitnessFunctionCalls { get; set; }
            public int Counter { get; set; }
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

        public void raport(AlgorithmResultDTO[] algorithmsResultsDto)
        {
            string reportsFolder = Path.Combine(_hostingEnvironment.ContentRootPath, "raports");
            Directory.CreateDirectory(reportsFolder);

            // Create a unique PDF file name based on the current date and time
            string pdfFileName = $"raport_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm")}.pdf";

            // Combine the folder and file paths
            string pdfFilePath = Path.Combine(reportsFolder, pdfFileName);

            using (var pdfWriter = new PdfWriter(pdfFilePath))
            {
                using (var pdfDocument = new PdfDocument(pdfWriter))
                {
                    var document = new iText.Layout.Document(pdfDocument);

                    // Dodaj nag��wek
                    document.Add(new Paragraph("Optimization Results")
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(20));

                    foreach (var algorithmResultDto in algorithmsResultsDto)
                    {
                        // Dodaj informacje o algorytmie i funkcji
                        document.Add(new Paragraph($"Algorithm: {algorithmResultDto.algName}, Function: {algorithmResultDto.funName}")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(12)
                            .SetFontColor(DeviceGray.BLACK));

                        // Dodaj dat� i godzin�
                        var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                        var currentTime = DateTime.Now.ToString("HH:mm:ss");
                        document.Add(new Paragraph($"Date: {currentDate}, Time: {currentTime}")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(14));

                        // Dodaj sekcj� z wynikami
                        document.Add(new Paragraph("Results:")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(16));

                        document.Add(new Paragraph($"Function Calls: {algorithmResultDto.fitnessFunctionCalls}")
                            .SetBold().SetFontSize(14).SetMarginBottom(10));

                        string xBestMaxString = string.Join(", ", algorithmResultDto.xBestMax);

                        // Dodaj informacje o XBest i FBest
                        document.Add(new Paragraph($"XBest: {xBestMaxString}")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(14));

                        document.Add(new Paragraph($"FBest: {algorithmResultDto.fBestMax}")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(14));

                        // Dodaj linie oddzielaj�ce
                        document.Add(new LineSeparator(new SolidLine())
                            .SetMarginTop(10)
                            .SetMarginBottom(10));
                    }
                    

                    // Dodaj stopk�
                    document.Add(new Paragraph("Generated by Honey Team Optimalization Algorithm")
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetFontSize(10)
                        .SetFontColor(DeviceGray.GRAY));

                    // Zako�cz dokument
                    document.Close();
                }
            }
        } 

        public async Task<AlgorithmResultDTO> Run(string algName, string funName, Dictionary<string, Dictionary<string, double>> parameters)
        {
            string writerPath = _hostingEnvironment.ContentRootPath + "/state/" + algName + funName;
            string dll = Path.Combine(_hostingEnvironment.ContentRootPath, "algorithms", algName);

            var alc = new AssemblyLoadContext("g", true);
            Assembly assembly = alc.LoadFromAssemblyPath(dll);

            Type? type = assembly.GetExportedTypes().FirstOrDefault(Type => Type.Name == "OptimizationAlgorithm");
            Type? writerObserverType = assembly.GetExportedTypes().FirstOrDefault(Type => Type.Name == "WriterObserver");
            object writerObserver = Activator.CreateInstance(writerObserverType, new object[] { writerPath });

            string funDll = Path.Combine(_hostingEnvironment.ContentRootPath, "functions", funName);

            var alc2 = new AssemblyLoadContext("g2", true);
            Assembly funAssembly = alc.LoadFromAssemblyPath(funDll);

            Type typeWithFitnessFunction = funAssembly.GetTypes().First(type => type.GetMethods().Any(m => m.Name == "fitnessFunction"));
            Type? delegateType = assembly.GetExportedTypes().FirstOrDefault(Type => Type.Name == "fitnessFunction");

            Tuple<double, double>[] testDomain = {
                Tuple.Create<double, double>(0.5, 1.5),
                Tuple.Create<double, double>(0.5, 1.5),
                Tuple.Create<double, double>(0.5, 1.5)
            };

            object? invokerInstance = Activator.CreateInstance(typeWithFitnessFunction);
            MethodInfo? invokerFunction = typeWithFitnessFunction.GetMethod("fitnessFunction");
            Delegate delgt = Delegate.CreateDelegate(delegateType, invokerInstance, invokerFunction);

            double[] parameterStartingValues = parameters.Select((parameter) => parameter.Value["lowerBound"]).ToArray();
            double[] parameterStepValues = parameters.Select((parameter) => parameter.Value["step"]).ToArray();
            double[] parameterMaxValues = parameters.Select((parameter) => parameter.Value["upperBound"]).ToArray();
            object? xBestMax = null;
            double fBestMax = double.PositiveInfinity;
            int counter =  1;
            double[] bestParameterValues = new double[parameterStartingValues.Length];

            foreach (var parameterValues in incrementParameters(parameterStartingValues, parameterStepValues, parameterMaxValues))
            {
                object? instance = Activator.CreateInstance(type);
                //type.GetMethod("Attach").Invoke(instance, new object[] { writerObserver });
                type.GetMethod("Solve")?.Invoke(instance, new object[] { delgt, testDomain, parameterValues });
                var xBest = type.GetProperty("XBest")?.GetValue(instance);
                double fBest = Convert.ToDouble(type.GetProperty("FBest")?.GetValue(instance));
                counter = (int)(type.GetProperty("NumberOfEvaluationFitnessFunction")?.GetValue(instance));

                if (Math.Abs(fBestMax) > Math.Abs(fBest))
                {
                    xBestMax = xBest;
                    fBestMax = fBest;
                    Array.Copy(parameterValues, bestParameterValues, parameterValues.Length);
                }
               // type.GetMethod("Detach").Invoke(instance, new object[] { writerObserver });
            }

            var result = new AlgorithmResultDTO
            {
                algName = algName,
                fBestMax = fBestMax,
                fitnessFunctionCalls = counter,
                funName = funName,
                xBestMax = xBestMax as double[]
            };


            alc2.Unload();
            alc.Unload();
            return result;
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

            AlgorithmResultDTO[] results = new AlgorithmResultDTO[1];
            var result = await Run(algName, funName, parameters);
            results[0] = result;
            raport(results);
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

            List<Task<AlgorithmResultDTO>> results = new List<Task<AlgorithmResultDTO>> { };
            int i = 0;
            foreach (string alg in algNames) 
            {
                foreach (string fun in funNames)
                {
                    results.Add(Run(alg, fun, algorithmsParameters[alg]));
                    i++;
                }
            }

            var res = Task.WhenAll(results).Result;
            raport(res);
            return Ok(res);
        }
    }
}