using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;

string wavFileName = "output.wav";
using var waveIn = new WaveInEvent();
waveIn.WaveFormat = new WaveFormat(16000, 1);
await using var writer = new WaveFileWriter(wavFileName, waveIn.WaveFormat);

waveIn.DataAvailable += async (s, a) =>
{
    await writer.WriteAsync(a.Buffer, 0, a.BytesRecorded);
};

waveIn.RecordingStopped += async (s, a) =>
{
    await writer.DisposeAsync();
    waveIn.Dispose();
};

waveIn.StartRecording();
Console.WriteLine("Recording... press enter to stop");
Console.ReadLine();
waveIn.StopRecording();

var httpClient = new HttpClient
{
    BaseAddress = new("http://localhost:5282")
    //BaseAddress = new("http://192.168.1.183:5000")
};

using var content = new StreamContent(File.OpenRead(wavFileName));
content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

var res = await httpClient.PostAsync("listen", content);
var str = await res.Content.ReadAsStringAsync();
return;


var modelName = "ggml-large-v1.bin";
if (!File.Exists(modelName))
{
    Console.WriteLine("Downloading...");
    await using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Base);
    await using var fileWriter = File.OpenWrite(modelName);
    await modelStream.CopyToAsync(fileWriter);
    Console.WriteLine("Downloaded!");
}


using var whisperFactory = WhisperFactory.FromPath(modelName);

using var processor = whisperFactory.CreateBuilder()
                                    .WithLanguage("auto")
                                    .Build();

await using var fileStream = File.OpenRead(wavFileName);

for (int i = 0; i < 20; i++)
{
    fileStream.Position = 0;
    await foreach (var result in processor.ProcessAsync(fileStream))
    {
        Console.WriteLine($"{result.Start}->{result.End}: {result.Text}");
    } 
}