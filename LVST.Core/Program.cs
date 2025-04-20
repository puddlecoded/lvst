using CommandLine;
using LibVLCSharp.Shared;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Streaming;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Error = CommandLine.Error;
using static System.Console;
using System.Linq;

namespace LVST.Core
{
    class Program
    {
    

        static LibVLC libVLC;
        static MediaPlayer mediaPlayer;
        static readonly List<RendererItem> renderers = new List<RendererItem>();

        static async Task Main(string[] args)
        {
            var result = await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunOptions);

            result.WithNotParsed(HandleParseError);
        }

        private static void HandleParseError(IEnumerable<Error> error)
        {
            WriteLine($"Error while parsing...");
        }

        private static async Task RunOptions(Options cliOptions)
        {
            var streamingService = new StreamingService();
            streamingService.OnReady += async (streamFile) =>
            {
                Console.WriteLine($"Streaming -> Stream is ready!");
                
                Play(streamFile, cliOptions);

            };
            var ts = new TorrentService();
            var t = await ts.StartAsync(cliOptions);
             streamingService.StreamAsync(t.Stream, t.FileName);
            
 
            ReadKey();
        }

        private static void Play(string s, Options cliOptions)
        {
            WriteLine($"Playing {s}....");
            LibVLCSharp.Shared.Core.Initialize();

            libVLC = new LibVLC();
            if(cliOptions.Verbose)
                libVLC.Log += (l, e) => WriteLine($"LibVLC -> {e.FormattedLog}");

            using var media = new Media(libVLC,s, FromType.FromPath, []);
            mediaPlayer = new MediaPlayer(media);
            mediaPlayer.Play();

        }

        private static async Task StartPlayback(Stream stream, Options cliOptions)
        {
            WriteLine("LibVLCSharp -> Loading LibVLC core library...");

            LibVLCSharp.Shared.Core.Initialize();

            libVLC = new LibVLC();
            if(cliOptions.Verbose)
                libVLC.Log += (s, e) => WriteLine($"LibVLC -> {e.FormattedLog}");

            using var media = new Media(libVLC, new StreamMediaInput(stream));
            mediaPlayer = new MediaPlayer(media);

            if (cliOptions.Chromecast)
            {
                var result = await FindAndUseChromecast();
                if (!result)
                    return;
            }

            WriteLine("LibVLCSharp -> Starting playback...");
            mediaPlayer.Play();
        }

        private static async Task<bool> FindAndUseChromecast()
        {
            using var rendererDiscoverer = new RendererDiscoverer(libVLC);
            rendererDiscoverer.ItemAdded += RendererDiscoverer_ItemAdded;
            if (rendererDiscoverer.Start())
            {
                WriteLine("LibVLCSharp -> Searching for chromecasts...");
                // give it some time...
                await Task.Delay(2000);
            }
            else
            {
                WriteLine("LibVLCSharp -> Failed starting the chromecast discovery");
            }

            rendererDiscoverer.ItemAdded -= RendererDiscoverer_ItemAdded;

            if (!renderers.Any())
            {
                WriteLine("LibVLCSharp -> No chromecast found... aborting.");
                return false;
            }

            mediaPlayer.SetRenderer(renderers.First());
            return true;
        }

     
        private static void RendererDiscoverer_ItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
        {
            WriteLine($"LibVLCSharp -> Found a new renderer {e.RendererItem.Name} of type {e.RendererItem.Type}!");
            renderers.Add(e.RendererItem);
        }
    }
}
