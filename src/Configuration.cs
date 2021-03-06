﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Configuration
{
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

    private delegate string FormatFunction(Configuration c, string args);
    private IDictionary<string, FormatFunction> Formats = new Dictionary<string, FormatFunction>();

    public IDictionary<string, string> GetArguments()
    {
      return new Arguments(Options.Args.Value, Arguments);
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

    public string ExecuteFormatFunction(string func, string args)
    {
      return Formats[func](this, args);
    }

    public void InitBuiltins()
    {
      Formats["ToolPath"] = (c, args) =>
      {
        return Tool[args.Trim()].GetExecutablePath(c);
      };
      Formats["ToolFolder"] = (c, args) =>
      {
        return Path.GetDirectoryName(Tool[args.Trim()].GetExecutablePath(c));
      };

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
              { "CreateDest", "true" },
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
              { "CreateDest", "true" },
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
              { "CreateDest", "true" },
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
              { "CreateDest", "true" },
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
              { "IgnoreUnexisting", "false" },
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
              { "IgnoreUnexisting", "false" },
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
      Command["retrieve"] = new Command()
      {
        Name = "retrieve",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "retrieve",
            Arguments =
            {
              { "Repo", null },
              { "Retrieval", string.Empty },
              { "RetrievalType", string.Empty },
            }
          }
        }
      };
      Command["retrieve-source"] = new Command()
      {
        Name = "retrieve-source",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "retrieve-src",
            Arguments =
            {
              { "Repo", null },
              { "Retrieval", string.Empty },
            }
          }
        }
      };
      Command["log"] = new Command()
      {
        Name = "log",
        Invoke = new List<ExecutableInvocation>()
        {
          new ExecutableInvocation()
          {
            BuiltIn = "log",
            Arguments =
            {
              { "LogLevel", "Info" },
              { "LogText", null },
            }
          }
        }
      };

      Builtins["download"] = (c, arg) =>
      {
        string url = arg.Format(c, "{Url}");
        string dest = arg.Format(c, "{DownloadDest}");
        if (arg.ParseBoolArg(c, "CreateDest"))
          Directory.CreateDirectory(Path.GetDirectoryName(dest));
        Console.WriteLine(LogLevel.Info, "Download {0} to {1}", url, dest);
        if (c.OnlyPrint)
          return true;
        return Downloader.DownloadSync(c, url, dest, (sender, e) =>
        {
          Console.Write(LogLevel.Info, "\r{0}% ({1}/{2})", e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
        });
      };

      Builtins["uncompress"] = (c, arg) =>
      {
        string archive = arg.Format(c, "{Archive}");
        string format = arg.Format(c, "{Format}");
        Compression compression;
        if (!Enum.TryParse<Compression>(format, out compression))
          compression = Uncompresser.GetCompressionFromExt(archive);
        string folderToUncompress = arg.Format(c, "{FolderToUncompress}");
        string dest = arg.Format(c, "{UncompressDest}");
        if (arg.ParseBoolArg(c, "CreateDest"))
          Directory.CreateDirectory(dest);
        Console.WriteLine(LogLevel.Trace, "Uncompress {0} ({1}) to {2}", archive, folderToUncompress, dest);
        if (c.OnlyPrint)
          return true;
        string uncompressedFolder = Uncompresser.Uncompress(archive, compression);
        string folderToMove = Path.Combine(uncompressedFolder, folderToUncompress);
        foreach (string d in Directory.GetDirectories(folderToMove))
          Directory.Move(d, Path.Combine(dest, Path.GetFileName(d)));
        foreach (string f in Directory.GetFiles(folderToMove))
          System.IO.File.Move(f, Path.Combine(dest, Path.GetFileName(f)));
        if (Directory.Exists(uncompressedFolder))
          Directory.Delete(uncompressedFolder, true);
        return true;
      };

      Builtins["retrieve"] = (c, arg) =>
      {
        string repo = arg.Format(c, "{Repo}");
        string retrieval = arg.Format(c, "{Retrieval}");
        string retrievalType = arg.Format(c, "{RetrievalType}");
        RetrievalRestriction restriction = null;
        if (!string.IsNullOrEmpty(retrieval) || !string.IsNullOrEmpty(retrievalType))
        {
          restriction = new RetrievalRestriction();
          if (!string.IsNullOrEmpty(retrieval))
            restriction.AllowedRetrieval.Add(retrieval);
          if (!string.IsNullOrEmpty(retrievalType))
          {
            RetrievalMethodType type;
            if (!Enum.TryParse(retrievalType, out type))
            {
              c.Console.WriteLine(LogLevel.Error, "{0} is not a valid retrieval method type", retrievalType);
              return false;
            }
            restriction.RetrievalType.Add(type);
          }
        }
        Repo r;
        if (!c.Repo.TryGetValue(repo, out r))
        {
          c.Console.WriteLine(LogLevel.Error, "Repo {0} not found", repo);
          return false;
        }
        return r.Retrieve(c, restriction) != null;
      };

      Builtins["retrieve-src"] = (c, arg) =>
      {
        string repo = arg.Format(c, "{Repo}");
        string retrieval = arg.Format(c, "{Retrieval}");
        RetrievalRestriction restriction = new RetrievalRestriction();
        restriction.RetrievalType.Add(RetrievalMethodType.Source);
        restriction.NoBuild = true;
        if (!string.IsNullOrEmpty(retrieval))
          restriction.AllowedRetrieval.Add(retrieval);
        Repo r;
        if (!c.Repo.TryGetValue(repo, out r))
        {
          c.Console.WriteLine(LogLevel.Error, "Repo {0} not found", repo);
          return false;
        }
        return r.Retrieve(c, restriction) != null;
      };

      Builtins["log"] = (c, arg) =>
      {
        string levelStr = arg.Format(c, "{LogLevel}");
        string text = arg.Format(c, "{LogText}");
        LogLevel level;
        if (!Enum.TryParse(levelStr, out level))
        {
          c.Console.WriteLine(LogLevel.Error, "{0} is not a valid log level", levelStr);
          return false;
        }
        c.Console.WriteLine(level, text);
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
      switch (Platform.CurrentOS)
      {
        case OS.Unix:
          args["IsLinux"] = "true";
          args["IsUnix"] = "true";
          break;
        case OS.Mac:
          args["IsMac"] = "true";
          args["IsUnix"] = "true";
          break;
        case OS.Windows:
          args["IsWindows"] = "true";
          break;
      }
      switch (Platform.CurrentArch)
      {
        case Arch.x86:
          args["Is32Bits"] = "true";
          args["IsIntel"] = "true";
          break;
        case Arch.x64:
          args["Is64Bits"] = "true";
          args["IsIntel"] = "true";
          break;
        case Arch.ARM:
          args["Is32Bits"] = "true";
          args["IsARM"] = "true";
          break;
        case Arch.ARM64:
          args["Is64Bits"] = "true";
          args["IsARM"] = "true";
          break;
      }
      args["Arch"] = Platform.CurrentArch.ToString();
      args["OS"] = Platform.CurrentOS.ToString();
      args["Platform"] = Platform.CurrentPlatform.ToString();
      args["BinarySubFolder"] = BinarySubfolder;
      args["TempSubFolder"] = TempSubfolder;
      args["SourceSubFolder"] = SourceSubfolder;

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
      Console.IsOwnerOfConsole = options.FromScript.Value || (System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle.ToInt64() != 0);
      Init();
    }

    public bool WriteFileIfDifferent(string path, string[] lines)
    {
      if (File.Exists(path))
      {
        string[] srcLines = File.ReadAllLines(path);
        if (lines.Length == srcLines.Length && lines.SequenceEqual(srcLines))
          return false;
      }
      File.WriteAllLines(path, lines);
      return true;
    }

    public void Execute()
    {
      string BuildDir = Options.BuildDir.Value;
      if (string.IsNullOrEmpty(BuildDir))
        BuildDir = Environment.CurrentDirectory;
      else if (Options.FromScript.Value)
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
        if (Options.FromScript.Value)
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
      if (!Options.FromScript.Value)
      {
        SerializerFactory.GetSerializer<BuildInfo>().Serialize(BuildInfoFile, BuildInfo);
        string exePath = GetType().Assembly.Location;
        switch (Platform.CurrentOS)
        {
          case OS.Windows:
            WriteFileIfDifferent(Path.Combine(BuildDir, string.Format("{0}.bat", Path.GetFileNameWithoutExtension(exePath))), new string[] { string.Format("\"{0}\" --from-script %*", exePath) });
            break;
          case OS.Unix:
          case OS.Mac:
            string scriptFile = Path.Combine(BuildDir, string.Format("{0}.sh", Path.GetFileNameWithoutExtension(exePath)));
            if (WriteFileIfDifferent(scriptFile, new string[]
              {
                "#!/bin/sh",
                string.Format("mono \"{0}\" --from-script \"$@\"", exePath)
              }))
            {
              File.SetAttributes(scriptFile, (FileAttributes)((uint)File.GetAttributes(scriptFile) | 0x80000000));
            }
            break;
        }
      }

      string InitialWorkingDirectory = Environment.CurrentDirectory;

      Config config = SerializerFactory.GetSerializer<Config>().Deserialize(SrcCfgFile);

      Environment.CurrentDirectory = BuildInfo.Source;
      string SrcToolsConfigRootPath = Path.GetFullPath(string.IsNullOrEmpty(config.ToolsConfigRootPath) ? "." : config.ToolsConfigRootPath);
      bool SrcToolsConfigRecursive = config.RecursiveSearch;

      if (File.Exists(BuildCfgFile))
        SerializerFactory.GetSerializer<Config>().Deserialize(BuildCfgFile);

      Environment.CurrentDirectory = BuildDir;
      string BuildToolsConfigRootPath = Path.GetFullPath(string.IsNullOrEmpty(config.ToolsConfigRootPath) ? "." : config.ToolsConfigRootPath);
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
      Arguments["RootPath"] = RootPath;
      foreach (ActionConfig action in actions)
        if (!Execute(action))
          Console.WriteLine(LogLevel.Fatal, "Failed to execute action {0}", action.Name);

      Console.WriteLine(LogLevel.MetaInfo, "Done!");
      Console.WaitExitInput("Press any key to exit...", 0);
    }

    public bool Execute(ActionConfig action)
    {
      Console.StartMeta("Execute action {0}", action.Name);
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
        if (!ci.Invoke(this, GetArguments()))
          return false;
      Console.EndMeta("Action {0} done", action.Name);
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

    public string ExecutableExtension
    {
      get
      {
        switch (Environment.OSVersion.Platform)
        {
          case PlatformID.MacOSX:
          case PlatformID.Unix:
            return "";
          case PlatformID.Win32NT:
          case PlatformID.Win32S:
          case PlatformID.Win32Windows:
          case PlatformID.WinCE:
          case PlatformID.Xbox:
            return ".exe";
          default:
            throw new NotSupportedException("Unknown platform");
        }
      }
    }
  }
}
