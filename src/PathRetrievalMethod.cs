using System;

namespace Configuration
{
  public class PathRetrievalMethod : IRetrievalMethod
  {
    public string Path;

    public bool TryRetrieve(Configuration c, Repo r)
    {
      c.Console.WriteLine(LogLevel.Info, "Searching for {0} in path {1}", r.VersionCheckExecutable, Path);
      System.Diagnostics.Process p;
      try
      {
        p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
          FileName = r.VersionCheckExecutable,
          Arguments = r.VersionCheckArguments,
          UseShellExecute = false,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          CreateNoWindow = true
        });
        p.BeginErrorReadLine();
      }
      catch (System.ComponentModel.Win32Exception)
      {
        c.Console.WriteLine(LogLevel.Debug, "{0} not found in path {1}", r.VersionCheckExecutable, Path);
        return false;
      }
      if (!string.IsNullOrEmpty(r.VersionCheckRegex))
      {
        string version = null;
        Version minVersion = null;
        if (!string.IsNullOrEmpty(r.VersionMin))
          minVersion = System.Version.Parse(r.VersionMin);
        Version maxVersion = null;
        if (!string.IsNullOrEmpty(r.VersionMax))
          maxVersion = System.Version.Parse(r.VersionMax);
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(r.VersionCheckRegex);
        while (!p.StandardOutput.EndOfStream)
        {
          string line = p.StandardOutput.ReadLine();
          if (!string.IsNullOrEmpty(version))
            continue;
          System.Text.RegularExpressions.Match match = regex.Match(line);
          if (match.Success && match.Groups.Count >= r.VersionCheckRegexCaptureIndex)
            version = match.Groups[r.VersionCheckRegexCaptureIndex].Value;
        }
        if (string.IsNullOrEmpty(version))
        {
          c.Console.WriteLine(LogLevel.Error, "Version not found in output");
          return false;
        }
        Version v;
        try
        {
          v = System.Version.Parse(version);
        }
        catch (Exception)
        {
          c.Console.WriteLine(LogLevel.Error, "Bad version format {0}", version);
          return false;
        }
        if ((minVersion != null && minVersion > v))
        {
          c.Console.WriteLine(LogLevel.Info, "Version too old, expected at least {0}, got {1}", minVersion, version);
          return false;
        }
        if ((maxVersion != null && maxVersion < v))
        {
          c.Console.WriteLine(LogLevel.Info, "Version too recent, expected at most {0}, got {1}", maxVersion, version);
          return false;
        }
      }
      else
      {
        p.BeginOutputReadLine();
      }
      return true;
    }

    public bool TryUpdate(Configuration c, Repo r)
    {
      return TryRetrieve(c, r);
    }

    public string GetToolPath(Configuration c, Repo r, string executable)
    {
      return string.IsNullOrEmpty(Path) ? executable : System.IO.Path.Combine(Path, executable);
    }

    public bool CanUpdate()
    {
      return true;
    }

    public bool ShouldAlwaysUpdate()
    {
      return true;
    }

    public bool NeedBuild()
    {
      return false;
    }

    public RetrievalMethodType GetRetrievalType()
    {
      return RetrievalMethodType.Path;
    }
  }
}
