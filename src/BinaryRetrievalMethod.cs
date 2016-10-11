using System;
using System.Collections.Generic;
using System.IO;

namespace Configuration
{
  public class BinaryRetrievalMethod : IRetrievalMethod
  {
    public Dictionary<Platform, RemoteFile> Url = new Dictionary<Platform, RemoteFile>();

    public string GetToolPath(Configuration c, Repo r, string executable)
    {
      return Path.Combine(c.RootPath, r.Name, c.BinarySubfolder, executable);
    }

    public bool TryRetrieve(Configuration c, Repo r)
    {
      RemoteFile file;
      if (!Url.TryGetValue(c.HostPlatform, out file))
      {
        Console.Error.WriteLine("No url for {0}", c.HostPlatform);
        return false;
      }
      if (c.OnlyPrint)
      {
        Console.WriteLine("Download {0} ({1})", file.Url, file.FolderToExtract);
        return true;
      }
      string dlFolder = Path.Combine(c.RootPath, r.Name, c.TempSubfolder);
      Directory.CreateDirectory(dlFolder);
      string dlFile = Path.Combine(dlFolder, "dl.tmp");
      Console.WriteLine("Fetching {0} ({1})", file.Url, file.FolderToExtract);
      try
      {
        Downloader.DownloadSync(file.Url, dlFile, (sender, e) =>
        {
          Console.Write("\r");
          Console.Write("{0}% ({1}/{2})", e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
        });
        string uncompressedFolder = Uncompresser.Uncompress(dlFile, Uncompresser.GetCompressionFromExt(file.Url));
        File.Move(Path.Combine(uncompressedFolder, file.FolderToExtract), Path.Combine(c.RootPath, r.Name, c.BinarySubfolder));
        return true;
      }
      catch (Exception e)
      {
        Console.Error.WriteLine(e.Message);
        return false;
      }
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
