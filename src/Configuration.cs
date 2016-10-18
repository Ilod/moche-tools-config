using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public readonly Console Console = new Console();
    [NonSerialized]
    public List<RetrievalMethodType> AllowedRetrievalType = new List<RetrievalMethodType>();
    [DictionaryEmbeddedKey("Name")]
    public Dictionary<string, Tool> Tool = new Dictionary<string, Tool>();
    [DictionaryEmbeddedKey("Name")]
    public Dictionary<string, Command> Command = new Dictionary<string, Command>();
    [DictionaryEmbeddedKey("Name")]
    public Dictionary<string, Repo> Repo = new Dictionary<string, Repo>();
    private IDictionary<string, string> Arguments;

    private delegate bool BuiltinFunction(Configuration c, Arguments args);
    private IDictionary<string, BuiltinFunction> Builtins = new Dictionary<string, BuiltinFunction>();

    public IDictionary<string, string> GetArguments()
    {
      return Arguments;
    }

    public bool CallBuiltin(string builtin, IDictionary<string, string> args)
    {
      return Builtins[builtin](this, new Arguments(args));
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
              { "Overwrite", "true" },
              { "MoveSource", "{Source}" },
              { "Source", null },
              { "MoveDest", "{Dest}" },
              { "Dest", null },
            },
          }
        }
      };
      Command["copy"] = new Command()
      {
        Name = "copy",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "copy",
            Arguments =
            {
              { "IgnoreUnexisting", null },
              { "CreateDest", "true" },
              { "Overwrite", "true" },
              { "CopySource", "{Source}" },
              { "Source", null },
              { "CopyDest", "{Dest}" },
              { "Dest", null },
            },
          }
        }
      };

      Builtins["download"] = (c, arg) =>
      {
        string url = arg.Format("{Url}");
        string dest = arg.Format("{DownloadDest}");
        Console.WriteLine(LogLevel.Info, "Download {0} to {1}", url, dest);
        if (c.OnlyPrint)
          return true;
        return Downloader.DownloadSync(c, url, dest, (sender, e) =>
        {
          Console.Write(LogLevel.Info, "\r");
          Console.Write(LogLevel.Info, "{0}% ({1}/{2})", e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
        });
      };

      Builtins["uncompress"] = (c, arg) =>
      {
        string archive = arg.Format("{Archive}");
        string format = arg.Format("{Format}");
        Compression compression;
        if (!Enum.TryParse<Compression>(format, out compression))
          compression = Uncompresser.GetCompressionFromExt(archive);
        string folderToUncompress = arg.Format("{FolderToUncompress}");
        string dest = arg.Format("{UncompressDest}");
        Console.WriteLine(LogLevel.Trace, "Uncompress {0} ({1}) to {2}", archive, folderToUncompress, dest);
        if (c.OnlyPrint)
          return true;
        string uncompressedFolder = Uncompresser.Uncompress(archive, compression);
        string folderToMove = Path.Combine(uncompressedFolder, folderToUncompress);
        Directory.Move(folderToMove, dest);
        if (Directory.Exists(uncompressedFolder))
          Directory.Delete(uncompressedFolder, true);
        return true;
      };

      Builtins["rm"] = FileSystem.BuiltinDelete;
      Builtins["mkdir"] = FileSystem.BuiltinCreateDirectory;
      Builtins["move"] = FileSystem.BuiltinMove;
      Builtins["copy"] = FileSystem.BuiltinCopy;
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

    public bool PopWorkingDirectory()
    {
      if (WorkingDirectoryStack.Count <= 1)
        return false;
      WorkingDirectoryStack.Pop();
      return true;
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
      OnlyPrint = options.Noop.Value;
      Console.LogLevel = options.Log.Value;
      Console.InteractivityLevel = options.Interactivity.Value;
      Init();
    }

    public void Execute()
    {
      string BuildDir = Options.BuildDir.Value;
      if (string.IsNullOrEmpty(BuildDir))
        BuildDir = Environment.CurrentDirectory;
      else if (Options.FromScrîpt.Value)
        Console.WriteLine(LogLevel.Fatal, "--build is not compatible with --from-script");
      BuildDir = Path.GetFullPath(BuildDir);

      string BuildCfgFile = Path.Combine(BuildDir, "moche.config");

      if (File.Exists(BuildDir))
      {
        BuildCfgFile = BuildDir;
        BuildDir = Path.GetFullPath(Path.GetDirectoryName(BuildDir));
      }

      BuildInfo BuildInfo = new BuildInfo();
      string BuildInfoFile = Path.Combine(BuildDir, "moche.build");
      if (File.Exists(BuildInfoFile))
        SerializerFactory.GetSerializer<BuildInfo>().Deserialize(BuildInfoFile, BuildInfo);
      if (!string.IsNullOrEmpty(Options.SrcDir.Value))
      {
        if (Options.FromScrîpt.Value)
          Console.WriteLine(LogLevel.Fatal, "--src is not compatible with --from-script");
        BuildInfo.Source = Path.GetFullPath(Options.SrcDir.Value);
      }
      if (string.IsNullOrEmpty(BuildInfo.Source))
        BuildInfo.Source = Environment.CurrentDirectory;

      string SrcCfgFile = Path.Combine(BuildInfo.Source, "moche.config");
      
      if (File.Exists(BuildInfo.Source))
      {
        SrcCfgFile = BuildInfo.Source;
        BuildInfo.Source = Path.GetFullPath(Path.GetDirectoryName(BuildInfo.Source));
      }

      if (!File.Exists(SrcCfgFile))
        throw new FileNotFoundException(string.Format("Can't find source config file {0}", SrcCfgFile));

      if (!Directory.Exists(BuildDir))
        Directory.CreateDirectory(BuildDir);
      if (!Options.FromScrîpt.Value)
      {
        SerializerFactory.GetSerializer<BuildInfo>().Serialize(BuildInfoFile, BuildInfo);
        string exePath = GetType().Assembly.Location;
        switch (HostPlatform)
        {
          case Platform.Win32:
          case Platform.Win64:
            File.WriteAllText(Path.Combine(BuildDir, string.Format("{0}.bat", Path.GetFileNameWithoutExtension(exePath))), string.Format("{0} --from-script %*", exePath));
            break;
          case Platform.Linux32:
          case Platform.Linux64:
          case Platform.Mac:
            string scriptFile = Path.Combine(BuildDir, string.Format("{0}.sh", Path.GetFileNameWithoutExtension(exePath)));
            File.WriteAllLines(scriptFile,
              new string[]
              {
                "#!/bin/sh",
                string.Format("{0} --from-script \"$@\"", exePath)
              });
            File.SetAttributes(scriptFile, (FileAttributes)((int)File.GetAttributes(scriptFile) | 0x8000000));
            break;
        }
      }

      string InitialWorkingDirectory = Environment.CurrentDirectory;

      Config config = SerializerFactory.GetSerializer<Config>().Deserialize(SrcCfgFile);

      Environment.CurrentDirectory = BuildInfo.Source;
      string SrcToolsConfigRootPath = Path.GetFullPath(config.ToolsConfigRootPath);
      bool SrcToolsConfigRecursive = config.RecursiveSearch;

      if (File.Exists(BuildCfgFile))
        SerializerFactory.GetSerializer<Config>().Deserialize(BuildCfgFile);

      Environment.CurrentDirectory = BuildDir;
      string BuildToolsConfigRootPath = Path.GetFullPath(config.ToolsConfigRootPath);
      bool BuildToolsConfigRecursive = config.RecursiveSearch;

      if (BuildToolsConfigRootPath == SrcToolsConfigRootPath)
      {
        Console.WriteLine(LogLevel.Error, "Trying to build in-tree!");
        if (!Console.ReadBool(InteractivityLevel.Fatal, false, "Are you sur to build in-tree?"))
        {
          Console.WriteLine(LogLevel.Fatal, "Exiting because in-tree build is not supported");
        }
        SrcToolsConfigRecursive |= BuildToolsConfigRecursive;
      }

      ISerializer<Configuration> serializer = SerializerFactory.GetSerializer<Configuration>();
      foreach (string file in Directory.GetFiles(SrcToolsConfigRootPath, "*.moche", SrcToolsConfigRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
        serializer.Deserialize(file, this);

      if (SrcToolsConfigRootPath != BuildToolsConfigRootPath && Directory.Exists(BuildToolsConfigRootPath))
        foreach (string file in Directory.GetFiles(BuildToolsConfigRootPath, "*.moche", BuildToolsConfigRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
          serializer.Deserialize(file, this);

      List<string> actionNames = Options.Actions.Value.Any() ? Options.Actions.Value : new List<string>() { "retrieve-tools" };
      Console.WriteLine(LogLevel.Info, "Action Requested: {0}", string.Join(", ", actionNames));
      List<ActionConfig> actions = config.ComputeActionOrder(actionNames);
      Console.WriteLine(LogLevel.Debug, "Actions to execute: {0}", string.Join(", ", actions.Select(a => a.Name)));

      AllowedRetrievalType = config.RetrievalType;
      SetWorkingDirectory(Environment.CurrentDirectory);
      RootPath = BuildToolsConfigRootPath;
      foreach (ActionConfig action in actions)
        if (!Execute(action))
          Console.WriteLine(LogLevel.Fatal, "Failed to execute action {0}", action.Name);

      Console.WriteLine(LogLevel.Info, "Done!");
      Console.WaitExitInput("Press any key to exit...", 0);
    }

    public bool Execute(ActionConfig action)
    {
      Console.WriteLine(LogLevel.Info, "Execute action {0}", action.Name);
      Arguments["Action"] = action.Name;
      if (action.Name == "retrieve-tools")
      {
        if (!RetrieveMandatoryTools())
          return false;
      }
      else if (action.Name == "clean-tools")
      {
        Clean();
      }
      foreach (CommandInvocation ci in action.Command)
        if (!ci.Invoke(this, Arguments))
          return false;
      return true;
    }

    public void Clean()
    {
      foreach (Repo r in Repo.Values)
        r.Clean(this);
    }

    public bool RetrieveMandatoryTools()
    {
      foreach (Tool t in Tool.Values)
        if (t.Mandatory)
          if (!t.Retrieve(this))
            return false;
      return true;
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
