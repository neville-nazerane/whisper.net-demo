﻿using System;
using System.IO;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.Logger;


// We declare three variables which we will use later, ggmlType, modelFileName and wavFileName
var ggmlType = GgmlType.Base;
var modelFileName = "ggml-base.bin";
//var wavFileName = @"D:\Jack\Play\Github\whisper.net demo\src\WhisperAPI\audio\80ae713e20f14f23b65dd03d759bb7ee.wav";
var wavFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                               "api",
                               "audio",
                               $"{args[0]}.wav");

 using var whisperLogger = LogProvider.AddConsoleLogging(WhisperLogLevel.Debug);

// This section detects whether the "ggml-base.bin" file exists in our project disk. If it doesn't, it downloads it from the internet
if (!File.Exists(modelFileName))
    await DownloadModelAsync(modelFileName, ggmlType);

// This section creates the whisperFactory object which is used to create the processor object.
using var whisperFactory = WhisperFactory.FromPath(modelFileName);

// This section creates the processor object which is used to process the audio file, it uses language `auto` to detect the language of the audio file.
using var processor = whisperFactory.CreateBuilder()
                                    .WithLanguage("auto")
                                    .Build();

await using var fileStream = File.OpenRead(wavFileName);

// This section processes the audio file and prints the results (start time, end time and text) to the console.
await foreach (var result in processor.ProcessAsync(fileStream))
    Console.WriteLine($"{result.Start}->{result.End}: {result.Text}");

static async Task DownloadModelAsync(string fileName, GgmlType ggmlType)
{
    Console.WriteLine($"Downloading Model {fileName}");
    using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
    using var fileWriter = File.OpenWrite(fileName);
    await modelStream.CopyToAsync(fileWriter);
}