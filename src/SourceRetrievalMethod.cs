﻿using System.Collections.Generic;
using System.IO;

namespace Configuration
{
  public class SourceRetrievalMethod : IRetrievalMethod
  {
    public bool Updatable;
    public bool AlwaysUpdate;
    public List<CommandInvocation> Command = new List<CommandInvocation>();
    public string BuildBinarySubFolder = "";

    public string GetToolPath(Configuration c, Repo r, string executable)
    {
      return Path.Combine(c.RootPath, r.Name, c.BinarySubfolder, BuildBinarySubFolder, executable);
    }

    public bool TryRetrieve(Configuration c, Repo r)
    {
      Arguments args = new Arguments(new Dictionary<string, string>() { { "Initial", "true" } }, r.GetArguments(c));
      foreach (CommandInvocation ci in Command)
        if (!ci.Invoke(c, args))
          return false;
      return true;
    }

    public bool TryUpdate(Configuration c, Repo r)
    {
      IDictionary<string, string> args = r.GetArguments(c);
      foreach (CommandInvocation ci in Command)
        if (!ci.Invoke(c, args))
          return false;
      return true;
    }

    public bool CanUpdate()
    {
      return Updatable;
    }

    public bool ShouldAlwaysUpdate()
    {
      return AlwaysUpdate;
    }

    public bool NeedBuild()
    {
      return true;
    }

    public RetrievalMethodType GetRetrievalType()
    {
      return RetrievalMethodType.Source;
    }
  }

}
