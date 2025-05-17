using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using YoloDetectionApp.Models;
using System.Text.Json;

namespace YoloDetectionApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new UploadResultViewModel());
        }

        [HttpPost]
        public IActionResult UploadImages(IEnumerable<IFormFile> uploadedFiles)
        {
            var model = new UploadResultViewModel();

            if (uploadedFiles != null && uploadedFiles.Any())
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                string outputsFolder = Path.Combine(uploadsFolder, "outputs");
                Directory.CreateDirectory(uploadsFolder);
                Directory.CreateDirectory(outputsFolder);

                foreach (var uploadedFile in uploadedFiles)
                {
                    string extension = Path.GetExtension(uploadedFile.FileName);
                    string safeFileName = Guid.NewGuid().ToString() + extension;

                    string inputFilePath = Path.Combine(uploadsFolder, safeFileName);
                    string outputFilePath = Path.Combine(outputsFolder, safeFileName);

                    using (var stream = new FileStream(inputFilePath, FileMode.Create))
                    {
                        uploadedFile.CopyTo(stream);
                    }

                    model.OriginalImages.Add("/uploads/" + safeFileName);

                    string pythonExe = "python";
                    string pythonScript = Path.Combine(_webHostEnvironment.WebRootPath, "yolo_script.py");

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = $"\"{pythonScript}\" \"{inputFilePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process process = new Process { StartInfo = psi };
                    process.Start();

                    string errors = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(errors))
                        Console.WriteLine("🟥 Python Hatası: " + errors);

                    int retry = 5;
                    bool fileExists = false;

                    while (retry-- > 0)
                    {
                        if (System.IO.File.Exists(outputFilePath))
                        {
                            fileExists = true;
                            break;
                        }
                        Thread.Sleep(200);
                    }

                    if (fileExists)
                    {
                        string relativePath = "/uploads/outputs/" + safeFileName;
                        model.ProcessedImages.Add(relativePath);

                        // 🔍 JSON sınıf sayımı oku
                        string jsonPath = Path.Combine(outputsFolder, Path.GetFileNameWithoutExtension(safeFileName) + "_classes.json");

                        if (System.IO.File.Exists(jsonPath))
                        {
                            string jsonContent = System.IO.File.ReadAllText(jsonPath);
                            var classDict = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonContent);

                            var classNames = new Dictionary<int, string>
                            {
                                { 0, "plane" },
                                { 1, "ship" },
                                { 2, "large-vehicle" },
                                { 3, "small-vehicle" }
                            };

                            var classColors = new Dictionary<int, string>
                            {
                                { 0, "#007bff" }, // Mavi
                                { 1, "#dc3545" }, // Kırmızı
                                { 2, "#28a745" }, // Yeşil
                                { 3, "#ffc107" }  // Sarı
                            };

                            var summary = classDict.Select(kv => new ClassSummaryItem
                            {
                                ClassName = classNames.TryGetValue(int.Parse(kv.Key), out var name) ? name : $"class {kv.Key}",
                                Count = kv.Value,
                                ColorHex = classColors.TryGetValue(int.Parse(kv.Key), out var color) ? color : "#6c757d"
                            }).ToList();

                            model.ClassSummaries[relativePath] = summary;
                        }

                        Console.WriteLine("✅ Çıktı bulundu ve eklendi: " + relativePath);
                    }
                    else
                    {
                        Console.WriteLine("❌ Çıktı bulunamadı: " + outputFilePath);
                    }
                }
            }

            return View("Index", model);
        }
    }
}
