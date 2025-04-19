using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.EventStream;

namespace LVST.Core;

using CliWrap;
public class StreamingService
{
    public event Action<string> OnReady;

    public async void StreamAsync(Stream source, string fname, CancellationToken cancellationToken = default,
        string ffmpegPath = @"C:\Users\piotr\AppData\Local\Microsoft\WinGet\Links\ffmpeg.exe",
        string ffmpegArgs =
            "-hwaccel d3d11va -i  - -sn -c:v h264_amf -ac 2 -c:a aac -f hls -hls_time 20 -hls_list_size 0 -hls_playlist_type event {plname}")

    {
        bool notified = false;
        var dir = Path.Combine(Path.GetTempPath(), "LVST", fname.Substring(0,10));
        var plname = "stream.txt";
        var playlist = Path.Combine(dir, plname);
        if (!Directory.Exists(dir))
            
        {
            Directory.CreateDirectory(dir);

        }

        Console.WriteLine($"directory: {dir}");

        var cmd = Cli.Wrap(ffmpegPath)
            .WithArguments(ffmpegArgs.Replace("{plname}", plname))
            .WithWorkingDirectory(dir)
            .WithStandardErrorPipe(PipeTarget.ToDelegate((string s) =>
            {
                if (s.Contains(plname) && File.Exists(Path.Combine(dir, plname)) && !notified)
                {
                    notified = true;
                    Console.WriteLine($"Playlist created: {playlist}");
                    OnReady?.Invoke(playlist);
                    
                }
                
            }))
            .WithStandardInputPipe(PipeSource.FromStream(source));
        
            await cmd.ExecuteAsync(cancellationToken);
    }
}