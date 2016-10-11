using System;

namespace Configuration
{
  public class PathRetrievalMethod : IRetrievalMethod
  {
    public string Path;

    public bool TryRetrieve(Configuration c, Repo r)
    {
      if (c.OnlyPrint)
      {
        Console.WriteLine("Searching for {0} in path {1}", r.VersionCheckExecutable, Path);
      }
      try
      {
        System.Diagnostics.Process p;
        try
        {
          p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
          {
            FileName = r.VersionCheckExecutable,
            Arguments = r.VersionCheckArguments,
            UseShellExecute = false,
            RedirectStandardOutput = !string.IsNullOrEmpty(r.VersionCheckRegex),
            CreateNoWindow = true
          });
        }
        catch (System.ComponentModel.Win32Exception)
        {
          throw new Exception(string.Format("{0} not found in path {1}", r.VersionCheckExecutable, Path));
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
            throw new Exception("Version not found in output");
          Version v = System.Version.Parse(version);
          if ((minVersion != null && minVersion > v))
            throw new Exception(string.Format("Version too old, expected at least {0}, got {1}", minVersion, version));
          if ((maxVersion != null && maxVersion < v))
            throw new Exception(string.Format("Version too recent, expected at most {0}, got {1}", maxVersion, version));
        }
        return true;
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
        return false;
      }
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
