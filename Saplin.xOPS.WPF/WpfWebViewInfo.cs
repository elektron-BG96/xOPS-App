﻿using Saplin.xOPS.UI.Misc;
using System;
using System.Diagnostics;
using System.IO;
using Xamarin.Forms;

[assembly: Dependency(typeof(Saplin.xOPS.WPF.WpfWebViewInfo))]
namespace Saplin.xOPS.WPF
{
    public class WpfWebViewInfo : IWpfWebViewInfo
    {
        public string GetIEVersion()
        {
            string mshtmlPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "mshtml.dll");
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(mshtmlPath);
            var version = new Version(
                         fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);

            return version.ToString();
        }

        public bool InternetConnected()
        {
            return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        }
    }
}
