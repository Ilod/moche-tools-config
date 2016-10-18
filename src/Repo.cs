using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Configuration
{
  public class Repo
  {
    public class Version
    {
      public string Name;
      public string VersionNumber;
      public string Branch;
      public string BuildBranch;
      public string BuildVersionNumber;
      public bool Built;
    }
    private static readonly string VersionFileName = "version.txt";

    public string Name;
    public List<RetrievalMethod> Retrieval = new List<RetrievalMethod>();
    public List<CommandInvocation> Build = new List<CommandInvocation>();
    public string BuildBranch;
    public string BuildVersion;
    [ClearSerializedCollectionOnMerge]
    public List<RetrievalMethodType> RetrievalType = new List<RetrievalMethodType>();
    [ClearSerializedCollectionOnMerge]
    public List<string> AllowedRetrieval = new List<string>();
    public string VersionCheckExecutable;
    public string VersionCheckArguments;
    public string VersionCheckRegex;
    public int VersionCheckRegexCaptureIndex = 1;
    public string VersionMin;
    public string VersionMax;
    private bool IsValid;
    private bool IsRetrieving;
    private RetrievalMethod CurrentRetrievalMethod = null;

    private string GetVersionFilePath(Configuration c)
    {
      return Path.Combine(c.RootPath, Name, VersionFileName);
    }

    private Version ParseCurrentRepoVersion(Configuration c)
    {
      string versionFile = GetVersionFilePath(c);
      if (!File.Exists(versionFile))
        return null;
      Version version = SerializerFactory.GetSerializer<Version>().Deserialize(versionFile);
      if (!Retrieval.Any(r => r.Name == version.Name))
        return null;
      return version;
    }

    private RetrievalMethod FullRetrieve(Configuration c)
    {
      if (AllowedRetrieval.Any())
      {
        foreach (string retrievalMethodName in AllowedRetrieval)
        {
          RetrievalMethod r = Retrieval.FirstOrDefault(m => m.Name == retrievalMethodName);
          if (r != null && r.TryRetrieve(c, this))
            return r;
        }
      }
      else if (RetrievalType.Any() || c.AllowedRetrievalType.Any())
      {
        foreach (RetrievalMethodType type in ((RetrievalType.Any() ? RetrievalType : c.AllowedRetrievalType)))
        {
          foreach (RetrievalMethod r in Retrieval.Where(rm => rm.GetRetrievalType() == type))
          {
            if (r.TryRetrieve(c, this))
              return r;
          }
        }
      }
      else
      {
        foreach (RetrievalMethod r in Retrieval)
        {
          if (r.TryRetrieve(c, this))
            return r;
        }
      }
      return null;
    }

    public RetrievalMethod Retrieve(Configuration c)
    {
      if (IsValid)
        return CurrentRetrievalMethod;
      if (IsRetrieving)
        c.Console.WriteLine(LogLevel.Fatal, "Already retrieving {0}, circular dependency?", Name);
      IsRetrieving = true;
      RetrieveInternal(c);
      IsValid = true;
      IsRetrieving = false;
      return CurrentRetrievalMethod;
    }

    public void Clean(Configuration c)
    {
      string path = Path.Combine(c.RootPath, Name);
      if (Directory.Exists(path))
        Directory.Delete(path, true);
    }

    public IDictionary<string, string> GetArguments(Configuration c)
    {
      IDictionary<string, string> args = new Dictionary<string, string>();
      args["BuildPath"] = Path.Combine(c.RootPath, Name, c.TempSubfolder);
      args["SourcePath"] = Path.Combine(c.RootPath, Name, c.SourceSubfolder);
      args["BinaryPath"] = Path.Combine(c.RootPath, Name, c.BinarySubfolder);
      return new Arguments(args, c.GetArguments());
    }

    private void RetrieveInternal(Configuration c)
    {
      Version version = ParseCurrentRepoVersion(c);
      bool updateVersionFile = false;
      if (version == null)
      {
        Clean(c);
        CurrentRetrievalMethod = FullRetrieve(c);
        if (CurrentRetrievalMethod == null)
          return;
        updateVersionFile = true;
      }
      else
      {
        RetrievalMethod retrieval = Retrieval.First(r => r.Name == version.Name);
        if (AllowedRetrieval.Any())
        {
          if (!AllowedRetrieval.Contains(retrieval.Name))
          {
            c.Console.WriteLine(LogLevel.Error, "Retrieval method no longer allowed");
            return;
          }
        }
        else if (RetrievalType.Any())
        {
          if (!RetrievalType.Contains(retrieval.GetRetrievalType()))
          {
            c.Console.WriteLine(LogLevel.Error, "Retrieval method no longer allowed");
            return;
          }
        }
        else if (c.AllowedRetrievalType.Any())
        {
          if (!c.AllowedRetrievalType.Contains(retrieval.GetRetrievalType()))
          {
            c.Console.WriteLine(LogLevel.Error, "Retrieval method no longer allowed");
            return;
          }
        }
        if (version.Branch != retrieval.Branch)
        {
          Clean(c);
          if (!retrieval.TryRetrieve(c, this))
            return;
          updateVersionFile = true;
        }
        else if (version.VersionNumber != retrieval.Version)
        {
          if (retrieval.CanUpdate())
          {
            if (!retrieval.TryUpdate(c, this))
              return;
          }
          else
          {
            Clean(c);
            if (!retrieval.TryRetrieve(c, this))
              return;
          }
          updateVersionFile = true;
        }
        else if (retrieval.ShouldAlwaysUpdate())
        {
          if (!retrieval.TryUpdate(c, this))
            return;
        }
        CurrentRetrievalMethod = retrieval;
      }
      bool builtOnce = version.Built;
      if (updateVersionFile)
      {
        version.Name = CurrentRetrievalMethod.Name;
        version.Branch = CurrentRetrievalMethod.Branch;
        version.VersionNumber = CurrentRetrievalMethod.Version;
        version.Built = false;
        SerializerFactory.GetSerializer<Version>().Serialize(GetVersionFilePath(c), version);
      }
      if (CurrentRetrievalMethod.NeedBuild() && (!version.Built || CurrentRetrievalMethod.ShouldAlwaysUpdate() || version.BuildVersionNumber != BuildVersion || version.BuildBranch != BuildBranch))
      {
        IDictionary<string, string> args = GetArguments(c);
        if (version.BuildBranch != BuildBranch && Directory.Exists(args["BuildPath"]))
          Directory.Delete(args["BuildPath"], true);
        if (!version.Built || version.BuildVersionNumber != BuildVersion || version.BuildBranch != BuildBranch)
          args["Initial"] = "true";

        foreach (CommandInvocation ci in Build)
        {
          if (!ci.Invoke(c, args))
          {
            CurrentRetrievalMethod = null;
            return;
          }
        }
        version.Built = true;
        SerializerFactory.GetSerializer<Version>().Serialize(GetVersionFilePath(c), version);
      }
    }
  }
}
