using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.LibraryLoader;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello API!");

app.MapPost("/listen", StreamMeAsync);

await app.RunAsync();


async Task<string> StreamMeAsync(HttpRequest req)
{
    Directory.CreateDirectory("audio");
    var wavFile = $"audio/{Guid.NewGuid():N}.wav";

    await using (var file = File.Create(wavFile))
        await req.Body.CopyToAsync(file);

    return await ReadFileAsync(wavFile);
}


async Task<string> ReadFileAsync(string wavFileName)
{
    var modelName = "ggml-medium.en.bin";
    if (!File.Exists(modelName))
    {
        Console.WriteLine("Downloading...");
        await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Base);
        await using var fileWriter = File.OpenWrite(modelName);
        await modelStream.CopyToAsync(fileWriter);
        Console.WriteLine("Downloaded!");
    }

    Console.WriteLine("Getting model...");


    using var whisperFactory = GetFactory(modelName);


    Console.WriteLine("Getting processor...");

    using var processor = whisperFactory.CreateBuilder()
                                        .WithLanguage("auto")
                                        .Build();

    Console.WriteLine("Getting file stream...");
    await using var fileStream = File.OpenRead(wavFileName);

    var builder = new StringBuilder();
    await foreach (var result in processor.ProcessAsync(fileStream))
        builder.AppendLine($"{result.Start}->{result.End}: {result.Text}");

    return builder.ToString();
}

WhisperFactory GetFactory(string modelName)
{
    RuntimeOptions.LoadedLibrary = RuntimeLibrary.Cuda;
    Console.WriteLine("loaded library has value: " + RuntimeOptions.LoadedLibrary.HasValue);

    var options = WhisperFactoryOptions.Default;
    options.UseGpu = true;
    return WhisperFactory.FromPath(modelName, options);
}