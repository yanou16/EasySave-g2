var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/logs", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    Console.WriteLine("📥 Log received:");
    Console.WriteLine(body);
    Console.WriteLine("-----------------------------------");

    return Results.Ok(new { status = "received" });
});

app.Run();