using System.Diagnostics;

namespace Configuration
{
  class Program
  {
    public static string Version = "0.1";
    static void Main(string[] args)
    {
      Options options = new Options();
      options.ParseCommandLine(args);
      new Configuration(options).Execute();
    }
  }
}
