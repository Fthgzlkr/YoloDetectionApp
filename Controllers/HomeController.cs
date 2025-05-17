using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using YoloDetectionApp.Models;
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
            // İlk açılışta boş ViewModel ile sayfa gösterilir
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
            // 🔐 Güvenli dosya adı oluştur
            string extension = Path.GetExtension(uploadedFile.FileName);
            string safeFileName = Guid.NewGuid().ToString() + extension;

            string inputFilePath = Path.Combine(uploadsFolder, safeFileName);
            string outputFilePath = Path.Combine(outputsFolder, safeFileName);

            // Girdiyi yükle
            using (var stream = new FileStream(inputFilePath, FileMode.Create))
            {
                uploadedFile.CopyTo(stream);
            }

            // Ham resmin web yolu
            model.OriginalImages.Add("/uploads/" + safeFileName);

            // Python betiğini çalıştır
            string pythonExe = "python";
            string pythonScript = Path.Combine(_webHostEnvironment.WebRootPath, "yolo_script.py");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{pythonScript}\" \"{inputFilePath}\"", // inputFilePath → script girişi
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

            // ✅ Retry mekanizması
            int retry = 5;
            bool fileExists = false;

            while (retry-- > 0)
            {
                if (System.IO.File.Exists(outputFilePath))
                {
                    fileExists = true;
                    break;
                }

                Thread.Sleep(200); // 200ms bekle
            }

            if (fileExists)
            {
                string relativePath = "/uploads/outputs/" + safeFileName;
                model.ProcessedImages.Add(relativePath);
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
