using System;
using System.IO;
using System.IO.Compression;

namespace Configuration
{

  public enum Compression
  {
    Zip,
    Tgz,
  };

  public static class Uncompresser
  {
    private static readonly string UncompressedFolder = "uncompressed";

    public static Compression GetCompressionFromExt(string path)
    {
      if (path.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
        return Compression.Zip;
      if (path.EndsWith(".tar.gz", StringComparison.InvariantCultureIgnoreCase))
        return Compression.Tgz;
      if (path.EndsWith(".tgz", StringComparison.InvariantCultureIgnoreCase))
        return Compression.Tgz;
      throw new Exception("Unknown compression format");
    }

    public static string Uncompress(string path, Compression format)
    {
      switch (format)
      {
        case Compression.Zip:
          return Unzip(path);
        case Compression.Tgz:
          return UnTgz(path);
        default:
          throw new Exception();
      }
    }

    public static string Unzip(string path)
    {
      string folder = Path.Combine(Path.GetDirectoryName(path), UncompressedFolder);
      Directory.CreateDirectory(folder);
      ZipFile.ExtractToDirectory(path, folder);
      return folder;
    }

    public static string UnTgz(string path)
    {
      using (MemoryStream ms = new MemoryStream())
      {
        using (FileStream fs = new FileStream(path, FileMode.Open))
        {
          using (GZipStream gz = new GZipStream(fs, CompressionMode.Decompress, true))
          {
            byte[] buffer = new byte[4096];
            for (;;)
            {
              int readCount = gz.Read(buffer, 0, buffer.Length);
              if (readCount <= 0)
                break;
              ms.Write(buffer, 0, readCount);
            }
          }
        }
        byte[] data = ms.ToArray();
        int block = 0;
        string uncompressFolder = Path.Combine(Path.GetDirectoryName(path), UncompressedFolder);
        while (block * 512 < data.Length && data[block * 512 + 156] != 0)
        {
          char type = (char)data[block * 512 + 156];
          int size = 0;
          for (int i = 0; i < 11; ++i)
          {
            size *= 8;
            size += data[block * 512 + 124 + i] - (byte)'0';
          }
          if (type == '0' || type == '7' || type == 'L')
          {
            string name = "";
            if (type == 'L')
            {
              ++block;
            }
            else
            {
              for (int i = block * 512 + 345; data[i] != 0; ++i)
                name += (char)data[i];
              if (name.Length != 0)
                name += '/';
            }
            for (int i = block * 512; data[i] != 0; ++i)
              name += (char)data[i];
            Directory.CreateDirectory(Path.Combine(uncompressFolder, Path.GetDirectoryName(name)));
            using (FileStream fs = new FileStream(Path.Combine(uncompressFolder, name), FileMode.CreateNew))
            {
              fs.Write(data, (block + 1) * 512, size);
            }
          }
          else if (type == 'K')
            ++block;
          ++block;
          block += (size + 511) / 512;
        }
        return uncompressFolder;
      }
    }
  }
}
