using System;
using CommandLine;

namespace LVST.Core;
public class Options
{
    [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
    public bool Verbose { get; set; } = false;
            
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
