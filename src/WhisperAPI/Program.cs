var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello API!");

await app.RunAsync();


async Task<string> StreamMeAsync(HttpRequest req)
{
    Directory.CreateDirectory("audio");
    var wavFile = $"audio/{Guid.NewGuid():N}.wav";

    await using var file = File.Create(wavFile);
    await req.Body.CopyToAsync(file);

    return wavFile;
}
