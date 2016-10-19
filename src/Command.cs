using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Configuration
{
  public class CommandInvocation
  {
    public string Command;
    public string Condition;
    public Dictionary<string, string> Arguments = new Dictionary<string, string>();
    public bool Invoke(Configuration c, IDictionary<string, string> args)
    {
      Arguments argFormat = new Arguments(Arguments, args);
      if (!string.IsNullOrEmpty(Condition))
      {
        bool negative = false;
        string conditionVariable = Condition;
        const string negativeStart = "not ";
        if (Condition.StartsWith(negativeStart, StringComparison.InvariantCultureIgnoreCase))
        {
          negative = true;
          conditionVariable = Condition.Substring(negativeStart.Length);
        }
        if (argFormat.ParseBoolArg(c, conditionVariable) == negative)
          return true;
      }
      return c.GetCommand(Command).InvokeCommand(c, argFormat);
    }
  }

  public class ExecutableInvocation
  {
    public string CommandLineExecutable = "{Executable}";
    public string CommandLineArguments = string.Empty;
    public string Condition;
    public Dictionary<string, string> Arguments = new Dictionary<string, string>();
    public string BuiltIn;

    public bool Invoke(Configuration c, IDictionary<string, string> args)
    {
      Arguments argFormat = new Arguments(args, Arguments);
      if (!string.IsNullOrEmpty(Condition))
      {
        bool negative = false;
        string conditionVariable = Condition;
        const string negativeStart = "not ";
        if (Condition.StartsWith(negativeStart, StringComparison.InvariantCultureIgnoreCase))
        {
          negative = true;
          conditionVariable = Condition.Substring(negativeStart.Length);
        }
        if (argFormat.ParseBoolArg(c, conditionVariable) == negative)
          return true;
      }
      if (!string.IsNullOrEmpty(BuiltIn))
      {
        return c.CallBuiltin(BuiltIn, argFormat);
      }
      else
      {
        c.Console.WriteLine(LogLevel.Debug, "{0}> {1} {2}", Environment.CurrentDirectory, argFormat.Format(c, CommandLineExecutable), argFormat.Format(c, CommandLineArguments));
        if (c.OnlyPrint)
          return true;
        Process p = new Process();
        p.StartInfo = new ProcessStartInfo()
        {
          FileName = argFormat.Format(c, CommandLineExecutable),
          Arguments = argFormat.Format(c, CommandLineArguments),
          UseShellExecute = false,
          RedirectStandardOutput = c.Options.HideExternalOutput.Value,
          RedirectStandardError = c.Options.HideExternalError.Value,
        };
        p.Start();
        if (c.Options.HideExternalOutput.Value)
          p.BeginOutputReadLine();
        if (c.Options.HideExternalError.Value)
          p.BeginErrorReadLine();
        p.WaitForExit();
        if (p.ExitCode != 0) {
          c.Console.WriteLine(LogLevel.Error, "Process {0} exit code {1}", argFormat.Format(c, CommandLineExecutable), p.ExitCode);
          return false;
        }
        return true;
      }
    }
  }

  public class Command
  {
    public string Name;
    public string Tool;
    public List<ExecutableInvocation> Invoke;

    private bool IsValid = false;

    public bool Retrieve(Configuration c)
    {
      if (IsValid)
        return true;
      bool found = true;
      if (!string.IsNullOrEmpty(Tool))
        found = c.Tool[Tool].Retrieve(c);
      IsValid = found;
      return IsValid;
    }

    public bool InvokeCommand(Configuration c, IDictionary<string, string> args)
    {
      if (!Retrieve(c))
        return false;
      Dictionary<string, string> additionnalArgs = new Dictionary<string, string>();
      if (!string.IsNullOrEmpty(Tool))
        additionnalArgs["Executable"] = c.GetTool(Tool).GetExecutablePath(c);
      foreach (ExecutableInvocation invocation in Invoke)
        if (!invocation.Invoke(c, new Arguments(additionnalArgs, args)))
          return false;
      return true;
    }
  }
}
