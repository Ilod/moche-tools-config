using System;

namespace Configuration
{
  public class RemoteFile
  {
    public string Url;
    public string FolderToExtract;
  }

  public interface IRetrievalMethod
  {
    bool TryRetrieve(Configuration c, Repo r);
    string GetToolPath(Configuration c, Repo r, string executable);
    bool TryUpdate(Configuration c, Repo r);
    bool CanUpdate();
    bool ShouldAlwaysUpdate();
    bool NeedBuild();
    RetrievalMethodType GetRetrievalType();
  }

  public enum RetrievalMethodType
  {
    Path,
    Binary,
    Source,
  }

  public class RetrievalMethod : IRetrievalMethod
  {
    public string Name;
    public string Version;
    public string Branch;
    public string Condition;
    public PathRetrievalMethod Path;
    public BinaryRetrievalMethod Binary;
    public SourceRetrievalMethod Source;

    private IRetrievalMethod Method
    {
      get
      {
        if (Path != null)
          return Path;
        if (Binary != null)
          return Binary;
        if (Source != null)
          return Source;
        return null;
      }
    }

    private bool CheckCondition(Configuration c, Repo r)
    {
      if (string.IsNullOrEmpty(Condition))
        return true;
      bool negative = false;
      string conditionVariable = Condition;
      const string negativeStart = "not ";
      if (Condition.StartsWith(negativeStart, StringComparison.InvariantCultureIgnoreCase))
      {
        negative = true;
        conditionVariable = Condition.Substring(negativeStart.Length);
      }
      return (new Arguments(r.GetArguments(c)).ParseBoolArg(conditionVariable) == negative);
    }

    public bool TryRetrieve(Configuration c, Repo r)
    {
      return CheckCondition(c, r) && Method.TryRetrieve(c, r);
    }

    public bool CanUpdate()
    {
      return Method.CanUpdate();
    }

    public string GetToolPath(Configuration c, Repo r, string executable)
    {
      return Method.GetToolPath(c, r, executable);
    }

    public bool ShouldAlwaysUpdate()
    {
      return Method.ShouldAlwaysUpdate();
    }

    public bool TryUpdate(Configuration c, Repo r)
    {
      return CheckCondition(c, r) && Method.TryUpdate(c, r);
    }

    public bool NeedBuild()
    {
      return Method.NeedBuild();
    }

    public RetrievalMethodType GetRetrievalType()
    {
      return Method.GetRetrievalType();
    }
  }
}
