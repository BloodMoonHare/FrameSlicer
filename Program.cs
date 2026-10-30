using System;
using System.Diagnostics;
using FFMpegCore;
using System.Data;
using System.IO;

class Program {
    static async Task Main() { 
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        if (OperatingSystem.IsWindows())
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = @"C:\ffmpeg\bin" });
        else if (OperatingSystem.IsMacOS())
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = "/opt/homebrew/bin" });
        else
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = "/usr/bin" });
        ColorWriteLine("<<FrameSlicer>>", ConsoleColor.DarkCyan);
        System.Console.WriteLine("~Beta\n");
        string? pathToVideo = null;
        while(true){
            Console.Write("Video File: ");
            pathToVideo = Console.ReadLine();
            if(File.Exists(pathToVideo))
                break;
            else
                ColorWriteLine("! This file doesn't exist. Enter path to your video file", ConsoleColor.Cyan);
        }
        int? FPS = null;
        double timeStart = 0;
        double timeEnd = 0;
        while(true){
            try {
                Console.Write("FPS: ");
                FPS = Convert.ToUInt16(Console.ReadLine());
                System.Console.WriteLine("\nif you don't want enter the lenght you can enter 0 into 'To' it means the end of your video\nExample: From-10 To-0 | It means - from 10th second to end of the video\n\nIf ypu want to write minutes/hours you can use mathematical expressions\nExample: 2 minutes -> 60 | 5 hours -> 3600 * 5\n");
                Console.Write("From(seconds): ");
                timeStart = Convert.ToDouble(new DataTable().Compute(Console.ReadLine(), null));
                Console.Write("To(seconds): ");
                timeEnd = Convert.ToDouble(new DataTable().Compute(Console.ReadLine(), null));
            }
            catch (FormatException) {
                ColorWriteLine("! please try again, enter only NUMBERs", ConsoleColor.Cyan);
            }
            catch (OverflowException) {
                ColorWriteLine("! please enter number above\n-FPS: 0 - 3000\n-From/To: 0 - 18446744073709551616", ConsoleColor.Cyan);
            }
            var vidInfo = await FFProbe.AnalyseAsync(pathToVideo);
            double lenghtSeconds = vidInfo.Duration.TotalSeconds;
            if(timeEnd == 0)
                timeEnd = lenghtSeconds;
            if(FPS != null && timeStart < timeEnd && timeStart >= 0 && timeEnd >= 0)
                break;
            else
                ColorWriteLine("something went wrong, please check:\n1. 'From' < 'To'\n2. 'From' >= 0, 'To' >= 0", ConsoleColor.Red);
        }
        string FolderWithFrames = $"FramesOfVideos/Folder_{Path.GetFileNameWithoutExtension(pathToVideo)}_{FPS}fps";

        if (Directory.Exists(FolderWithFrames)) {
            if(Directory.GetFiles(FolderWithFrames).Length != 0 && FPS != 0) {
               await VideoToFile(pathToVideo, FPS, timeStart, timeEnd); 
            }
        }
        else {
            await VideoToFile(pathToVideo, FPS, timeStart, timeEnd); 
        }
    }

    static async Task VideoToFile(string path, int? fps, double start, double end) {
        string FolderName = $"Frames_{Path.GetFileNameWithoutExtension(path)}_{fps}fps_{start}-{end}";
        string DirectoryWithFrames = Path.Combine($"FramesOfVideos/{FolderName}");
        Directory.CreateDirectory(DirectoryWithFrames);
        await FFMpegArguments
            .FromFileInput(path, true, inp => inp
            .Seek(TimeSpan.FromSeconds(start))
            .WithDuration(TimeSpan.FromSeconds(end - start))
            )
            .OutputToFile(Path.Combine(DirectoryWithFrames, "frame_%05d.png"), true,
            n => n.WithCustomArgument($"-vf fps={fps}"))
            .ProcessAsynchronously();
        if(Directory.EnumerateFiles(DirectoryWithFrames).Count() == 0) {
            Directory.Delete(DirectoryWithFrames);
            ColorWriteLine("!!!  This file doesn't exist or this isn't video file", ConsoleColor.Red);
        }
    }

    static void ColorWriteLine(string text, ConsoleColor color) {
        Console.ForegroundColor = color;
        System.Console.WriteLine(text);
        Console.ResetColor();
    }
}
