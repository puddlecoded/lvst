#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoTorrent;
using MonoTorrent.Client;

namespace LVST.Core;

public class TorrentService
{
    private Stream? _stream;
       public async Task<TorrentServiceResponse?> StartAsync(Options cliOptions,CancellationToken cancellationToken = default)
        {
            TorrentManager manager;
            var engine = new ClientEngine();
            if (string.IsNullOrWhiteSpace(cliOptions.Magnet))
            {
                Console.WriteLine("MonoTorrent -> Loading torrent file...");
                var torrent = await Torrent.LoadAsync(new Uri(cliOptions.Torrent),
                    Path.Combine(Environment.CurrentDirectory, "video.torrent"));
                
                Console.WriteLine("MonoTorrent -> Creating a new StreamProvider...");
                manager = await engine.AddStreamingAsync (torrent, cliOptions.Path);
                
                if (cliOptions.Verbose)
                {
                    manager.PeerConnected += (o, e) => Console.WriteLine($"MonoTorrent -> Connection succeeded: {e.Peer.Uri}");
                    manager.ConnectionAttemptFailed += (o, e) => Console.WriteLine($"MonoTorrent -> Connection failed: {e.Peer.ConnectionUri} - {e.Reason} - {e.Peer}");
                }

                Console.WriteLine("MonoTorrent -> Starting the StreamProvider...");
                await manager.StartAsync();

            }
            else
            {
                MagnetLink magnetLink = MagnetLink.FromUri(new Uri(cliOptions.Magnet));
                manager = await engine.AddStreamingAsync (magnetLink, cliOptions.Path);
                
                if (cliOptions.Verbose)
                {
                    manager.PeerConnected += (o, e) => Console.WriteLine($"MonoTorrent -> Connection succeeded: {e.Peer.Uri}");
                    manager.ConnectionAttemptFailed += (o, e) => Console.WriteLine($"MonoTorrent -> Connection failed: {e.Peer.ConnectionUri} - {e.Reason} - {e.Peer}");
                }

                Console.WriteLine("MonoTorrent -> Starting the StreamProvider...");
                await manager.StartAsync();
            }



            // As the TorrentManager was created using an actual torrent, the metadata will already exist.
            // This is future proofing in case a MagnetLink is used instead
            if (!manager.HasMetadata)
            {
               Console.WriteLine("MonoTorrent -> Waiting for the metadata to be downloaded from a peer...");
                await manager.WaitForMetadataAsync(cancellationToken);
            }
            var files = manager.Files.OrderByDescending(t => t.Length).ToList();
            if (files.Count >0)
            {
                return await StreamFile(files.First(), manager, cancellationToken);
            }
            return null;
        }


        public List<ITorrentFileInfo> Sources
        {
            get;
            set;
        }

        public async Task<TorrentServiceResponse?> StreamFile(
            ITorrentFileInfo? file,
            TorrentManager manager,
            CancellationToken cancellationToken)
        {
            try
            {
                if (_stream != null)
                {
                    _stream.Close();
                    await _stream.DisposeAsync();
                }
                if (file == null) return null;
                _stream = await manager.StreamProvider.CreateStreamAsync(file, cancellationToken);
                if (_stream == null)
                {
                    Console.WriteLine("MonoTorrent -> Failed to create stream");
                    return null;
                }

                return new TorrentServiceResponse() { Stream = _stream, FileName = file.Path };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
}