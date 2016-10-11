using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Configuration
{
  public interface ISerializer<T>
  {
    void Serialize(string path, T obj);
    void Serialize(string path, T obj, SerializationOptions options);
    T Deserialize(string path);
    T Deserialize(string path, SerializationOptions options);
    void Deserialize(string path, T obj);
    void Deserialize(string path, T obj, SerializationOptions options);
  }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
  public class ClearSerializedCollectionOnMergeAttribute : Attribute
  {
  }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
  public class NonSerializedAttribute : Attribute
  {
  }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
  public class SerializedAttribute : Attribute
  {
  }

  public enum AccessModifier
  {
    Public,
    Protected,
    Private,
  }

  public class SerializationOptions
  {
    public class AccessibilityOptions
    {
      public AccessModifier LevelAllowed = AccessModifier.Public;
      public bool AllowInternal = false;

      public bool Check(FieldInfo info)
      {
        if (info.IsPublic)
          return true;
        if (info.IsFamilyOrAssembly)
          return LevelAllowed >= AccessModifier.Protected || AllowInternal;
        if (info.IsFamily)
          return LevelAllowed >= AccessModifier.Protected;
        if (info.IsAssembly)
          return AllowInternal;
        if (info.IsFamilyAndAssembly)
          return LevelAllowed >= AccessModifier.Protected && AllowInternal;
        if (info.IsPrivate)
          return LevelAllowed >= AccessModifier.Private;
        throw new NotSupportedException("Unhandled accessibility level");
      }

      public bool Check(MethodInfo info)
      {
        if (info.IsPublic)
          return true;
        if (info.IsFamilyOrAssembly)
          return LevelAllowed >= AccessModifier.Protected || AllowInternal;
        if (info.IsFamily)
          return LevelAllowed >= AccessModifier.Protected;
        if (info.IsAssembly)
          return AllowInternal;
        if (info.IsFamilyAndAssembly)
          return LevelAllowed >= AccessModifier.Protected && AllowInternal;
        if (info.IsPrivate)
          return LevelAllowed >= AccessModifier.Private;
        throw new NotSupportedException("Unhandled accessibility level");
      }
    }

    public bool IgnoreAttributes = false;
    public bool SerializeFields = true;
    public bool SerializeProperties = true;
    public AccessibilityOptions FieldAccess = new AccessibilityOptions();
    public bool FieldAllowStatic = false;
    public AccessibilityOptions PropertyGetterAccess = new AccessibilityOptions();
    public AccessibilityOptions PropertySetterAccess = new AccessibilityOptions();
    public bool PropertyAllowStatic = false;
    public delegate bool CheckAllowed(MemberInfo member, bool valid);
    public CheckAllowed CustomChecker = null;

    private bool CheckInternal(MemberInfo member)
    {
      if (!IgnoreAttributes)
      {
        if (member.GetCustomAttribute<NonSerializedAttribute>() != null || member.GetCustomAttribute<System.NonSerializedAttribute>() != null)
          return false;
        if (member.GetCustomAttribute<SerializedAttribute>() != null)
          return true;
      }
      if (member is FieldInfo)
      {
        FieldInfo field = member as FieldInfo;
        if (field.IsStatic && !FieldAllowStatic)
          return false;
        if (field.IsInitOnly)
          return false;
        return (FieldAccess.Check(field));
      }
      if (member is PropertyInfo)
      {
        PropertyInfo property = member as PropertyInfo;
        if (property.GetGetMethod() == null || property.GetSetMethod() == null)
          return false;
        if (property.GetGetMethod().IsStatic && !PropertyAllowStatic)
          return false;
        return (PropertyGetterAccess.Check(property.GetGetMethod()) && PropertySetterAccess.Check(property.GetSetMethod()));
      }
      throw new NotSupportedException(string.Format("Unsupported MemberInfo type {0}", member.GetType()));
    }

    public bool Check(MemberInfo member)
    {
      bool valid = CheckInternal(member);
      if (CustomChecker != null)
        return CustomChecker(member, valid);
      return valid;
    }
  }

  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
  public class DictionaryEmbeddedKeyAttribute : Attribute
  {
    public DictionaryEmbeddedKeyAttribute() { }
    public DictionaryEmbeddedKeyAttribute(string keyMemberName) { KeyMemberName = keyMemberName; }
    public string KeyMemberName { get; set; }
  }

  public static class SerializerFactory
  {
    public static ISerializer<T> GetSerializer<T>() where T : class
    {
      return new ConfigSerializer<T>();
    }

    private class ConfigSerializer<T> : ISerializer<T> where T : class
    {
      #region Interface
      public void Serialize(string path, T obj)
      {
        Serialize(path, obj, new SerializationOptions());
      }

      public void Serialize(string path, T obj, SerializationOptions options)
      {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (StreamWriter sw = new StreamWriter(path))
        {
          SerializeObject(sw, obj, typeof(T), "", options);
        }
      }

      public T Deserialize(string path)
      {
        return Deserialize(path, new SerializationOptions());
      }

      public T Deserialize(string path, SerializationOptions options)
      {
        T obj = Activator.CreateInstance<T>();
        Deserialize(path, obj, options);
        return obj;
      }

      public void Deserialize(string path, T obj)
      {
        Deserialize(path, obj, new SerializationOptions());
      }

      public void Deserialize(string path, T obj, SerializationOptions options)
      {
        DeserializeInternal(File.ReadAllLines(path), obj, typeof(T), 0, -1, options);
      }
      #endregion

      #region Formatting helpers
      private const uint ColumnAlignmentStep = 4;
      private static string[] GenerateColumnAlignment(int n)
      {
        string[] ret = new string[n];
        for (int i = 0; i < n - 1; ++i)
          ret[i] = new string(' ', n - 1 - i);
        ret[n - 1] = string.Empty;
        return ret;
      }
      private static readonly string[] ColumnAlignment = GenerateColumnAlignment(40);

      private static readonly string IndentIncrement = "  ";
      #endregion

      #region Type helpers
      private static bool IsSimpleType(Type t)
      {
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal);
      }

      private static KeyValuePair<Type, Type>? FindDictionaryElementsType(Type t)
      {
        if (t == null)
          return null;
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>))
        {
          Type[] genericParams = t.GetGenericArguments();
          if (genericParams.Length == 2)
          {
            Type iDic = typeof(IDictionary<,>).MakeGenericType(genericParams);
            if (iDic.IsAssignableFrom(t))
              return new KeyValuePair<Type, Type>(genericParams[0], genericParams[1]);
          }
        }
        Type[] interfaceList = t.GetInterfaces();
        if (interfaceList != null && interfaceList.Length > 0)
        {
          foreach (Type interfaceType in interfaceList)
          {
            KeyValuePair<Type, Type>? ret = FindDictionaryElementsType(interfaceType);
            if (ret != null)
              return ret;
          }
        }
        if (t.BaseType != null && t.BaseType != typeof(object))
          return FindDictionaryElementsType(t.BaseType);
        return null;
      }

      private static Type FindCollectionElementType(Type t)
      {
        if (t == null || t == typeof(string))
          return null;
        if (t.IsArray)
          return t.GetElementType();
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
          foreach (Type genericParams in t.GetGenericArguments())
          {
            Type iCol = typeof(ICollection<>).MakeGenericType(genericParams);
            if (iCol.IsAssignableFrom(t))
              return genericParams;
          }
        }
        Type[] interfaceList = t.GetInterfaces();
        if (interfaceList != null && interfaceList.Length > 0)
        {
          foreach (Type interfaceType in interfaceList)
          {
            Type iCol = FindCollectionElementType(interfaceType);
            if (iCol != null)
              return iCol;
          }
        }
        if (t.BaseType != null && t.BaseType != typeof(object))
          return FindCollectionElementType(t.BaseType);
        return null;
      }

      private static object Parse(Type t, string s)
      {
        if (t.IsEnum)
          return Enum.Parse(t, s);
        return Convert.ChangeType(s, t);
      }

      private class FieldAccessor
      {
        #region Public interface
        public readonly string Name;
        public readonly Type FieldType;
        public readonly Type Type;
        public readonly Type KeyType;
        public readonly string EmbeddedKey;
        public readonly bool ClearCollection;
        #endregion

        #region Internal union
        private readonly FieldInfo fi;
        private readonly PropertyInfo pi;
        private readonly object Key;

        private object obj;
        #endregion

        public FieldAccessor(object obj, Type t, object key)
        {
          Type = t;
          Name = (key != null ? key.ToString() : null);
          this.obj = obj;
          KeyValuePair<Type, Type>? kvpDic = FindDictionaryElementsType(t);
          if (kvpDic.HasValue)
          {
            fi = null;
            pi = null;
            KeyType = kvpDic.Value.Key;
            FieldType = kvpDic.Value.Value;
            EmbeddedKey = null;
            ClearCollection = false;
          }
          else
          {
            KeyType = null;
            fi = t.GetField(Name, BindingFlags.Public | BindingFlags.Instance);
            pi = t.GetProperty(Name, BindingFlags.Public | BindingFlags.Instance);
            if (fi == null && pi == null)
              throw new Exception(string.Format("Field {0} not found in type {1}", key, t));
            if (fi != null && fi.IsInitOnly)
              throw new Exception(string.Format("Field {0} in {1} is read-only", key, t));
            if (pi != null && (pi.GetGetMethod() == null || pi.GetSetMethod() == null))
              throw new Exception(string.Format("Property {0} in {1} lacks getter or setter", key, t));
            FieldType = (fi != null ? fi.FieldType : pi.PropertyType);
            DictionaryEmbeddedKeyAttribute attr = ((MemberInfo)fi ?? pi).GetCustomAttribute<DictionaryEmbeddedKeyAttribute>();
            EmbeddedKey = (attr == null ? null : attr.KeyMemberName);
            ClearCollection = (((MemberInfo)fi ?? pi).GetCustomAttribute<ClearSerializedCollectionOnMergeAttribute>() != null);
          }
          Key = (KeyType != null && key != null && key.GetType() == typeof(string)) ? Parse(KeyType, (string)key) : key;
        }

        public object GetValue()
        {
          if (KeyType != null)
          {
            IDictionary dic = obj as IDictionary;
            return dic.Contains(Key) ? dic[Key] : null;
          }
          if (pi != null)
            return pi.GetValue(obj);
          return fi.GetValue(obj);
        }

        public void SetValue(object val)
        {
          if (KeyType != null)
            (obj as IDictionary)[Key] = val;
          else if (pi != null)
            pi.SetValue(obj, val);
          else
            fi.SetValue(obj, val);
        }

        public bool CheckAllowed(SerializationOptions options)
        {
          return (KeyType != null || options.Check((MemberInfo)fi ?? pi));
        }
      }
      #endregion

      #region Serialization
      private void SerializeMember(StreamWriter writer, string name, MemberInfo mi, object obj, Type t, string indent, SerializationOptions options)
      {
        if (IsSimpleType(t))
        {
          if ((t == typeof(string) ? !string.IsNullOrEmpty((string)obj) : obj != Activator.CreateInstance(t)))
            writer.WriteLine("{0}{1} {2}{3}", indent, name, (name.Length + indent.Length < ColumnAlignment.Length ? ColumnAlignment[name.Length + indent.Length] : ColumnAlignment[(ColumnAlignment.Length + indent.Length - ColumnAlignmentStep) + (name.Length + indent.Length) % ColumnAlignmentStep]), obj);
          return;
        }
        if (obj == null)
        {
          if (mi == null)
            writer.WriteLine("{0}[{1}]", indent, name);
          return;
        }
        KeyValuePair<Type, Type>? dicTypeOpt = FindDictionaryElementsType(t);
        if (dicTypeOpt.HasValue)
        {
          Type keyType = dicTypeOpt.Value.Key;
          Type valType = dicTypeOpt.Value.Value;
          IDictionary dic = obj as IDictionary;
          DictionaryEmbeddedKeyAttribute embed = null;
          if (mi != null)
            embed = mi.GetCustomAttribute<DictionaryEmbeddedKeyAttribute>();
          if (embed != null)
          {
            FieldInfo fi = valType.GetField(embed.KeyMemberName);
            PropertyInfo pi = valType.GetProperty(embed.KeyMemberName);
            if (fi == null && (pi == null || !pi.CanRead))
              throw new System.Runtime.Serialization.SerializationException(string.Format("Bad DictionaryEmbeddedKeyAttribute, {0} not found in {1}", embed.KeyMemberName, t));
            foreach (object val in dic.Values)
              SerializeMember(writer, name, null, val, valType, indent, options);
          }
          else
          {
            IDictionaryEnumerator enumerator = dic.GetEnumerator();
            writer.WriteLine("{0}[{1}]", indent, name);
            while (enumerator.MoveNext())
            {
              SerializeMember(writer, enumerator.Key.ToString(), null, enumerator.Value, valType, indent + IndentIncrement, options);
            }
          }
          return;
        }

        Type colType = FindCollectionElementType(t);
        if (colType != null)
        {
          foreach (object o in (obj as ICollection))
            SerializeMember(writer, name, null, o, colType, indent, options);
          return;
        }
        
        writer.WriteLine("{0}[{1}]", indent, name);
        SerializeObject(writer, obj, t, indent + IndentIncrement, options);
      }

      private void SerializeObject(StreamWriter writer, object obj, Type t, string indent, SerializationOptions options)
      {
        foreach (FieldInfo fi in t.GetFields())
        {
          SerializeMember(writer, fi.Name, fi, fi.GetValue(obj), fi.FieldType, indent, options);
        }
        foreach (PropertyInfo pi in t.GetProperties())
        {
          SerializeMember(writer, pi.Name, pi, pi.GetValue(obj), pi.PropertyType, indent, options);
        }
      }
      #endregion

      #region Deserialization
      private int DeserializeInternal(string[] lines, object obj, Type t, int lineStart, int previousIndent, SerializationOptions options)
      {
        ISet<string> clearedCollections = new SortedSet<string>();
        for (int i = lineStart; i < lines.Length; ++i)
        {
          if (string.IsNullOrWhiteSpace(lines[i]))
            continue;
          string line = lines[i].TrimStart();
          // Less spaces -> go back to outter object
          int whitespace = lines[i].Length - line.Length;
          if (whitespace <= previousIndent)
            return i - lineStart;

          string name;
          string value = null;
          if (line.StartsWith("["))
          {
            int idxEnd = line.IndexOf(']');
            if (idxEnd < 0)
              throw new Exception("Bad format, [ but no ]");
            name = line.Substring(1, idxEnd - 1);
            value = line.Substring(idxEnd + 1);
          }
          else
          {
            name = new string(line.TakeWhile(c => !char.IsWhiteSpace(c)).ToArray());
            value = line.Substring(name.Length);
          }

          value = value.Trim();
          FieldAccessor fa = new FieldAccessor(obj, t, name);
          if (!fa.CheckAllowed(options))
            throw new System.Runtime.Serialization.SerializationException(string.Format("Field {0} of type {1} is not allowed to deserialize as per serialization options", name, t));
          if (IsSimpleType(fa.FieldType))
          {
            object val = Parse(fa.FieldType, value);
            fa.SetValue(val);
            continue;
          }
          else
          {
            object val = fa.GetValue();
            if (val == null)
            {
              val = Activator.CreateInstance(fa.FieldType);
              fa.SetValue(val);
            }

            if (fa.EmbeddedKey != null)
            {
              KeyValuePair<Type, Type>? dicType = FindDictionaryElementsType(fa.FieldType);
              object dicVal = Activator.CreateInstance(dicType.Value.Value);
              int readLines = DeserializeInternal(lines, dicVal, dicType.Value.Value, i + 1, whitespace, options);
              FieldAccessor faDicKey = new FieldAccessor(dicVal, dicType.Value.Value, fa.EmbeddedKey);
              object keyVal = faDicKey.GetValue();
              FieldAccessor faDicVal = new FieldAccessor(val, fa.FieldType, keyVal);
              object oldVal = faDicVal.GetValue();
              if (oldVal == null)
              {
                faDicVal.SetValue(dicVal);
              }
              else
              {
                DeserializeInternal(lines, oldVal, dicType.Value.Value, i + 1, whitespace, options);
              }
              i += readLines;
              continue;
            }
            
            Type colType = FindCollectionElementType(fa.FieldType);
            if (colType != null && !FindDictionaryElementsType(fa.FieldType).HasValue)
            {
              if (fa.ClearCollection && !clearedCollections.Add(fa.Name))
                fa.FieldType.GetMethod("Clear").Invoke(val, new object[] {});
              object colVal;
              if (IsSimpleType(colType))
              {
                colVal = Parse(colType, value);
              }
              else
              {
                colVal = Activator.CreateInstance(colType);
                i += DeserializeInternal(lines, colVal, colType, i + 1, whitespace, options);
              }
              fa.FieldType.GetMethod("Add").Invoke(val, new object[] { colVal });
              continue;
            }
            
            i += DeserializeInternal(lines, val, fa.FieldType, i + 1, whitespace, options);
          }
        }
        return lines.Length - lineStart;
      }
      #endregion
    }
  }

}
