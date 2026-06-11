using EyewaysMergeSafeServer.Data;
using EyewaysMergeSafeServer.Models;
using EyewaysMergeSafeServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewaysMergeSafeServer.Controllers;

public class DataInputFormatsController : Controller
{
    private readonly AppDbContext       _db;
    private readonly InputPayloadService _payloadSvc;
    private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

    public DataInputFormatsController(AppDbContext db, InputPayloadService payloadSvc)
    { _db = db; _payloadSvc = payloadSvc; }

    [HttpGet]
    public async Task<IActionResult> Index(string? activeTab)
    {
        var vm = new ViewModels.DataInputFormatsViewModel
        {
            ActiveTab        = activeTab ?? "physical",
            PhysicalConfigs  = await _db.InputFormatConfigs.AsNoTracking().Where(c => c.SourceType == "physical").ToListAsync(),
            SatelliteConfigs = await _db.InputFormatConfigs.AsNoTracking().Where(c => c.SourceType == "satellite").ToListAsync(),
            TelecomConfigs   = await _db.InputFormatConfigs.AsNoTracking().Where(c => c.SourceType == "telecom").ToListAsync(),
            TrackerConfigs   = await _db.InputFormatConfigs.AsNoTracking().Where(c => c.SourceType == "tracker").ToListAsync(),
            SavedPayloads    = await _db.SamplePayloads.AsNoTracking().OrderByDescending(p => p.CreatedDate).Take(20).ToListAsync(),
            Highways         = await _db.Highways.AsNoTracking().OrderBy(h => h.Name).ToListAsync(),
            AllZones         = await _db.MergeZones.AsNoTracking().OrderBy(z => z.ZoneName).ToListAsync(),
            AllSwitchServers = await _db.SwitchServers.AsNoTracking().OrderBy(s => s.ServerName).ToListAsync(),
        };
        return View(vm);
    }

