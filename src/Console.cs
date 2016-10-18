using System.Collections.Generic;

namespace Configuration
{
  public enum LogLevel
  {
    None,
    Fatal,
    Error,
    Warning,
    Info,
    Debug,
    Trace,
  }

  public enum InteractivityLevel
  {
    None,
    Exit, // Normal application exit
    Fatal, // Unrecoverable error
    Error, // Recoverable error
    Choice,
    Confirm,
  }

  public class Console
  {
    public LogLevel LogLevel = LogLevel.Info;
    public InteractivityLevel InteractivityLevel = InteractivityLevel.Error;

    public enum ReadResult
    {
      Ok,
      NonInteractive,
      NoChoice,
      InvalidChoice,
    }

    public void WaitInput(InteractivityLevel level, string text)
    {
      if (level > InteractivityLevel)
        return;
      System.Console.Write(text);
      System.Console.ReadKey();
    }
    public string ReadLine(InteractivityLevel level, string text)
    {
      string res;
      TryReadLine(level, out res, text);
      return res;
    }
    public ReadResult TryReadLine(InteractivityLevel level, out string input, string text)
    {
      input = string.Empty;
      if (level > InteractivityLevel)
        return ReadResult.NonInteractive;
      System.Console.Write(text);
      input = System.Console.ReadLine();
      return ReadResult.Ok;
    }
    public ReadResult TryReadBool(InteractivityLevel level, out bool input, string text)
    {
      string res;
      input = false;
      if (TryReadLine(level, out res, text) == ReadResult.NonInteractive)
        return ReadResult.NonInteractive;
      if (string.IsNullOrEmpty(res))
        return ReadResult.NoChoice;
      if (res == "n" || res == "N")
        return ReadResult.Ok;
      if (res != "y" && res != "Y")
        return ReadResult.InvalidChoice;
      input = true;
      return ReadResult.Ok;
    }
    public bool ReadBool(InteractivityLevel level, bool defaultValue, string text)
    {
      bool b;
      if (TryReadBool(level, out b, string.Format("{0} [{1}]", text, defaultValue ? "Y/n" : "N/y")) == ReadResult.Ok)
        return b;
      return defaultValue;
    }
    public T Read<T>(InteractivityLevel level, IEnumerable<T> values, bool caseSensitive, T defaultValue, string text)
    {
      T res;
      if (TryRead(level, values, caseSensitive, out res, text) == ReadResult.Ok)
        return res;
      return defaultValue;
    }
    public ReadResult TryRead<T>(InteractivityLevel level, IEnumerable<T> values, bool caseSensitive, out T input, string text)
    {
      string s;
      input = default(T);
      ReadResult r = TryReadLine(level, out s, text);
      if (r != ReadResult.Ok)
        return r;
      if (string.IsNullOrEmpty(s))
        return ReadResult.NoChoice;
      foreach (T value in values)
      {
        if (value.ToString().Equals(s, caseSensitive ? System.StringComparison.InvariantCulture : System.StringComparison.InvariantCultureIgnoreCase))
        {
          input = value;
          return ReadResult.Ok;
        }
      }
      return ReadResult.InvalidChoice;
    }

    public void WaitExitInput(string text, int exitCode)
    {
      WaitInput((System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle.ToInt64() == 0) ? InteractivityLevel.Confirm : InteractivityLevel.Exit, text);
      System.Environment.Exit(exitCode);
    }

    public void Write(LogLevel level, string text)
    {
      if (level > LogLevel)
        return;
      if (level == LogLevel.Warning)
        System.Console.ForegroundColor = System.ConsoleColor.Yellow;
      else if (level == LogLevel.Error || level == LogLevel.Fatal)
        System.Console.ForegroundColor = System.ConsoleColor.Red;
      ((level == LogLevel.Error || level == LogLevel.Fatal) ? System.Console.Error : System.Console.Out).Write(text);
      System.Console.ResetColor();
      if (level == LogLevel.Fatal)
        WaitExitInput("", 1);
    }
    public void Write<T>(LogLevel level, T obj) { Write(level, obj.ToString()); }
    public void Write(LogLevel level, string format, object arg0) { Write(level, string.Format(format, arg0)); }
    public void Write(LogLevel level, string format, object arg0, object arg1) { Write(level, string.Format(format, arg0, arg1)); }
    public void Write(LogLevel level, string format, object arg0, object arg1, object arg2) { Write(level, string.Format(format, arg0, arg1, arg2)); }
    public void Write(LogLevel level, string format, params object[] args) { Write(level, string.Format(format, args)); }
    public void WriteLine(LogLevel level, string text) { Write(level, text + "\n"); }
    public void WriteLine(LogLevel level, string format, object arg0) { WriteLine(level, string.Format(format, arg0)); }
    public void WriteLine(LogLevel level, string format, object arg0, object arg1) { WriteLine(level, string.Format(format, arg0, arg1)); }
    public void WriteLine(LogLevel level, string format, object arg0, object arg1, object arg2) { WriteLine(level, string.Format(format, arg0, arg1, arg2)); }
    public void WriteLine(LogLevel level, string format, params object[] args) { WriteLine(level, string.Format(format, args)); }
  }
}
