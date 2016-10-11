using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Configuration
{
  public enum CommandInvocationMode
  {
    Retrieve,
    Update,
    Build,
    Custom
  }

  public class CommandInvocation
  {
    public string Command;
    public Dictionary<string, string> Arguments = new Dictionary<string, string>();
    public void Invoke(Configuration conf, CommandInvocationMode mode, IDictionary<string, string> args)
    {
      Command c = conf.GetCommand(Command);
      c.InvokeCommand(conf, mode, new Arguments(Arguments, args));
    }
  }




  public class ExecutableInvocation
  {
    public string CommandLineExecutable = "{Executable}";
    public string CommandLineArguments = string.Empty;
    public Dictionary<string, string> Arguments = new Dictionary<string, string>();
    public Dictionary<CommandInvocationMode, Dictionary<string, string>> ModeArguments = new Dictionary<CommandInvocationMode, Dictionary<string, string>>();
    public string BuiltIn;

    public void Invoke(Configuration c, CommandInvocationMode mode, IDictionary<string, string> args)
    {
      Arguments argFormat = new Arguments(args, ModeArguments.ContainsKey(mode) ? ModeArguments[mode] : new Dictionary<string, string>(), Arguments);
      if (!string.IsNullOrEmpty(BuiltIn))
      {
        c.CallBuiltin(BuiltIn, mode, argFormat);
      }
      else
      {
        Console.WriteLine("{0}> {1} {2}", Environment.CurrentDirectory, argFormat.Format(CommandLineExecutable), argFormat.Format(CommandLineArguments));
        if (c.OnlyPrint)
        {
          return;
        }
        Process p = new Process();
        p.StartInfo = new ProcessStartInfo()
        {
          FileName = argFormat.Format(CommandLineExecutable),
          Arguments = argFormat.Format(CommandLineArguments),
          UseShellExecute = false,
        };
        p.Start();
        p.WaitForExit();
        if (p.ExitCode != 0)
          throw new Exception(string.Format("Process {0} exit code {1}", argFormat.Format(CommandLineExecutable), p.ExitCode));
      }
    }
  }

  public class Command
  {
    public string Name;
    public string Tool;
    public List<ExecutableInvocation> Invoke;

    private bool IsValid = false;

    public void Retrieve(Configuration c)
    {
      if (IsValid)
        return;
      if (!string.IsNullOrEmpty(Tool))
        c.Tool[Tool].Retrieve(c);
      IsValid = true;
    }

    public void InvokeCommand(Configuration c, CommandInvocationMode mode, IDictionary<string, string> args)
    {
      Retrieve(c);
      Dictionary<string, string> additionnalArgs = new Dictionary<string, string>();
      if (!string.IsNullOrEmpty(Tool))
      {
        additionnalArgs["Executable"] = c.GetTool(Tool).GetExecutablePath(c);
      }
      foreach (ExecutableInvocation invocation in Invoke)
      {
        invocation.Invoke(c, mode, new Arguments(additionnalArgs, args));
      }
    }
  }
}
