using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;
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

app.MapPost("/listen", StreamMeAsync);

await app.RunAsync();



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
        Console.WriteLine(result.Text);
        builder.AppendLine(result.Text);
    }

    return builder.ToString();
}

