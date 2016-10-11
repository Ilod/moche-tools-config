﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Configuration
{
  public class Options
  {
    public Option Version = new VersionOption();
    public Option Help = new HelpOption();
    public ValueOption<bool> Verbose = new FlagOption() { Flags = { "-v", "--verbose" }, Description = "Display additional logs" };
    public ValueOption<string> BuildDir = new SimpleValueOption<string>() { Flags = { "--build", "-b" }, AdditionalParametersDescrption = "<path>", Description = "The path in which to generate the files", Value = Environment.CurrentDirectory };
    public ValueOption<string> SrcDir = new SimpleValueOption<string>() { Flags = { "--src", "-s" }, AdditionalParametersDescrption = "<path>", Description = "The path to the moche config file used to generate the project, or a folder with a moche.config file", Value = Environment.CurrentDirectory };
    public ValueOption<bool> Interactive = new FlagOption() { Flags = { "--interactive", "-i" }, Description = "Enter interactive mode" };
    public ValueOption<List<string>> Actions = new SimpleListOption<string> { Flags = { "--action", "-a" }, Description = "The action to execute. Can be provided multiple times to execute multiple actions. Built-in actions are retrieve-tools and clean-tools, others can be defined in moche configuration file. If not provided, defaults to retrieve-tools." };
    public Option Clean = new AliasFlagOption() { Flags = { "--clean", "-c" }, Description = "Clean the tool version files, forcing a full retrieval", Alias = { "--action", "clean-tools" } };
    public Option Regenerate = new AliasFlagOption() { Flags = { "--regenerate", "-r" }, Description = "Clean and redo the retrieval", Alias = { "--action", "delete", "--action", "retrieve-tools" } };
    public ValueOption<bool> AllowSourceChange = new FlagOption() { Flags = { "--allow-souce-change" }, Description = "Allow source change uppon error on a source. This may result in larger generation time, especially if some sources unavailability are only temporary" };
    public ValueOption<bool> Silent = new FlagOption() { Flags = { "--silent", "-s" }, Description = "Don't prompt for user input even uppon error" };

    public List<Option> GetAllOptions()
    {
      List<Option> optionList = new List<Option>();
      foreach (System.Reflection.FieldInfo field in GetType().GetFields().Where(field => typeof(Option).IsAssignableFrom(field.FieldType)))
      {
        optionList.Add(field.GetValue(this) as Option);
      }
      return optionList;
    }

    public void ParseCommandLine(string[] args)
    {
      List<Option> optionList = GetAllOptions();
      for (uint i = 0; i < args.Length; ++i)
      {
        Option opt = optionList.FirstOrDefault(o => o.Flags.Contains(args[i]));
        if (opt == null)
        {
          Console.Error.WriteLine("Unknown option {0}", args[i]);
          Environment.Exit(-1);
        }
        i += opt.Execute(this, args, i);
      }
    }
  }

  public abstract class Option
  {
    public List<string> Flags = new List<string>();
    public string AdditionalParametersDescrption;
    public virtual string AdditionalInfoDescription { get; }
    public string Description;
    public abstract uint Execute(Options options, string[] args, uint idx);
  }

  public abstract class ValueOption<T> : Option
  {
    public T Value;
  }

  public class AliasFlagOption : Option
  {
    public List<string> Alias = new List<string>();

    public override string AdditionalInfoDescription
    {
      get
      {
        return string.Format("Alias for {0}", string.Join(" ", Alias));
      }
    }

    public override uint Execute(Options options, string[] args, uint idx)
    {
      options.ParseCommandLine(Alias.ToArray());
      return 1;
    }
  }

  public class FlagOption : ValueOption<bool>
  {
    public override uint Execute(Options options, string[] args, uint idx)
    {
      Value = true;
      return 0;
    }
  }

  public class SimpleValueOption<T> : ValueOption<T>
  {
    public override uint Execute(Options options, string[] args, uint idx)
    {
      if (typeof(T).IsEnum)
        Value = (T)Enum.Parse(typeof(T), args[idx + 1]);
      else
        Value = (T)Convert.ChangeType(args[idx + 1], typeof(T));
      return 1;
    }
  }

  public class SimpleListOption<T> : ValueOption<List<T>>
  {
    public SimpleListOption()
    {
      Value = new List<T>();
    }

    public override uint Execute(Options options, string[] args, uint idx)
    {
      if (typeof(T).IsEnum)
        Value.Add((T)Enum.Parse(typeof(T), args[idx + 1]));
      else
        Value.Add((T)Convert.ChangeType(args[idx + 1], typeof(T)));
      return 1;
    }
  }

  public class VersionOption : Option
  {
    public VersionOption()
    {
      Flags.Add("--version");
      Description = "Print version number and exit";
    }

    public override uint Execute(Options options, string[] args, uint idx)
    {
      Console.WriteLine("moche configuration version {0}", Program.Version);
      Environment.Exit(0);
      return 0;
    }
  }

  public class HelpOption : Option
  {
    public HelpOption()
    {
      Flags.Add("-h");
      Flags.Add("--help");
      Description = "Print this help and exit";
    }

    public override uint Execute(Options options, string[] args, uint idx)
    {
      Console.WriteLine("configuration [options]");
      Console.WriteLine();
      Console.WriteLine("Options:");
      Console.WriteLine();
      foreach (Option opt in options.GetAllOptions())
      {
        Console.Write("  ");
        Console.Write(string.Join(" | ", opt.Flags));
        if (!string.IsNullOrWhiteSpace(opt.AdditionalParametersDescrption))
        {
          Console.Write(" ");
          Console.Write(opt.AdditionalParametersDescrption);
        }
        Console.WriteLine();
        string additionalInfoDescription = opt.AdditionalInfoDescription;
        if (!string.IsNullOrWhiteSpace(additionalInfoDescription))
        {
          Console.Write("        ");
          Console.WriteLine(additionalInfoDescription);
        }
        Console.Write("      ");
        Console.WriteLine(opt.Description);
      }
      Environment.Exit(0);
      return 0;
    }
  }
}
