using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Central log directory inside the container (mapped to a volume in production)
string logsDir = Path.Combine(AppContext.BaseDirectory, "CentralLogs");
Directory.CreateDirectory(logsDir);

// Thread-safety: one write at a time to the shared daily log file
var fileLock = new object();

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

// POST /logs  — receives one log entry from any EasySave instance
app.MapPost("/logs", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    string body = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(body))
        return Results.BadRequest(new { error = "empty body" });

    // ── Machine identifier sent by EasySave via header ──────────────────────
    string machine = context.Request.Headers["X-Machine-Name"].FirstOrDefault()
                     ?? "unknown";

    // ── Deserialise the entry and inject MachineName ─────────────────────────
    Dictionary<string, JsonElement>? entry;
    try
    {
        entry = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
    }
    catch
    {
        return Results.BadRequest(new { error = "invalid JSON" });
    }

    if (entry is null)
        return Results.BadRequest(new { error = "null entry" });

    // Build enriched entry: original fields + MachineName
    var enriched = new Dictionary<string, object?>
    {
        ["MachineName"]      = machine,
        ["Timestamp"]        = entry.TryGetValue("Timestamp",        out var ts)  ? ts.GetString()  : null,
        ["BackupName"]       = entry.TryGetValue("BackupName",       out var bn)  ? bn.GetString()  : null,
        ["SourcePath"]       = entry.TryGetValue("SourcePath",       out var sp)  ? sp.GetString()  : null,
        ["TargetPath"]       = entry.TryGetValue("TargetPath",       out var tp)  ? tp.GetString()  : null,
        ["FileSize"]         = entry.TryGetValue("FileSize",         out var fs)  ? fs.GetInt64()   : 0L,
        ["TransferTimeMs"]   = entry.TryGetValue("TransferTimeMs",   out var ttm) ? ttm.GetInt64()  : 0L,
        ["EncryptionTimeMs"] = entry.TryGetValue("EncryptionTimeMs", out var etm) ? etm.GetInt64()  : 0L,
    };

    // ── Append to the single daily log file ──────────────────────────────────
    string logFile = Path.Combine(logsDir, $"{DateTime.UtcNow:yyyy-MM-dd}.json");

    lock (fileLock)
    {
        List<object> existing = new();

        if (File.Exists(logFile))
        {
            try
            {
                existing = JsonSerializer.Deserialize<List<object>>(File.ReadAllText(logFile))
                           ?? new List<object>();
            }
            catch { existing = new List<object>(); }
        }

        existing.Add(enriched);
        File.WriteAllText(logFile, JsonSerializer.Serialize(existing, jsonOptions));
    }

    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] [{machine}] Log saved → {Path.GetFileName(logFile)}");

    return Results.Ok(new { status = "received", machine, logFile = Path.GetFileName(logFile) });
});

// GET /logs/today  — read today's centralised log (useful for demo / monitoring)
app.MapGet("/logs/today", () =>
{
    string logFile = Path.Combine(logsDir, $"{DateTime.UtcNow:yyyy-MM-dd}.json");

    if (!File.Exists(logFile))
        return Results.NotFound(new { message = "No log entries today yet." });

    string content = File.ReadAllText(logFile);
    return Results.Content(content, "application/json");
});

// GET /logs/files  — list all daily log files available
app.MapGet("/logs/files", () =>
{
    var files = Directory.GetFiles(logsDir, "*.json")
                         .Select(Path.GetFileName)
                         .OrderByDescending(f => f)
                         .ToList();
    return Results.Ok(files);
});

app.Run();
