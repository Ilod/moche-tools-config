using System;
using System.IO;

namespace Configuration
{
  class FileSystem
  {
    public static void BuiltinMove(Configuration c, CommandInvocationMode mode, Arguments args)
    {
      string src = args.Format("{MoveSource}");
      string dest = args.Format("{MoveDest}");
      c.Console.WriteLine(LogLevel.Trace, "Move {0} to {1}", src, dest);
      if (c.OnlyPrint)
        return;
      bool ignoreUnexisting = args.ParseBoolArg("IgnoreUnexisting");
      bool createDest = args.ParseBoolArg("CreateDest");
      if (createDest)
        Directory.CreateDirectory(Directory.GetParent(dest).FullName);
      if (Directory.Exists(src))
        Directory.Move(src, dest);
      else if (File.Exists(src))
        File.Move(src, dest);
      else if (!ignoreUnexisting)
        throw new FileNotFoundException(src);
    }

    public static void BuiltinDelete(Configuration c, CommandInvocationMode mode, Arguments args)
    {
      string path = args.Format("{Path}");
      c.Console.WriteLine(LogLevel.Trace, "rm {0}", path);
      if (c.OnlyPrint)
        return;
      bool recursive = args.ParseBoolArg("Recursive");
      bool ignoreUnexisting = args.ParseBoolArg("IgnoreUnexisting");
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
        throw new FileNotFoundException(path);
      }
    }

    public static void BuiltinCreateDirectory(Configuration c, CommandInvocationMode mode, Arguments args)
    {
      string path = args.Format("{Path}");
      c.Console.WriteLine(LogLevel.Trace, "mkdir {0}", path);
      if (c.OnlyPrint)
        return;
      bool recursive = args.ParseBoolArg("Recursive");
      bool ignoreExisting = args.ParseBoolArg("IgnoreExisting");
      if (File.Exists(path))
        throw new IOException(string.Format("{0} already exists and is a file instead of a directory", path));
      if (!ignoreExisting && Directory.Exists(path))
        throw new IOException(string.Format("{0} already exists", path));
      if (!recursive && !Directory.Exists(Directory.GetParent(path).FullName))
        throw new FileNotFoundException(Directory.GetParent(path).FullName);
      Directory.CreateDirectory(path);
    }
    
    public static void BuiltinPushWorkingDirectory(Configuration c, CommandInvocationMode mode, Arguments args)
    {
      bool create = args.ParseBoolArg("Create");
      string path = args.Format("{Path}");
      c.Console.WriteLine(LogLevel.Trace, "pushd {0}", path);
      if (c.OnlyPrint)
        return;
      if (create)
        Directory.CreateDirectory(path);
      else if (!Directory.Exists(path))
        throw new FileNotFoundException(path);
      c.PushWorkingDirectory(path);
    }

    public static void BuiltinPopWorkingDirectory(Configuration c, CommandInvocationMode mode, Arguments args)
    {
      c.Console.WriteLine(LogLevel.Trace, "popd");
      if (c.OnlyPrint)
        return;
      c.PopWorkingDirectory();
    }

    public static void BuiltinSetWorkingDirectory(Configuration c, CommandInvocationMode mode, Arguments args)
    {
      bool create = args.ParseBoolArg("Create");
      string path = args.Format("{Path}");
      c.Console.WriteLine(LogLevel.Trace, "cd {0}", path);
      if (c.OnlyPrint)
        return;
      if (create)
        Directory.CreateDirectory(path);
      else if (!Directory.Exists(path))
        throw new FileNotFoundException(path);
      c.SetWorkingDirectory(path);
    }
  }
}
