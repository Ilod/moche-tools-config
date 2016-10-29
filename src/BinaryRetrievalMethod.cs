using System;
using System.Collections.Generic;
using System.IO;

namespace Configuration
{
  public class BinaryRetrievalMethod : IRetrievalMethod
  {
    public RemoteFile File = new RemoteFile();
    public string BuildBinarySubFolder = "";

    public string GetToolPath(Configuration c, Repo r, string executable)
    {
      return Path.Combine(c.RootPath, r.Name, c.BinarySubfolder, BuildBinarySubFolder, executable);
    }

    public bool TryRetrieve(Configuration c, Repo r)
    {
      if (string.IsNullOrEmpty(File.Url))
      {
        c.Console.WriteLine(LogLevel.Warning, "No url - skip");
        return false;
      }
      c.Console.WriteLine(LogLevel.Trace, "Download {0} ({1})", File.Url, File.FolderToExtract);
      if (c.OnlyPrint)
        return true;
      string dlFolder = Path.Combine(c.RootPath, r.Name, c.TempSubfolder);
      Directory.CreateDirectory(dlFolder);
      string dlFile = Path.Combine(dlFolder, "dl.tmp");
      c.Console.WriteLine(LogLevel.Info, "Fetching {0}", File.Url);
      if (!Downloader.DownloadSync(c, File.Url, dlFile, (sender, e) =>
      {
        c.Console.Write(LogLevel.Info, "\r{0}% ({1}/{2})", e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
      }))
        return false;
      string uncompressedFolder = Uncompresser.Uncompress(dlFile, Uncompresser.GetCompressionFromExt(File.Url));
      string folderToExtract = string.IsNullOrEmpty(File.FolderToExtract) ? uncompressedFolder : Path.Combine(uncompressedFolder, File.FolderToExtract);
      string destFolder = Path.Combine(c.RootPath, r.Name, c.BinarySubfolder);
      Directory.CreateDirectory(destFolder);
      foreach (string d in Directory.GetDirectories(folderToExtract))
        Directory.Move(d, Path.Combine(destFolder, Path.GetFileName(d)));
      foreach (string f in Directory.GetFiles(folderToExtract))
        System.IO.File.Move(f, Path.Combine(destFolder, Path.GetFileName(f)));
      if (Directory.Exists(uncompressedFolder))
        Directory.Delete(uncompressedFolder, true);
      return true;
    }

    public bool TryUpdate(Configuration c, Repo r)
    {
      return false;
    }

    public bool CanUpdate()
    {
      return false;
    }

    public bool ShouldAlwaysUpdate()
    {
      return false;
    }

    public bool NeedBuild()
    {
      return false;
    }

    public RetrievalMethodType GetRetrievalType()
    {
      return RetrievalMethodType.Binary;
    }
  }

}
