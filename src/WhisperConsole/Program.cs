﻿using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;


using var httpClient = new HttpClient
{
    //BaseAddress = new("http://localhost:5282")
    BaseAddress = new("http://192.168.1.183:5000")
};

while (true)
{
    string wavFileName = $"output-{Guid.NewGuid():N}.wav";
    TaskCompletionSource completionSource = new();
    using var waveIn = new WaveInEvent();
    waveIn.WaveFormat = new WaveFormat(16000, 1);
    var writer = new WaveFileWriter(wavFileName, waveIn.WaveFormat);

    var disposed = false;
    var writeLock = new object();

    waveIn.DataAvailable += (s, a) =>
    {
        lock (writeLock)
        {
            if (!disposed)
                writer.Write(a.Buffer, 0, a.BytesRecorded);
        }
    };

    waveIn.RecordingStopped += async (s, a) =>
    {
        lock (writeLock)
        {
            disposed = true;
            writer.Dispose();
        }

        await SendItToTheInternetAsync(wavFileName);
        completionSource.SetResult();
    };

    waveIn.StartRecording();
    Console.WriteLine("Recording... press enter to stop");
    Console.ReadLine();
    waveIn.StopRecording();

    await completionSource.Task;
}





async Task SendItToTheInternetAsync(string wavFileName)
{
    using var content = new StreamContent(File.OpenRead(wavFileName));
    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

    using var res = await httpClient.PostAsync("listen", content);
    DateTime started = DateTime.Now;
    var str = await res.Content.ReadAsStringAsync();
    DateTime ended = DateTime.Now;
    Console.WriteLine($"Took: {(ended - started).TotalMilliseconds} ms");
    Console.WriteLine(str);
}

//var modelName = "ggml-large-v1.bin";
//if (!File.Exists(modelName))
//{
//    Console.WriteLine("Downloading...");
//    await using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Base);
//    await using var fileWriter = File.OpenWrite(modelName);
//    await modelStream.CopyToAsync(fileWriter);
//    Console.WriteLine("Downloaded!");
//}


//using var whisperFactory = WhisperFactory.FromPath(modelName);

//using var processor = whisperFactory.CreateBuilder()
//                                    .WithLanguage("auto")
//                                    .Build();

//await using var fileStream = File.OpenRead(wavFileName);

//for (int i = 0; i < 20; i++)
//{
//    fileStream.Position = 0;
//    await foreach (var result in processor.ProcessAsync(fileStream))
//    {
//        Console.WriteLine($"{result.Start}->{result.End}: {result.Text}");
//    } 
//}