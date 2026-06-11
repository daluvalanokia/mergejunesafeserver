using EyewaysMergeSafeServer.Data;
using EyewaysMergeSafeServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EyewaysMergeSafeServer.Controllers;

[Route("api")]
[ApiController]
public class ApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public ApiController(AppDbContext db) { _db = db; }

    // ── Stats ──────────────────────────────────────────────────────────────

    [HttpGet("stats"), OutputCache(PolicyName = "ShortLive")]
    public async Task<IActionResult> Stats(string? highwayId)
    {
        var zones   = await _db.MergeZones.AsNoTracking().Where(z => highwayId == null || z.HighwayId == highwayId).CountAsync();
        var servers = await _db.SwitchServers.AsNoTracking().Where(s => highwayId == null || s.HighwayId == highwayId).CountAsync();
        var sensors = await _db.SensorDevices.AsNoTracking().Where(d => highwayId == null || d.HighwayId == highwayId).CountAsync();
        var events  = await _db.VehicleEvents.AsNoTracking().Where(e => highwayId == null || e.HighwayId == highwayId).CountAsync();
        return Ok(new { zones, servers, sensors, events, timestamp = DateTime.UtcNow });
    }

    [HttpGet("zone/{zoneId}/status")]
    public async Task<IActionResult> ZoneStatus(string zoneId)
    {
        var zone = await _db.MergeZones.AsNoTracking().FirstOrDefaultAsync(z => z.ZoneId == zoneId);
        if (zone == null) return NotFound();
        var sensors = await _db.SensorDevices.AsNoTracking().Where(d => d.ZoneId == zoneId).ToListAsync();
        var servers = await _db.SwitchServers.AsNoTracking().Where(s => s.ZoneId == zoneId).ToListAsync();
        return Ok(new {
            zoneId,
            zoneName = zone.ZoneName,
            status   = zone.Status,
            sensors  = sensors.Select(s => new { s.DeviceId, s.DeviceName, s.Status }),
            servers  = servers.Select(s => new { s.ServerId, s.ServerName, s.Status })
        });
    }

    // ── Live event feed (ALL event types, delta-poll by since cursor) ──────
    [HttpGet("events/live")]
    public async Task<IActionResult> LiveEvents(string? highwayId, string? zoneId, string? since)
    {
        var cutoff = ParseSince(since, -30);
        var q = _db.VehicleEvents.AsNoTracking()
            .Where(e => e.CreatedDate > cutoff);
        if (!string.IsNullOrEmpty(highwayId)) q = q.Where(e => e.HighwayId == highwayId);
        // Zone filter: match either null/empty OR the specific zone
        if (!string.IsNullOrEmpty(zoneId)) q = q.Where(e => e.ZoneId == zoneId);
        var events = await q.OrderByDescending(e => e.CreatedDate).Take(50).ToListAsync();
        return Ok(new { events = events.Select(ProjectEvent), serverTime = DateTime.UtcNow });
    }

    // ── Simulation-only delta feed (for 3D scene live top-up while sim runs) ─
    // Uses IsSimulated=true (more reliable than VehicleId prefix)
    [HttpGet("events/simulation")]
    public async Task<IActionResult> SimulationEvents(string? highwayId, string? zoneId, string? since)
    {
        var cutoff = ParseSince(since, -30);
        var q = _db.VehicleEvents.AsNoTracking()
            .Where(e => e.IsSimulated && e.CreatedDate > cutoff);
        if (!string.IsNullOrEmpty(highwayId)) q = q.Where(e => e.HighwayId == highwayId);
        if (!string.IsNullOrEmpty(zoneId))    q = q.Where(e => e.ZoneId == zoneId);
        var events = await q.OrderByDescending(e => e.CreatedDate).Take(50).ToListAsync();
        return Ok(new { events = events.Select(ProjectEvent), serverTime = DateTime.UtcNow });
    }

    // ── Scene snapshot: ALL persisted simulated records for initial 3D load ─
    // Returns newest-first up to limit; client advances cursor from max(createdDate)
    [HttpGet("events/scene")]
    public async Task<IActionResult> SceneEvents(string? highwayId, string? zoneId, int limit = 200)
    {
        limit = Math.Clamp(limit, 1, 500);
        var q = _db.VehicleEvents.AsNoTracking()
            .Where(e => e.IsSimulated);
        if (!string.IsNullOrEmpty(highwayId)) q = q.Where(e => e.HighwayId == highwayId);
        if (!string.IsNullOrEmpty(zoneId))    q = q.Where(e => e.ZoneId == zoneId);
        var events = await q.OrderByDescending(e => e.CreatedDate).Take(limit).ToListAsync();
        // serverTime is the timestamp of the NEWEST event (for the client's poll cursor)
        var cursor = events.Count > 0 ? events[0].CreatedDate : DateTime.UtcNow;
        return Ok(new {
            events     = events.Select(ProjectEvent).ToList(),
            count      = events.Count,
            cursor     = cursor,          // client should use this as "since" for subsequent delta-polls
            serverTime = DateTime.UtcNow
        });
    }

    // ── All events for all types (live + sim combined, no filter) ──────────
    [HttpGet("events/all")]
    public async Task<IActionResult> AllEvents(string? highwayId, string? zoneId, string? since)
    {
        var cutoff = ParseSince(since, -60);
        var q = _db.VehicleEvents.AsNoTracking()
            .Where(e => e.CreatedDate > cutoff);
        if (!string.IsNullOrEmpty(highwayId)) q = q.Where(e => e.HighwayId == highwayId);
        if (!string.IsNullOrEmpty(zoneId))    q = q.Where(e => e.ZoneId == zoneId);
        var events = await q.OrderByDescending(e => e.CreatedDate).Take(100).ToListAsync();
        return Ok(new { events = events.Select(ProjectEvent), serverTime = DateTime.UtcNow });
    }

    // ── Ingest ─────────────────────────────────────────────────────────────
    [HttpPost("events/ingest")]
    public async Task<IActionResult> IngestEvent([FromBody] VehicleEvent ev)
    {
        ev.CreatedDate = DateTime.UtcNow;
        // Normalize empty strings → null for consistent filtering
        if (string.IsNullOrWhiteSpace(ev.ZoneId))    ev.ZoneId    = null;
        if (string.IsNullOrWhiteSpace(ev.HighwayId)) ev.HighwayId = "";
        _db.VehicleEvents.Add(ev);
        await _db.SaveChangesAsync();
        return Ok(new { id = ev.Id, status = "accepted", timestamp = ev.CreatedDate });
    }

    [HttpPost("sensor/{deviceId}/heartbeat")]
    public async Task<IActionResult> SensorHeartbeat(string deviceId)
    {
        var sensor = await _db.SensorDevices.FirstOrDefaultAsync(d => d.DeviceId == deviceId);
        if (sensor == null) return NotFound(new { deviceId, error = "device not found" });
        sensor.LastHeartbeat = DateTime.UtcNow;
        sensor.Status = "online";
        await _db.SaveChangesAsync();
        return Ok(new { deviceId, status = "online", timestamp = DateTime.UtcNow });
    }

    [HttpPost("server/{serverId}/heartbeat")]
    public async Task<IActionResult> ServerHeartbeat(string serverId)
    {
        var server = await _db.SwitchServers.FirstOrDefaultAsync(s => s.ServerId == serverId);
        if (server == null) return NotFound(new { serverId, error = "server not found" });
        server.LastHeartbeat = DateTime.UtcNow;
        server.Status = "online";
        await _db.SaveChangesAsync();
        return Ok(new { serverId, status = "online", timestamp = DateTime.UtcNow });
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static DateTime ParseSince(string? since, int defaultOffsetSeconds)
    {
        if (!string.IsNullOrEmpty(since) &&
            DateTime.TryParse(since, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return dt;
        return DateTime.UtcNow.AddSeconds(defaultOffsetSeconds);
    }

    private static object ProjectEvent(VehicleEvent e) => new
    {
        e.Id,
        e.EventType,
        e.VehicleId,
        e.SpeedMph,
        e.ZoneId,
        e.HighwayId,
        e.Direction,
        e.IsSimulated,
        e.Latitude,
        e.Longitude,
        vehicleType  = ParsePayload(e.Payload, "vehicle_type"),
        vehicleColor = ParsePayload(e.Payload, "color"),
        vehicleMake  = ParsePayload(e.Payload, "make"),
        vehicleModel = ParsePayload(e.Payload, "model"),
        lane         = ParsePayload(e.Payload, "lane"),
        e.CreatedDate
    };

    private static string? ParsePayload(string? payload, string field)
    {
        if (string.IsNullOrEmpty(payload)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            return doc.RootElement.TryGetProperty(field, out var v) ? v.GetString() : null;
        }
        catch { return null; }
    }
}
