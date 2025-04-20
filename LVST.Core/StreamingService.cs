using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LVST.Core;

using CliWrap;
public class StreamingService: IDisposable, IAsyncDisposable
{
    private CancellationTokenSource _cts;
    public event Action<string> OnReady;
    public event Action<string> OnCancel;

    public async Task StreamAsync(Stream source, string fname, CancellationToken cancellationToken = default,
        string ffmpegPath = @"C:\Users\piotr\AppData\Local\Microsoft\WinGet\Links\ffmpeg.exe",
        string ffmpegArgs =
            "-hwaccel d3d11va -i  - -sn -c:v h264 -ac 2 -c:a aac -f hls -hls_time 20 -hls_list_size 0 -hls_playlist_type event {plname}")

    {

        try
        {
            await CancelStreamAsync();
            

            bool notified = false;
            var dir = Path.Combine(Path.GetTempPath(), "LVST", fname.ToBase64());
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
                .WithStandardInputPipe(PipeSource.FromStream(source,false));

            await cmd.ExecuteAsync(_cts!.Token, cancellationToken);
        }
        catch (OperationCanceledException)
        {
          await CancelStreamAsync();
        }
    }

    public async Task CancelStreamAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        _cts = new CancellationTokenSource();

        // if (_stream != null)
        // {
        //     _stream.Close();
        //     await _stream.DisposeAsync();
        // }
        //
        OnCancel?.Invoke($"Stream cancelled");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _cts?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_cts != null) await _cts.CancelAsync();
    }
}