using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;

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
            return View();
        }

      [HttpPost]
public IActionResult UploadImages(IEnumerable<IFormFile> uploadedFiles)
{
    if (uploadedFiles != null && uploadedFiles.Any())
    {
        var processedImages = new List<string>();
        var originalImages = new List<string>();

        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
        string outputsFolder = Path.Combine(uploadsFolder, "outputs");
        Directory.CreateDirectory(uploadsFolder);
        Directory.CreateDirectory(outputsFolder);

        foreach (var uploadedFile in uploadedFiles)
        {
            string filePath = Path.Combine(uploadsFolder, uploadedFile.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                uploadedFile.CopyTo(stream);
            }

            // Python scripti
            string pythonExe = "python";
            string pythonScript = Path.Combine(_webHostEnvironment.WebRootPath, "yolo_script.py");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{pythonScript}\" \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = psi };
            process.Start();
            process.WaitForExit();

            string outputFilePath = Path.Combine(outputsFolder, uploadedFile.FileName);

            // ⚠️ Retry ekledik
            int retry = 5;
            while (!System.IO.File.Exists(outputFilePath) && retry-- > 0)
                Thread.Sleep(200);

            if (System.IO.File.Exists(outputFilePath))
            {
                string relativePath = "/uploads/outputs/" + uploadedFile.FileName;
                processedImages.Add(relativePath);
            }

            originalImages.Add("/uploads/" + uploadedFile.FileName);
        }

        ViewBag.OriginalImages = originalImages;
        ViewBag.ProcessedImages = processedImages;
    }

    return View("Index");
}



    }
}
