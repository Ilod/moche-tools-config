using System;
using System.IO;

namespace Configuration
{
  class FileSystem
  {
    public static bool BuiltinMove(Configuration c, Arguments args)
    {
      string src = args.Format(c, "{MoveSource}");
      string dest = args.Format(c, "{MoveDest}");
      bool overwrite = args.ParseBoolArg(c, "Overwrite");
      c.Console.WriteLine(LogLevel.Trace, "Move {0} to {1}", src, dest);
      if (c.OnlyPrint)
        return true;
      bool ignoreUnexisting = args.ParseBoolArg(c, "IgnoreUnexisting");
      bool createDest = args.ParseBoolArg(c, "CreateDest");
      if (createDest)
        Directory.CreateDirectory(Directory.GetParent(dest).FullName);
      if (Directory.Exists(src))
      {
        if (!overwrite && Directory.Exists(dest))
        {
          c.Console.WriteLine(LogLevel.Error, "Folder {0} already exists in {1}", Path.GetFileName(src), Path.GetDirectoryName(Path.GetFullPath(dest)));
          return false;
        }
        Directory.Move(src, dest);
      }
      else if (File.Exists(src))
      {
        if (!overwrite && File.Exists(dest))
        {
          c.Console.WriteLine(LogLevel.Error, "File {0} already exists in {1}", Path.GetFileName(src), Path.GetDirectoryName(Path.GetFullPath(dest)));
          return false;
        }
        File.Move(src, dest);
      }
      else if (!ignoreUnexisting)
      {
        c.Console.WriteLine(LogLevel.Error, "File {0} not found", src);
        return false;
      }
      return true;
    }

    private static bool RecursiveCopy(Configuration c, DirectoryInfo src, string dest, bool overwrite)
    {
      foreach (FileInfo fi in src.GetFiles())
      {
        string fDest = Path.Combine(dest, fi.Name);
        if (!overwrite && File.Exists(fDest))
        {
          c.Console.WriteLine(LogLevel.Error, "File {0} already exists in {1}", fi.Name, dest);
          return false;
        }
        fi.CopyTo(Path.Combine(dest, fi.Name), overwrite);
      }
      foreach (DirectoryInfo di in src.GetDirectories())
      {
        string subDest = Path.Combine(dest, di.Name);
        Directory.CreateDirectory(subDest);
        RecursiveCopy(c, di, subDest, overwrite);
      }
      return true;
    }

    public static bool BuiltinCopy(Configuration c, Arguments args)
    {
      string src = args.Format(c, "{CopySource}");
      string dest = args.Format(c, "{CopyDest}");
      bool overwrite = args.ParseBoolArg(c, "Overwrite");
      c.Console.WriteLine(LogLevel.Trace, "Copy {0} to {1}", src, dest);
      if (c.OnlyPrint)
        return true;
      bool ignoreUnexisting = args.ParseBoolArg(c, "IgnoreUnexisting");
      bool createDest = args.ParseBoolArg(c, "CreateDest");
      if (createDest)
        Directory.CreateDirectory(Directory.GetParent(dest).FullName);
      if (Directory.Exists(src))
        return RecursiveCopy(c, new DirectoryInfo(src), dest, overwrite);
      else if (File.Exists(src))
      {
        if (!overwrite && File.Exists(dest))
        {
          c.Console.WriteLine(LogLevel.Error, "File {0} already exists in {1}", Path.GetFileName(src), Path.GetDirectoryName(Path.GetFullPath(dest)));
          return false;
        }
        File.Copy(src, dest, overwrite);
      }
      else if (!ignoreUnexisting)
      {
        c.Console.WriteLine(LogLevel.Error, "File {0} not found", src);
        return false;
      }
      return true;
    }

    public static bool BuiltinDelete(Configuration c, Arguments args)
    {
      string path = args.Format(c, "{Path}");
      c.Console.WriteLine(LogLevel.Trace, "rm {0}", path);
      if (c.OnlyPrint)
        return true;
      bool recursive = args.ParseBoolArg(c, "Recursive");
      bool ignoreUnexisting = args.ParseBoolArg(c, "IgnoreUnexisting");
      if (Directory.Exists(path))
      {
        Directory.Delete(path, recursive);
      }
      else if (File.Exists(path))
      {
        File.Delete(path);
      }
      else if (!ignoreUnexisting)
      {
        c.Console.WriteLine(LogLevel.Error, "File {0} not found for deletion", path);
        return false;
      }
      return true;
    }

    public static bool BuiltinCreateDirectory(Configuration c, Arguments args)
    {
      string path = args.Format(c, "{Path}");
      c.Console.WriteLine(LogLevel.Trace, "mkdir {0}", path);
      if (c.OnlyPrint)
        return true;
      bool recursive = args.ParseBoolArg(c, "Recursive");
      bool ignoreExisting = args.ParseBoolArg(c, "IgnoreExisting");
      if (File.Exists(path))
      {
        c.Console.WriteLine(LogLevel.Error, "{0} already exists and is a file instead of a directory", path);
        return false;
      }
      if (!ignoreExisting && Directory.Exists(path))
      {
        c.Console.WriteLine(LogLevel.Error, "{0} already exists", path);
        return false;
      }
      if (!recursive && !Directory.Exists(Directory.GetParent(path).FullName))
      {
        c.Console.WriteLine(LogLevel.Error, "Parent directory {0} not found", Directory.GetParent(path).FullName);
        return false;
      }
      Directory.CreateDirectory(path);
      return true;
    }
    
    public static bool BuiltinPushWorkingDirectory(Configuration c, Arguments args)
    {
      bool create = args.ParseBoolArg(c, "Create");
      string path = args.Format(c, "{Path}");
      c.Console.WriteLine(LogLevel.Trace, "pushd {0}", path);
      if (c.OnlyPrint)
        return true;
      if (create)
        Directory.CreateDirectory(path);
      else if (!Directory.Exists(path))
      {
        c.Console.WriteLine(LogLevel.Error, "Directory {0} not found", path);
        return false;
      }
      c.PushWorkingDirectory(path);
      return true;
    }

    public static bool BuiltinPopWorkingDirectory(Configuration c, Arguments args)
    {
      c.Console.WriteLine(LogLevel.Trace, "popd");
      if (c.OnlyPrint)
        return true;
      return c.PopWorkingDirectory();
    }

    public static bool BuiltinSetWorkingDirectory(Configuration c, Arguments args)
    {
      bool create = args.ParseBoolArg(c, "Create");
      string path = args.Format(c, "{Path}");
      c.Console.WriteLine(LogLevel.Trace, "cd {0}", path);
      if (c.OnlyPrint)
        return true;
      if (create)
        Directory.CreateDirectory(path);
      else if (!Directory.Exists(path))
      {
        c.Console.WriteLine(LogLevel.Error, "Directory {0} not found", path);
        return false;
      }
      c.SetWorkingDirectory(path);
      return true;
    }
  }
}
