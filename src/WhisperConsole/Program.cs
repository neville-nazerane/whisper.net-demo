using NAudio.Wave;
using System.Diagnostics;


using var httpClient = new HttpClient
{
    //BaseAddress = new("http://localhost:5282")
    BaseAddress = new("http://192.168.1.183:5000")
};


string? file;

//file = @"C:\Users\liven\output-2a412e90a7594cd4a8d8f319739d0b47.wav";

file = args.FirstOrDefault();


if (file is not null)
{
    await SendItToTheInternetAsync(file);
    return;
}

while (true)
    await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), HandledRunningAsync());





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

async Task HandledRunningAsync()
{
    try
    {
        string wavFileName = $"output-{Guid.NewGuid():N}.wav";

        Console.WriteLine("Saving " + wavFileName);
        if (OperatingSystem.IsWindows())
            await RunMeAsync(wavFileName);
        else
            await RunMeOnLinuxAsync(wavFileName);

        if (File.Exists(wavFileName))
        {
            await SendItToTheInternetAsync(wavFileName);
            File.Delete(wavFileName);
        }
        else
        {
            Console.WriteLine("No file saved " + wavFileName);
        }

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}

async Task RunMeOnLinuxAsync(string wavFileName)
{
    var completion = new TaskCompletionSource();

    var psi = new ProcessStartInfo
    {
        FileName = "arecord",
        ArgumentList = { "-Dshared_mic", "-f", "S32_LE", "-r", "16000", "-c", "4", wavFileName },
        RedirectStandardOutput = true,
        //RedirectStandardError = true
    };

    using var process = new Process
    {
        StartInfo = psi,
        EnableRaisingEvents = true,
    };

    process.OutputDataReceived += (_, s) => Console.WriteLine(s.Data);

    process.ErrorDataReceived += (_, s) => completion.SetException(new Exception(s.Data));

    process.Exited += (_, _) => completion.TrySetResult();

    process.Start();
    await Task.Delay(TimeSpan.FromSeconds(15));
    process.Kill();
    //process.BeginOutputReadLine();
    //process.BeginErrorReadLine();

    await completion.Task;
    await Task.Delay(TimeSpan.FromMilliseconds(500));
}

async Task RunMeAsync(string wavFileName)
{
    TaskCompletionSource completionSource = new();

    using var waveIn = new WaveInEvent();
    WaveFileWriter? writer = null;
    try
    {

        waveIn.WaveFormat = new WaveFormat(16000, 1);
        writer = new(wavFileName, waveIn.WaveFormat);

    }
    catch (Exception ex)
    {
        completionSource.SetException(ex);
    }

    if (writer is not null)
    {
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

        waveIn.RecordingStopped += (s, a) =>
        {
            lock (writeLock)
            {
                disposed = true;
                writer.Dispose();
            }


            completionSource.SetResult();
        };

        try
        {
            waveIn.StartRecording();
            await Task.Delay(TimeSpan.FromSeconds(10));
            waveIn.StopRecording();
        }
        catch (Exception ex)
        {
            completionSource.SetException(ex);
        }
    }

    await completionSource.Task;
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