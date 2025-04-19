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

namespace LVST
{
    class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; } = true;
            
            public string Btih { get; set; }
            private string _magnet;

            [Option('t', "torrent", Required = false, HelpText = "The torrent link to download and play")]
            public string Torrent { get; set; } = "http://www.publicdomaintorrents.com/bt/btdownload.php?type=torrent&file=Charlie_Chaplin_Mabels_Strange_Predicament.avi.torrent";

            [Option('m', "magnet", Required = false, HelpText = "The magnet link to download and play")]
            public string Magnet
            {
                get => _magnet;
                set
                {
                    _magnet = value;
                    Btih = value.IndexOf("btih:", StringComparison.Ordinal) > 0 ? value.Substring(value.IndexOf("btih:", StringComparison.Ordinal) + 5, 40) : string.Empty;
                    
                }
            }
            // TODO: If multiple chromecast on the network, allow selecting it interactively via the CLI
            [Option('c', "cast", Required = false, HelpText = "Cast to the chromecast")]
            public bool Chromecast { get; set; }

            [Option('s', "save", Required = false, HelpText = "Whether to save the media file. Defaults to true.")]
            public bool Save { get; set; } = true;

            [Option('p', "path", Required = false, HelpText = "Set the path where to save the media file.")]
            public string Path { get; set; } = Environment.CurrentDirectory;
        }

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
            var stream = await StartTorrenting(cliOptions);

            await StartPlayback(stream, cliOptions);

            ReadKey();
        }

        private static async Task StartPlayback(Stream stream, Options cliOptions)
        {
            WriteLine("LibVLCSharp -> Loading LibVLC core library...");

            Core.Initialize();

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

        private static async Task<Stream> StartTorrenting(Options cliOptions)
        {
            TorrentManager manager = null;
            var engine = new ClientEngine();
            if (string.IsNullOrWhiteSpace(cliOptions.Magnet))
            {
                WriteLine("MonoTorrent -> Loading torrent file...");
                var torrent = await Torrent.LoadAsync(new Uri(cliOptions.Torrent),
                    Path.Combine(Environment.CurrentDirectory, "video.torrent"));
                
                WriteLine("MonoTorrent -> Creating a new StreamProvider...");
                manager = await engine.AddStreamingAsync (torrent, cliOptions.Path);
                
                if (cliOptions.Verbose)
                {
                    manager.PeerConnected += (o, e) => WriteLine($"MonoTorrent -> Connection succeeded: {e.Peer.Uri}");
                    manager.ConnectionAttemptFailed += (o, e) => WriteLine($"MonoTorrent -> Connection failed: {e.Peer.ConnectionUri} - {e.Reason} - {e.Peer}");
                }

                WriteLine("MonoTorrent -> Starting the StreamProvider...");
                await manager.StartAsync();

            }
            else
            {
                MagnetLink magnetLink = MagnetLink.FromUri(new Uri(cliOptions.Magnet));
                manager = await engine.AddStreamingAsync (magnetLink, cliOptions.Path);
                
                if (cliOptions.Verbose)
                {
                    manager.PeerConnected += (o, e) => WriteLine($"MonoTorrent -> Connection succeeded: {e.Peer.Uri}");
                    manager.ConnectionAttemptFailed += (o, e) => WriteLine($"MonoTorrent -> Connection failed: {e.Peer.ConnectionUri} - {e.Reason} - {e.Peer}");
                }

                WriteLine("MonoTorrent -> Starting the StreamProvider...");
                await manager.StartAsync();
            }



            // As the TorrentManager was created using an actual torrent, the metadata will already exist.
            // This is future proofing in case a MagnetLink is used instead
            if (!manager.HasMetadata)
            {
                WriteLine("MonoTorrent -> Waiting for the metadata to be downloaded from a peer...");
                await manager.WaitForMetadataAsync();
            }

            var largestFile = manager.Files.OrderByDescending(t => t.Length).First();
            WriteLine($"MonoTorrent -> Creating a stream for the torrent file... {largestFile.Path}");
            var stream = await manager.StreamProvider.CreateStreamAsync(largestFile);

            return stream;
        }

        private static void RendererDiscoverer_ItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
        {
            WriteLine($"LibVLCSharp -> Found a new renderer {e.RendererItem.Name} of type {e.RendererItem.Type}!");
            renderers.Add(e.RendererItem);
        }
    }
}
