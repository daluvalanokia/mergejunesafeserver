using EyewaysMergeSafeServer.Data;
using EyewaysMergeSafeServer.Models;
using EyewaysMergeSafeServer.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EyewaysMergeSafeServer.Controllers;

public class SettingsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly IWebHostEnvironment _env;

    public SettingsController(AppDbContext db, IConfiguration cfg, IWebHostEnvironment env)
    { _db = db; _cfg = cfg; _env = env; }

    public async Task<IActionResult> Index()
    {
        return View(new SettingsViewModel
        {
            Highways = await _db.Highways.AsNoTracking().OrderBy(h => h.Name).ToListAsync(),
            TomTomApiKey = _cfg["TomTomApiKey"] ?? ""
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Highway model)
    {
        if (ModelState.IsValid)
        {
            model.CreatedDate = DateTime.UtcNow;
            _db.Highways.Add(model);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Highway model)
    {
        if (ModelState.IsValid)
        {
            _db.Highways.Update(model);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var h = await _db.Highways.FindAsync(id);
        if (h != null) { _db.Highways.Remove(h); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveTomTomKey(string apiKey)
    {
        var keyFilePath = Path.Combine(AppContext.BaseDirectory, "tomtomkey.json");
        var payload = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["TomTomApiKey"] = apiKey?.Trim() ?? ""
        }, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(keyFilePath, payload);

        var reloadable = _cfg as IConfigurationRoot;
        reloadable?.Reload();

        TempData["Success"] = "TomTom API key saved successfully.";
        return RedirectToAction(nameof(Index));
    }
}
