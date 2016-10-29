using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Configuration
{
  public enum Arch
  {
    Unknown,
    x86,
    x64,
    ARM,
    ARM64,
    IA64,
  };

  public enum OS
  {
    Unknown,
    Windows,
    Unix,
    Mac,
  };

  public struct Platform
  {
    public static OS GetOS(string os)
    {
      switch (os.ToLowerInvariant())
      {
        case "win":
        case "windows":
        case "win32":
        case "win64":
          return OS.Windows;
        case "unix":
        case "linux":
          return OS.Unix;
        case "mac":
        case "macos":
        case "macosx":
          return OS.Mac;
        default:
          return OS.Unknown;
      }
    }

    public static Arch GetArch(string arch)
    {
      switch (arch.ToLowerInvariant())
      {
        case "x86":
        case "i386":
        case "i586":
        case "i686":
        case "win32":
          return Arch.x86;
        case "x64":
        case "x86-64":
        case "x86_64":
        case "amd64":
          return Arch.x64;
        case "itanium":
        case "ia64":
          return Arch.IA64;
        case "arm":
        case "arm7":
        case "armv7":
        case "arm6":
        case "armv6":
        case "arm32":
          return Arch.ARM;
        case "arm64":
        case "arm8":
        case "armv8":
          return Arch.ARM64;
        default:
          return Arch.Unknown;
      }
    }

    public static Platform GetPlatform(string platform)
    {
      int idx = platform.IndexOf('-');
      int idxUnder = platform.IndexOf('_');
      if (idxUnder >= 0 && (idx < 0 || idxUnder < idx))
        idx = idxUnder;
      string os = (idx < 0) ? platform : platform.Substring(0, idx);
      string arch = (idx < 0) ? platform : platform.Substring(idx + 1);
      return new Platform() { OS = GetOS(os), Arch = GetArch(arch) };
    }

    public static bool operator==(Platform a, Platform b)
    {
      return a.OS == b.OS && a.Arch == b.Arch;
    }

    public static bool operator!=(Platform a, Platform b)
    {
      return !(a == b);
    }

    public override bool Equals(object obj)
    {
      return base.Equals(obj);
    }

    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    public OS OS;
    public Arch Arch;

    public static readonly Platform CurrentPlatform = new Platform() { OS = GetOS(), Arch = GetArch() };
    public static OS CurrentOS { get { return CurrentPlatform.OS; } }
    public static Arch CurrentArch { get { return CurrentPlatform.Arch; } }
    public override string ToString()
    {
      return string.Format("{0}-{1}", CurrentOS, CurrentArch);
    }

    private static OS GetOS()
    {
      switch (Environment.OSVersion.Platform)
      {
        case PlatformID.MacOSX:
          return OS.Mac;
        case PlatformID.Unix:
          return OS.Unix;
        case PlatformID.Win32NT:
        case PlatformID.Win32S:
        case PlatformID.Win32Windows:
        case PlatformID.WinCE:
          return OS.Windows;
        default:
          return OS.Unknown;
      }
    }

    private static Arch GetArch()
    {
      PortableExecutableKinds peKind;
      ImageFileMachine machine;
      Assembly.GetExecutingAssembly().ManifestModule.GetPEKind(out peKind, out machine);
      switch (machine)
      {
        case ImageFileMachine.AMD64:
          return Environment.Is64BitOperatingSystem ? Arch.x64 : Arch.x86;
        case ImageFileMachine.I386:
          return Arch.x86;
        case ImageFileMachine.IA64:
          return Arch.IA64;
        case ImageFileMachine.ARM:
          return Environment.Is64BitOperatingSystem ? Arch.ARM64 : Arch.ARM;
        default:
          return Arch.Unknown;
      }
    }
  }
}
