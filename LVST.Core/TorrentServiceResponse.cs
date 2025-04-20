using System.IO;

namespace LVST.Core;

public class TorrentServiceResponse
{
    public Stream Stream { get; set; }
    public string FileName { get; set; }
}