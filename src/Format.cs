using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Configuration
{
  public class Arguments : IDictionary<string, string>
  {
    private class ArgumentFormatInfo
    {
      public ArgumentFormatInfo(int capacity = 50)
      {
        Str = new StringBuilder(capacity);
      }
      public override string ToString()
      {
        return Str.ToString();
      }
      public StringBuilder Str;
      public bool IsFunction = false;
      public string FunctionName = null;
      public bool IsConditional = false;
      public bool IsConditionChecked = false;
      public bool IsConditionNegative = false;
      public bool IsConditionValid = false;
      public string UnresolvedArgument = null;
    }

    private IDictionary<string, string>[] ArgumentsMapList;

    public Arguments(params IDictionary<string, string>[] args)
    {
      ArgumentsMapList = args;
    }

    public bool ParseBoolArg(Configuration c, string name, bool defaultValue = false)
    {
      string valUnformat;
      if (!TryGetValue(name, out valUnformat))
        return defaultValue;
      string val = Format(c, string.Format("{{{0}}}", name));
      return (!string.IsNullOrWhiteSpace(val) && val != "0" && !val.Equals("false", StringComparison.InvariantCultureIgnoreCase));
    }

    public string Format(Configuration c, string format)
    {
      char[] search = { '{', '}' };
      Stack<ArgumentFormatInfo> stack = new Stack<ArgumentFormatInfo>(5);

      stack.Push(new ArgumentFormatInfo(format.Length * 2));
      int startIndex = -1;
      int lastInsertedIdx = 0;
      while ((startIndex = format.IndexOfAny(search, startIndex + 1)) >= 0)
      {
        if (format[startIndex] == '{')
        {
          if (startIndex + 1 < format.Length && format[startIndex + 1] == '{') // Ignore {{
          {
            ++startIndex;
            continue;
          }
          if (stack.Count > 1 && stack.Peek().Str.Length == 0 && !stack.Peek().IsConditional && !stack.Peek().IsFunction)
          {
            if (format.Substring(lastInsertedIdx, startIndex - lastInsertedIdx).Equals("if ", StringComparison.InvariantCultureIgnoreCase))
            {
              stack.Peek().IsConditional = true;
            }
            else if (format.Substring(lastInsertedIdx, startIndex - lastInsertedIdx).Equals("if not ", StringComparison.InvariantCultureIgnoreCase))
            {
              stack.Peek().IsConditional = true;
              stack.Peek().IsConditionNegative = true;
            }
            else if (format.Substring(lastInsertedIdx, startIndex - lastInsertedIdx).Equals("function ", StringComparison.InvariantCulture))
            {
              stack.Peek().IsFunction = true;
            }
            if (!stack.Peek().IsConditional && !stack.Peek().IsFunction)
              stack.Peek().Str.Append(format, lastInsertedIdx, startIndex - lastInsertedIdx);
          }
          else
          {
            stack.Peek().Str.Append(format, lastInsertedIdx, startIndex - lastInsertedIdx);
          }
          stack.Push(new ArgumentFormatInfo());
          lastInsertedIdx = startIndex + 1;
        }
        else // '}'
        {
          int braceCount = 1;
          int idx = startIndex + 1;
          while (idx < format.Length && format[idx] == '}')
          {
            ++braceCount;
            ++idx;
          }
          if (braceCount % 2 == 0)
          {
            startIndex = idx;
            continue;
          }
          if (stack.Count == 1)
            throw new Exception("} without corresponding {");
          if (!stack.Peek().IsConditional && !stack.Peek().IsFunction)
          {
            string argName = stack.Pop().Str.Append(format, lastInsertedIdx, startIndex - lastInsertedIdx).ToString();
            if (stack.Peek().IsFunction && stack.Peek().FunctionName == null)
            {
              stack.Peek().FunctionName = argName;
            }
            else if (stack.Peek().IsConditional)
            {
              bool conditionValid = ParseBoolArg(c, argName);
              stack.Peek().IsConditionChecked = true;
              stack.Peek().IsConditionValid = (conditionValid != stack.Peek().IsConditionNegative);
              if (startIndex + 1 < format.Length && format[startIndex + 1] == ' ') // Ignore one space after condition
                ++startIndex;
            }
            else
            {
              string argValue;
              if (TryGetValue(argName, out argValue) && argValue != null)
              {
                stack.Peek().Str.Append(Format(c, argValue));
              }
              else if (stack.Peek().UnresolvedArgument == null)
              {
                stack.Peek().UnresolvedArgument = argName;
              }
            }
          }
          else if (stack.Peek().IsFunction)
          {
            string unresolvedArgument = stack.Peek().UnresolvedArgument;
            string functionName = stack.Peek().FunctionName;
            string arguments = stack.Pop().Str.Append(format, lastInsertedIdx, startIndex - lastInsertedIdx).ToString();
            if (unresolvedArgument == null)
              stack.Peek().Str.Append(Format(c, c.ExecuteFormatFunction(functionName, arguments)));
            else if (stack.Peek().UnresolvedArgument == null)
              stack.Peek().UnresolvedArgument = unresolvedArgument;
          }
          else
          {
            if (stack.Peek().IsConditionValid)
            {
              string unresolvedArgument = stack.Peek().UnresolvedArgument;
              string content = stack.Pop().Str.Append(format, lastInsertedIdx, startIndex - lastInsertedIdx).ToString();
              stack.Peek().Str.Append(content);
              if (stack.Peek().UnresolvedArgument == null)
                stack.Peek().UnresolvedArgument = unresolvedArgument;
            }
            else
            {
              stack.Pop();
            }
          }
          lastInsertedIdx = startIndex + 1;
        }
      }
      if (stack.Count != 1)
        throw new Exception("{ without corresponding }");
      if (stack.Peek().UnresolvedArgument != null)
        throw new Exception(string.Format("Argument {0} not found", stack.Peek().UnresolvedArgument));
      return stack.Pop().Str.Append(format, lastInsertedIdx, format.Length - lastInsertedIdx).ToString();
    }

    public ICollection<string> Keys
    {
      get
      {
        return ArgumentsMapList.SelectMany(d => d.Keys).Distinct().ToList();
      }
    }

    public ICollection<string> Values
    {
      get
      {
        return Keys.Select(k => this[k]).ToList();
      }
    }

    public int Count
    {
      get
      {
        return Keys.Count;
      }
    }

    public bool IsReadOnly
    {
      get
      {
        return true;
      }
    }

    public string this[string key]
    {
      get
      {
        string value;
        if (!TryGetValue(key, out value))
          throw new KeyNotFoundException(string.Format("Argument {0} not found", key));
        return value;
      }

      set
      {
        throw new NotSupportedException();
      }
    }

    public bool ContainsKey(string key)
    {
      string v;
      return TryGetValue(key, out v);
    }

    public void Add(string key, string value)
    {
      throw new NotSupportedException();
    }

    public bool Remove(string key)
    {
      throw new NotSupportedException();
    }

    public bool TryGetValue(string key, out string value)
    {
      foreach (IDictionary<string, string> dic in ArgumentsMapList)
      {
        if (dic.TryGetValue(key, out value))
          return true;
      }
      value = null;
      return false;
    }

    public void Add(KeyValuePair<string, string> item)
    {
      throw new NotSupportedException();
    }

    public void Clear()
    {
      throw new NotSupportedException();
    }

    public bool Contains(KeyValuePair<string, string> item)
    {
      string v;
      return (TryGetValue(item.Key, out v) && item.Value == v);
    }

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
      foreach (KeyValuePair<string, string> kvp in this)
      {
        array[arrayIndex++] = kvp;
      }
    }

    public bool Remove(KeyValuePair<string, string> item)
    {
      throw new NotSupportedException();
    }

    private class Enumerator : IEnumerator<KeyValuePair<string, string>>, IEnumerator
    {
      public Enumerator(Arguments args)
      {
        internalEnumerator = args.ArgumentsMapList.SelectMany(d => d).GetEnumerator();
      }

      private IEnumerator<KeyValuePair<string, string>> internalEnumerator;
      private ISet<string> usedKeys = new SortedSet<string>();

      public KeyValuePair<string, string> Current
      {
        get
        {
          return internalEnumerator.Current;
        }
      }

      object IEnumerator.Current
      {
        get
        {
          return internalEnumerator.Current;
        }
      }

      public void Dispose()
      {
        internalEnumerator.Dispose();
      }

      public bool MoveNext()
      {
        while (internalEnumerator.MoveNext())
        {
          if (usedKeys.Add(internalEnumerator.Current.Key))
            return true;
        }
        return false;
      }

      public void Reset()
      {
        internalEnumerator.Reset();
        usedKeys.Clear();
      }
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
      return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return new Enumerator(this);
    }
  }
}
