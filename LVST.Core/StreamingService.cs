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

    public async void StreamAsync(Stream source, CancellationToken cancellationToken = default,
        string ffmpegPath = @"C:\Users\piotr\AppData\Local\Microsoft\WinGet\Links\ffmpeg.exe",
        string ffmpegArgs =
            "-hwaccel d3d11va -i  - -sn -c:v h264_amf -ac 2 -c:a copy -f hls -hls_time 20 -hls_list_size 0 -hls_playlist_type event {plname}")

    {
        bool notified = false;
        var dir = Path.Combine(Path.GetTempPath(), "LVST.Core");
        var plname = "stream.m3u8";
        var playlist = Path.Combine(dir, "stream.m3u8");
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
                if (s.Contains("stream.m3u8") && File.Exists(Path.Combine(dir, "stream.m3u8")) && !notified)
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