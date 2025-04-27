using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;
using System.Threading;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.LibraryLoader;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


// START UP
var modelName = "ggml-large-v3.bin";

Directory.CreateDirectory("audio");

if (!File.Exists(modelName))
{
    Console.WriteLine("Downloading...");
    await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Base);
    await using var fileWriter = File.OpenWrite(modelName);
    await modelStream.CopyToAsync(fileWriter);
    Console.WriteLine("Downloaded!");
}

Console.WriteLine("Getting model...");




app.MapGet("/", () => "Hello API!");

app.MapPost("/listen", StreamMeDirectlyAsync);

await app.RunAsync();


async Task<string> StreamMeDirectlyAsync(HttpRequest req, CancellationToken cancellationToken = default)
{
    await using var ms = new MemoryStream();
    await req.Body.CopyToAsync(ms, cancellationToken);
    ms.Position = 0;

    DateTime? startedOn = null;

    if (req.Headers.TryGetValue("startedOn", out var startedOns) && startedOns.Any())
        if (long.TryParse(startedOns.First(), out var ticks))
            startedOn = new DateTime(ticks, DateTimeKind.Utc);

    Console.WriteLine("Getting processor...");
    using var whisperFactory = WhisperFactory.FromPath(modelName);

    using var processor = whisperFactory.CreateBuilder()
                                        .WithLanguage("en")
                                        .Build();

    var builder = new StringBuilder();
    await foreach (var result in processor.ProcessAsync(ms, cancellationToken))
    {
        Console.WriteLine();
        if (startedOn is null)
            Console.WriteLine($"Probab: {result.Probability:F4}, no voice: {result.NoSpeechProbability:F4} min: {result.MinProbability:F4}, max: {result.MaxProbability:F4}");
        else
        {
            var time = startedOn.Value.ToLocalTime();
            var startTime = time + result.Start;
            var endTime = time + result.End;
            Console.WriteLine($"Probably {result.Probability:F4}, from {startTime} to {endTime}");
        }
        Console.WriteLine($"Timestamps: {result.Start}, {result.End}");
        Console.WriteLine(result.Text);

        builder.AppendLine(result.Text);
    }

    return builder.ToString();
}

async Task<string> StreamMeAsync(HttpRequest req)
{
    var wavFile = $"audio/{Guid.NewGuid():N}.wav";

    await using (var file = File.Create(wavFile))
        await req.Body.CopyToAsync(file);

    var res = await ReadFileAsync(wavFile);

    File.Delete(wavFile);
    return res;
}


async Task<string> ReadFileAsync(string wavFileName)
{

    Console.WriteLine("Getting processor...");

    using var whisperFactory = WhisperFactory.FromPath(modelName);

    using var processor = whisperFactory.CreateBuilder()
                                        .WithLanguage("en")
                                        .Build();

    Console.WriteLine("Getting file stream...");
    await using var fileStream = File.OpenRead(wavFileName);

    var builder = new StringBuilder();
    await foreach (var result in processor.ProcessAsync(fileStream))
    {
        Console.WriteLine();
        Console.WriteLine($"Probab: {result.Probability:F4}, no voice: {result.NoSpeechProbability:F4} min: {result.MinProbability:F4}, max: {result.MaxProbability:F4}");
        Console.WriteLine($"Timestamps: {result.Start}, {result.End}");
        Console.WriteLine(result.Text);

        builder.AppendLine(result.Text);
    }

    return builder.ToString();
}

