namespace Configuration
{
  public class Tool
  {
    public string Name;
    public string Executable;
    public string Repo;
    public bool Mandatory;

    private bool IsValid = false;
    private string ExecutablePath;
    public void Retrieve(Configuration c)
    {
      if (IsValid)
        return;
      Repo repo = c.Repo[Repo];
      ExecutablePath = repo.Retrieve(c).GetToolPath(c, repo, Executable);
      IsValid = true;
    }

    public string GetExecutablePath(Configuration c)
    {
      c.Repo[Repo].Retrieve(c);
      return ExecutablePath;
    }
  }
}
