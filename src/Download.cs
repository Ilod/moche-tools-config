using System;
using System.ComponentModel;
using System.Net;
using System.Threading;

namespace Configuration
{
  public static class Downloader
  {
    public static void DownloadAsync(string file, string destination, AsyncCompletedEventHandler fileCompletedCallback, DownloadProgressChangedEventHandler progressChangedCallback = null)
    {
      WebClient wc = new WebClient();
      if (progressChangedCallback !=  null)
        wc.DownloadProgressChanged += progressChangedCallback;
      if (fileCompletedCallback != null)
        wc.DownloadFileCompleted += fileCompletedCallback;
      wc.DownloadFileAsync(new Uri(file), destination);
    }

    public static void DownloadSync(string file, string destination, DownloadProgressChangedEventHandler progressChangedCallback)
    {
      Synchronizer sync = new Synchronizer();
      DownloadAsync(file, destination, sync.DownloadCompleted, progressChangedCallback);
      sync.Signal.WaitOne();
      if (sync.Error != null)
        throw sync.Error;
    }

    private class Synchronizer
    {
      public ManualResetEvent Signal = new ManualResetEvent(false);
      public Exception Error = null;

      public void DownloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
      {
        if (e.Cancelled)
          Error = new Exception("Cancelled");
        if (e.Error != null)
          Error = e.Error;
        Signal.Set();
      }
    }
  }
}
