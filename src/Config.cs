using System;
using System.Collections.Generic;
using System.Linq;

namespace Configuration
{
  public class Config
  {
    public string ToolsConfigRootPath;
    public bool RecursiveSearch;
    [ClearSerializedCollectionOnMerge]
    public List<RetrievalMethodType> RetrievalType = new List<RetrievalMethodType>();
    [DictionaryEmbeddedKey("Name")]
    public Dictionary<string, ActionConfig> Action = new Dictionary<string, ActionConfig>() {
      { "clean-tools", new ActionConfig() { Name = "clean-tools" } },
      { "retrieve-tools", new ActionConfig() { Name = "retrieve-tools" } },
    };

    public List<ActionConfig> ComputeActionOrder(IList<string> actions)
    {
      IList<string> orderedActions = new List<string>();
      IDictionary<string, bool> visited = new Dictionary<string, bool>();
      foreach (string action in actions.Reverse())
        VisitDependencies(action, visited, orderedActions);
      ISet<string> insertedActions = new SortedSet<string>();
      IList<ActionConfig> uniqueOrderedActions = new List<ActionConfig>();
      for (int i = orderedActions.Count - 1; i >= 0; --i)
        if (insertedActions.Add(orderedActions[i]))
          uniqueOrderedActions.Add(Action[orderedActions[i]]);
      return uniqueOrderedActions.Reverse().ToList();
    }

    private void VisitDependencies(string action, IDictionary<string, bool> visited, IList<string> actions)
    {
      bool fullyVisited;
      if (!visited.TryGetValue(action, out fullyVisited))
        visited[action] = false;
      else if (!fullyVisited)
        throw new Exception(string.Format("Circular dependency detected, {0} action needed by itself", action));
      actions.Add(action);
      ActionConfig config;
      if (!Action.TryGetValue(action, out config))
        throw new KeyNotFoundException(string.Format("Action {0} not defined", action));
      foreach (string dependency in config.Dependency.Reverse<string>())
        VisitDependencies(dependency, visited, actions);
      visited[action] = true;
    }
  }

  public class ActionConfig
  {
    public string Name;
    public List<string> Dependency = new List<string>();
    public List<CommandInvocation> Command = new List<CommandInvocation>();
  }
}