    // ── Simulation post: generates a payload AND saves a VehicleEvent ──────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulationPost(
        string? highwayId, string? zoneId, string? serverId, string? sourceType)
    {
        var type   = sourceType ?? "physical";
        var fields = new[] { "vehicle_id","timestamp","speed_mph","latitude","longitude",
                             "direction","lane","vehicle_type","event_type","zone_id",
                             "highway_id","signal_strength","altitude_ft" };

        // Generate raw payload first
        var payload = _payloadSvc.Generate(type, fields);
        var now     = DateTime.UtcNow;

        // Parse generated values so we can override with UI selections
        string GetStr(System.Text.Json.JsonDocument doc, string k) =>
            doc.RootElement.TryGetProperty(k, out var v) ? (v.GetString() ?? "") : "";
        double? GetDbl(System.Text.Json.JsonDocument doc, string k) =>
            doc.RootElement.TryGetProperty(k, out var v) &&
            v.ValueKind == System.Text.Json.JsonValueKind.Number ? v.GetDouble() : null;

        using var doc = System.Text.Json.JsonDocument.Parse(payload);

        var resolvedHighway = !string.IsNullOrWhiteSpace(highwayId) ? highwayId : GetStr(doc, "highway_id");
        var resolvedZone    = !string.IsNullOrWhiteSpace(zoneId)    ? zoneId    : GetStr(doc, "zone_id");
        // Normalize: empty string → null so DB NULL filtering works correctly
        if (string.IsNullOrWhiteSpace(resolvedZone))    resolvedZone    = null;
        if (string.IsNullOrWhiteSpace(resolvedHighway)) resolvedHighway = null;

        var rawVehicleId = GetStr(doc, "vehicle_id");
        var simVehicleId = "SIM-" + (rawVehicleId.Length > 0 ? rawVehicleId : Guid.NewGuid().ToString("N")[..8]);

        var dirStr   = GetStr(doc, "direction");
        var cardinal = dirStr is "N" or "S" or "E" or "W" ? dirStr
                     : dirStr is { Length: > 0 } && int.TryParse(dirStr, out var deg)
                           ? (deg < 45 || deg >= 315 ? "N" : deg < 135 ? "E" : deg < 225 ? "S" : "W")
                           : null;

        var vehicleType = GetStr(doc, "vehicle_type");
        var eventType   = GetStr(doc, "event_type") is { Length: > 0 } et ? et : "detection";

        // Re-serialize payload with UI-overridden fields so stored payload matches the event
        var payloadObj = new Dictionary<string, object?>
        {
            ["vehicle_id"]      = rawVehicleId,
            ["sim_vehicle_id"]  = simVehicleId,
            ["timestamp"]       = now.ToString("o"),
            ["speed_mph"]       = GetDbl(doc, "speed_mph"),
            ["latitude"]        = GetDbl(doc, "latitude"),
            ["longitude"]       = GetDbl(doc, "longitude"),
            ["direction"]       = dirStr,
            ["lane"]            = doc.RootElement.TryGetProperty("lane", out var lv) ? lv.GetInt32() : 1,
            ["vehicle_type"]    = vehicleType,
            ["event_type"]      = eventType,
            ["zone_id"]         = resolvedZone,
            ["highway_id"]      = resolvedHighway,
            ["source_type"]     = type,
            ["is_simulated"]    = true,
        };
        if (doc.RootElement.TryGetProperty("signal_strength", out var ss)) payloadObj["signal_strength"] = ss.GetInt32();
        if (doc.RootElement.TryGetProperty("altitude_ft",     out var af)) payloadObj["altitude_ft"]     = af.GetInt32();

        var finalPayload = System.Text.Json.JsonSerializer.Serialize(payloadObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var shortPayload = finalPayload.Length > 490 ? finalPayload[..490] : finalPayload;

        var label = $"Simulation [{type.ToUpper()}] — {now:HH:mm:ss}";

        // Save SamplePayload record
        _db.SamplePayloads.Add(new SamplePayload
        {
            SourceType  = type,
            Label       = label,
            Payload     = shortPayload,
            IsValid     = true,
            CreatedDate = now
        });

        // Save VehicleEvent record (IsSimulated = true, fields all consistent)
        _db.VehicleEvents.Add(new VehicleEvent
        {
            EventType   = eventType,
            ZoneId      = resolvedZone,
            HighwayId   = resolvedHighway ?? "",
            VehicleId   = simVehicleId,
            SpeedMph    = GetDbl(doc, "speed_mph"),
            Latitude    = GetDbl(doc, "latitude"),
            Longitude   = GetDbl(doc, "longitude"),
            Direction   = cardinal,
            IsSimulated = true,
            Payload     = shortPayload,
            CreatedDate = now
        });

        await _db.SaveChangesAsync();

        return Json(new { ok = true, label, payload = shortPayload, vehicleId = simVehicleId, vehicleType, direction = cardinal, zoneId = resolvedZone, highwayId = resolvedHighway });
    }

    // ── Generate payload preview (no DB write) ──────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GeneratePayloadAjax(int configId)
    {
        var config = _db.InputFormatConfigs.Find(configId);
        if (config == null) return NotFound();
        var fields  = config.EnabledFieldsRaw?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var payload = _payloadSvc.Generate(config.SourceType, fields);
        return Json(new { ok = true, payload, label = $"{config.FormatName} — {DateTime.UtcNow:HH:mm:ss}" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePayload(int configId, string payload)
    {
        var config = await _db.InputFormatConfigs.FindAsync(configId);
        if (config == null) return NotFound();
        _db.SamplePayloads.Add(new SamplePayload
        {
            ConfigId    = configId,
            SourceType  = config.SourceType,
            Label       = $"{config.FormatName} — {DateTime.UtcNow:HH:mm:ss}",
            Payload     = payload,
            IsValid     = true,
            CreatedDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        if (IsAjax) return Json(new { ok = true });
        return RedirectToAction(nameof(Index), new { activeTab = config.SourceType });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InputFormatConfig model, string[] enabledFields, string[]? customFieldNames)
    {
        var combined = enabledFields.ToList();
        if (customFieldNames != null) combined.AddRange(customFieldNames.Where(n => !string.IsNullOrWhiteSpace(n)));
        model.EnabledFieldsRaw = string.Join(",", combined);
        _db.InputFormatConfigs.Add(model);
        await _db.SaveChangesAsync();
        if (IsAjax) return Json(new { ok = true, activeTab = model.SourceType });
        return RedirectToAction(nameof(Index), new { activeTab = model.SourceType });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InputFormatConfig model, string[] enabledFields, string[]? customFieldNames)
    {
        var combined = enabledFields.ToList();
        if (customFieldNames != null) combined.AddRange(customFieldNames.Where(n => !string.IsNullOrWhiteSpace(n)));
        model.EnabledFieldsRaw = string.Join(",", combined);
        _db.InputFormatConfigs.Update(model);
        await _db.SaveChangesAsync();
        if (IsAjax) return Json(new { ok = true, activeTab = model.SourceType });
        return RedirectToAction(nameof(Index), new { activeTab = model.SourceType });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.InputFormatConfigs.FindAsync(id);
        if (c != null) { _db.InputFormatConfigs.Remove(c); await _db.SaveChangesAsync(); }
        if (IsAjax) return Json(new { ok = true });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DuplicateConfig(int id)
    {
        var src = await _db.InputFormatConfigs.FindAsync(id);
        if (src == null) return NotFound();
        var copy = new InputFormatConfig
        {
            SourceType       = src.SourceType,
            FormatName       = src.FormatName + " (copy)",
            SourceId         = src.SourceId + "-copy",
            Description      = src.Description,
            InputSource      = src.InputSource,
            EnabledFieldsRaw = src.EnabledFieldsRaw
        };
        _db.InputFormatConfigs.Add(copy);
        await _db.SaveChangesAsync();
        if (IsAjax) return Json(new { ok = true, activeTab = copy.SourceType });
        return RedirectToAction(nameof(Index), new { activeTab = copy.SourceType });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePayload(int id)
    {
        var p = await _db.SamplePayloads.FindAsync(id);
        if (p != null) { _db.SamplePayloads.Remove(p); await _db.SaveChangesAsync(); }
        if (IsAjax) return Json(new { ok = true });
        return RedirectToAction(nameof(Index));
    }
}
