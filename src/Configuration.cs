﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Configuration
{
  public enum Platform
  {
    Win32,
    Win64,
    Mac,
    Linux32,
    Linux64,
  }

  public class Configuration
  {
    [NonSerialized]
    public bool OnlyPrint = false;
    [NonSerialized]
    public List<RetrievalMethodType> AllowedRetrievalType = new List<RetrievalMethodType>();
    [DictionaryEmbeddedKey("Name")]
    public Dictionary<string, Tool> Tool = new Dictionary<string, Tool>();
    [DictionaryEmbeddedKey("Name")]
    public Dictionary<string, Command> Command = new Dictionary<string, Command>();
    [DictionaryEmbeddedKey("Name")]
    public Dictionary<string, Repo> Repo = new Dictionary<string, Repo>();
    private IDictionary<string, string> Arguments;

    private delegate void BuiltinFunction(Configuration c, CommandInvocationMode mode, Arguments args);
    private IDictionary<string, BuiltinFunction> Builtins = new Dictionary<string, BuiltinFunction>();

    public IDictionary<string, string> GetArguments()
    {
      return Arguments;
    }

    public void CallBuiltin(string builtin, CommandInvocationMode mode, IDictionary<string, string> args)
    {
      Builtins[builtin](this, mode, new Arguments(args));
    }

    public Command GetCommand(string name)
    {
      Command c = Command[name];
      c.Retrieve(this);
      return c;
    }

    public Tool GetTool(string name)
    {
      Tool t = Tool[name];
      t.Retrieve(this);
      return t;
    }

    public void InitBuiltins()
    {
      Command["download"] = new Command()
      {
        Name = "download",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "download",
            Arguments =
            {
              { "Url", null },
              { "DownloadDest", "{Dest}" },
              { "Dest", "{SourcePath}" },
            },
          },
        },
      };
      Command["uncompress"] = new Command()
      {
        Name = "uncompress",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "uncompress",
            Arguments =
            {
              { "Archive", null },
              { "UncompressDest", "{Dest}"},
              { "Dest", null },
              { "Format", "" },
              { "FolderToUncompress", "" },
            },
          },
        },
      };
      Command["download-archive"] = new Command()
      {
        Name = "download-archive",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "download",
            Arguments =
            {
              { "Url", null },
              { "UncompressDest", "{Dest}"},
              { "Dest", null },
              { "DownloadDest", "{UncompressDest}/tmp.tmp" },
            },
          },
          new ExecutableInvocation()
          {
            BuiltIn = "uncompress",
            Arguments =
            {
              { "Archive", "{UncompressDest}/tmp.tmp"},
              { "UncompressDest", "{Dest}"},
              { "Format", null },
              { "Dest", null },
            },
          },
          new ExecutableInvocation()
          {
            BuiltIn = "rm",
            Arguments =
            {
              { "Path", "{UncompressDest}/tmp.tmp"},
              { "UncompressDest", "{Dest}"},
              { "Dest", null },
            },
          },
        },
      };
      Command["pushd"] = new Command()
      {
        Name = "pushd",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "pushd",
            Arguments =
            {
              { "Create", "true" },
              { "Path", null },
            }
          }
        }
      };
      Command["popd"] = new Command()
      {
        Name = "popd",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "popd",
          }
        }
      };
      Command["cd"] = new Command()
      {
        Name = "cd",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "cd",
            Arguments =
            {
              { "Create", "true" },
              { "Path", null },
            }
          }
        }
      };
      Command["mkdir"] = new Command()
      {
        Name = "mkdir",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "mkdir",
            Arguments =
            {
              { "IgnoreExisting", "true" },
              { "Recursive", "true" },
              { "Path", null },
            }
          }
        }
      };
      Command["rm"] = new Command()
      {
        Name = "rm",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "rm",
            Arguments =
            {
              { "IgnoreUnexisting", "true" },
              { "Recursive", "true" },
              { "Path", null },
            }
          }
        }
      };
      Command["move"] = new Command()
      {
        Name = "move",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "move",
            Arguments =
            {
              { "IgnoreUnexisting", null },
              { "CreateDest", "true" },
              { "MoveSource", "{Source}" },
              { "Source", null },
              { "MoveDest", "{Dest}" },
              { "Dest", null },
            },
          }
        }
      };

      Builtins["download"] = (c, mode, arg) =>
      {
        string url = arg.Format("{Url}");
        string dest = arg.Format("{DownloadDest}");
        if (c.OnlyPrint)
        {
          Console.WriteLine("Download {0} to {1}", url, dest);
          return;
        }
        Downloader.DownloadSync(url, dest, (sender, e) =>
        {
          Console.Write("\r");
          Console.Write("{0}% ({1}/{2})", e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
        });
      };

      Builtins["uncompress"] = (c, mode, arg) =>
      {
        string archive = arg.Format("{Archive}");
        string format = arg.Format("{Format}");
        Compression compression;
        if (!Enum.TryParse<Compression>(format, out compression))
          compression = Uncompresser.GetCompressionFromExt(archive);
        string folderToUncompress = arg.Format("{FolderToUncompress}");
        string dest = arg.Format("{UncompressDest}");
        if (c.OnlyPrint)
        {
          Console.WriteLine("Uncompress {0} ({1}) to {2}", archive, folderToUncompress, dest);
          return;
        }
        string uncompressedFolder = Uncompresser.Uncompress(archive, compression);
        string folderToMove = Path.Combine(uncompressedFolder, folderToUncompress);
        Directory.Move(folderToMove, dest);
        if (Directory.Exists(uncompressedFolder))
          Directory.Delete(uncompressedFolder, true);
      };

      Builtins["rm"] = FileSystem.BuiltinDelete;
      Builtins["mkdir"] = FileSystem.BuiltinCreateDirectory;
      Builtins["move"] = FileSystem.BuiltinMove;
      Builtins["popd"] = FileSystem.BuiltinPopWorkingDirectory;
      Builtins["pushd"] = FileSystem.BuiltinPushWorkingDirectory;
      Builtins["cd"] = FileSystem.BuiltinSetWorkingDirectory;
    }

    public void Init()
    {
      IDictionary<string, string> args = new Dictionary<string, string>();
      switch (HostPlatform)
      {
        case Platform.Linux32:
          args["IsLinux"] = "true";
          args["IsUnix"] = "true";
          args["Is32Bits"] = "true";
          break;
        case Platform.Linux64:
          args["IsLinux"] = "true";
          args["IsUnix"] = "true";
          args["Is64Bits"] = "true";
          break;
        case Platform.Mac:
          args["IsUnix"] = "true";
          args["IsMac"] = "true";
          args["Is64Bits"] = "true";
          break;
        case Platform.Win32:
          args["IsWindows"] = "true";
          args["Is32Bits"] = "true";
          break;
        case Platform.Win64:
          args["IsWindows"] = "true";
          args["Is64Bits"] = "true";
          break;
        default:
          throw new NotSupportedException("Unknown platform");
      }

      Arguments = args;

      WorkingDirectoryStack.Push(Environment.CurrentDirectory);
      InitBuiltins();
    }

    public void PushWorkingDirectory(string path)
    {
      WorkingDirectoryStack.Push(path);
      Environment.CurrentDirectory = path;
    }

    public void PopWorkingDirectory()
    {
      if (WorkingDirectoryStack.Count <= 1)
        throw new Exception("No working directory to pop");
      WorkingDirectoryStack.Pop();
    }

    public void SetWorkingDirectory(string path)
    {
      WorkingDirectoryStack.Pop();
      WorkingDirectoryStack.Push(path);
      Environment.CurrentDirectory = path;
    }

    private Stack<string> WorkingDirectoryStack = new Stack<string>();

    private bool ParseBoolArg(Arguments args, string name, bool defaultValue = false)
    {
      string val;
      if (!args.TryGetValue(name, out val))
        return defaultValue;
      return (!string.IsNullOrWhiteSpace(val) && val != "0" && !val.Equals("false", StringComparison.InvariantCultureIgnoreCase));
    }

    public Configuration(Options options)
    {
      Options = options;
      Init();
    }

    public void Execute()
    {
      string SrcDir = Path.GetFullPath(Options.SrcDir.Value);
      string BuildDir = Path.GetFullPath(Options.BuildDir.Value);

      string SrcCfgFile = Path.Combine(SrcDir, "moche.config");
      string BuildCfgFile = Path.Combine(BuildDir, "moche.config");
      
      if (File.Exists(SrcDir))
      {
        SrcCfgFile = SrcDir;
        SrcDir = Path.GetFullPath(Path.GetDirectoryName(SrcDir));
      }

      if (File.Exists(BuildDir))
      {
        BuildCfgFile = BuildDir;
        BuildDir = Path.GetFullPath(Path.GetDirectoryName(BuildDir));
      }

      if (!File.Exists(SrcCfgFile))
        throw new FileNotFoundException(string.Format("Can't find source config file {0}", SrcCfgFile));

      if (!Directory.Exists(BuildDir))
        Directory.CreateDirectory(BuildDir);

      string InitialWorkingDirectory = Environment.CurrentDirectory;

      Config config = SerializerFactory.GetSerializer<Config>().Deserialize(SrcCfgFile);

      Environment.CurrentDirectory = SrcDir;
      string SrcToolsConfigRootPath = Path.GetFullPath(config.ToolsConfigRootPath);
      bool SrcToolsConfigRecursive = config.RecursiveSearch;

      if (File.Exists(BuildCfgFile))
        SerializerFactory.GetSerializer<Config>().Deserialize(BuildCfgFile);

      Environment.CurrentDirectory = BuildDir;
      string BuildToolsConfigRootPath = Path.GetFullPath(config.ToolsConfigRootPath);
      bool BuildToolsConfigRecursive = config.RecursiveSearch;

      if (BuildToolsConfigRootPath == SrcToolsConfigRootPath)
        SrcToolsConfigRecursive |= BuildToolsConfigRecursive;

      ISerializer<Configuration> serializer = SerializerFactory.GetSerializer<Configuration>();
      foreach (string file in Directory.GetFiles(SrcToolsConfigRootPath, "*.moche", SrcToolsConfigRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        serializer.Deserialize(file, this);

      if (SrcToolsConfigRootPath != BuildToolsConfigRootPath && Directory.Exists(BuildToolsConfigRootPath))
        foreach (string file in Directory.GetFiles(BuildToolsConfigRootPath, "*.moche", BuildToolsConfigRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
          serializer.Deserialize(file, this);

      List<ActionConfig> actions = config.ComputeActionOrder(Options.Actions.Value.Any() ? Options.Actions.Value : new List<string>() { "retrieve-tools" });

      AllowedRetrievalType = config.RetrievalType;
      SetWorkingDirectory(Environment.CurrentDirectory);
      RootPath = BuildToolsConfigRootPath;
      foreach (ActionConfig action in actions)
        Execute(action);
    }

    public void Execute(ActionConfig action)
    {
      Arguments["Action"] = action.Name;
      if (action.Name == "retrieve-tools")
      {
        RetrieveMandatoryTools();
      }
      else if (action.Name == "clean-tools")
      {
        Clean();
      }
      foreach (CommandInvocation ci in action.Command)
      {
        ci.Invoke(this, CommandInvocationMode.Custom, Arguments);
      }
    }

    public void Clean()
    {
      foreach (Repo r in Repo.Values)
        r.Clean(this);
    }

    public void RetrieveMandatoryTools()
    {
      foreach (Tool t in Tool.Values)
      {
        if (t.Mandatory)
        {
          t.Retrieve(this);
        }
      }
    }

    private Repo GetToolRepo(string tool)
    {
      Tool t;
      if (!Tool.TryGetValue(tool, out t))
        return null;
      if (string.IsNullOrEmpty(t.Repo))
        return null;
      Repo r;
      if (!Repo.TryGetValue(t.Repo, out r))
        return null;
      return r;
    }

    public readonly Options Options;
    public string RootPath { get; private set; }
    public readonly string BinarySubfolder = "bin";
    public readonly string TempSubfolder = "tmp";
    public readonly string SourceSubfolder = "src";

    public Platform HostPlatform
    {
      get
      {
        switch (Environment.OSVersion.Platform)
        {
          case PlatformID.MacOSX:
            if (!Environment.Is64BitOperatingSystem)
              throw new NotSupportedException("32 bits MacOSX not handled");
            return Platform.Mac;
          case PlatformID.Unix:
            return Environment.Is64BitOperatingSystem ? Platform.Linux64 : Platform.Linux32;
          case PlatformID.Win32NT:
          case PlatformID.Win32S:
          case PlatformID.Win32Windows:
          case PlatformID.WinCE:
            return Environment.Is64BitOperatingSystem ? Platform.Win64 : Platform.Win32;
          case PlatformID.Xbox:
            throw new NotSupportedException("Xbox not handled");
          default:
            throw new NotSupportedException("Unknown platform");
        }
      }
    }
  }
}
