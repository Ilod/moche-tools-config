﻿namespace Configuration
{
  public class Tool
  {
    public string Name;
    public string Executable;
    public string Repo;
    public bool Mandatory;

    private bool IsValid = false;
    private string ExecutablePath;
    public bool Retrieve(Configuration c)
    {
      if (IsValid)
        return true;
      c.Console.StartMeta("Retrieve tool {0}...", Name);
      Repo repo = c.Repo[Repo];
      RetrievalMethod method = repo.Retrieve(c);
      if (method == null)
        return false;
      ExecutablePath = method.GetToolPath(c, repo, Executable);
      IsValid = true;
      c.Console.EndMeta("Tool {0} retrieved", Name);
      return true;
    }

    public string GetExecutablePath(Configuration c)
    {
      c.Repo[Repo].Retrieve(c);
      return ExecutablePath;
    }
  }
}
