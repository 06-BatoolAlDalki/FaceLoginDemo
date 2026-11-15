using FaceLoginDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;

namespace FaceLoginDemo.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string apiKey = "VyOnm-tmFOMbu6b5viywcU8qCyyb5F8s";
        private readonly string apiSecret = "M05MxA51T5HoS29h3qWxRaDyGsrdFFgy";

        public AccountController(MyDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }
        public IActionResult Index()
        {
            return View();
        }
        //private string CleanBase64String(string base64)
        //{
        //    if (string.IsNullOrEmpty(base64)) return string.Empty;
        //    int commaIndex = base64.IndexOf(',');
        //    if (commaIndex > 0)
        //    {
        //        return base64.Substring(commaIndex + 1);
        //    }
        //    return base64;
        //}

        [HttpPost]
        public async Task<IActionResult> LoginByFace([FromBody] FaceLoginModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FullName == model.Username);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            string currentImageBase64 = model.Image.Replace("data:image/jpeg;base64,", "").Replace("data:image/png;base64,", "");
            string storedImageBase64 = user.ProfileImage.Replace("data:image/jpeg;base64,", "").Replace("data:image/png;base64,", "");

            var client = _httpClientFactory.CreateClient();

            var content = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("api_key", apiKey),
        new KeyValuePair<string, string>("api_secret", apiSecret),
        new KeyValuePair<string, string>("image_base64_1", currentImageBase64),
        new KeyValuePair<string, string>("image_base64_2", storedImageBase64)
    });

            var response = await client.PostAsync("https://api-us.faceplusplus.com/facepp/v3/compare", content);
            var result = await response.Content.ReadAsStringAsync();

            try
            {
                using var jsonDocument = JsonDocument.Parse(result);
                var root = jsonDocument.RootElement;

                if (root.TryGetProperty("error_message", out var errorProperty))
                {
                    var errorMessage = errorProperty.GetString();
                    return Json(new { success = false, message = $"API Error: {errorMessage}" });
                }

                if (root.TryGetProperty("confidence", out var confidenceProperty))
                {
                    var confidence = confidenceProperty.GetDouble();

                    if (confidence > 70)
                    {
                        return Json(new { success = true, message = "Login successful ✅", confidence });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Face not recognized ❌", confidence });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Failed to get match score. Please ensure both images contain clear faces." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred while processing the request: {ex.Message}" });
            }
        }



        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        public IActionResult LoginByFace()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register([FromBody] RegisterModel model)
        {
            var user = new User
            {
                FullName = model.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                ProfileImage = model.Image,
                Email = model.Password
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Json(new { success = true, message = "User registered successfully." });
        }
        public class RegisterModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Image { get; set; }
        }


        public class FaceLoginModel
        {
            public string Username { get; set; }
            public string Image { get; set; }
        }

    }
}
