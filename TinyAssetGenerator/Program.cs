using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TinyAssetGenerator
{
    public enum Platform
    {
        Unknown,
        iOS,
        Android,
        UWP
    }

    public class AssetParser
    {
        private string[] handledExtensions = { ".gif", ".jpg", ".png", ".jpeg" };

        public void ParseDirectory(DirectoryInfo dir, Platform probablePlatform)
        {
            if (dir.Name.ToLower().Contains("droid"))
                probablePlatform = Platform.Android;
            if (dir.Name.ToLower().Contains("ios"))
                probablePlatform = Platform.iOS;
            if (dir.Name.ToLower().Contains("uwp"))
                probablePlatform = Platform.UWP;
            
            foreach (var file in dir.GetFiles())
            {
                if (HandleFile(file))
                {
                    MoveFile(file, probablePlatform);
                }
            }
            foreach (var subdir in dir.GetDirectories())
            {
                ParseDirectory(subdir, probablePlatform);
            }
        }

        private void MoveFile(FileInfo file, Platform probablePlatform)
        {
            var targetPlatform = probablePlatform;
            if (file.Name.Contains("dpi"))
            {
                targetPlatform = Platform.Android;
            }
            else if (file.Name.Contains("@"))
            {
                targetPlatform = Platform.iOS;
            }

            if (targetPlatform != Platform.Unknown)
            {
                //Console.WriteLine($"Moving {file.Name} to {targetPlatform}");
                switch (targetPlatform)
                {
                    case Platform.Android:
                        MoveToAndroid(file);
                        break;
                    case Platform.iOS:
                        MoveToApple(file);
                        break;
                    default:
                        Console.WriteLine($"Unhandled platform, skipping {file.Name}");
                        break;
                }
               
            }
        }

        private string droidResourceDir = "";
        private string iosResourceDir = "";

        private Dictionary<string, DirectoryInfo> droidDrawableDirs = new Dictionary<string, DirectoryInfo>();

        public void MatchDroidFolders(string path)
        {
            droidResourceDir = path;
            droidDrawableDirs.Add("", new DirectoryInfo(path + "/drawable"));
            foreach (var dir in new DirectoryInfo(path).GetDirectories().OrderByDescending(d => d.Name.Length))
            {
                var dirName = dir.Name.ToLower();
                if (dirName.Contains("drawable-"))
                {
                    var resName = dirName.Replace("drawable-", "");
                    droidDrawableDirs.Add(resName, dir);
                }
            }
        }

        public bool OverwriteExisting { get; set; }

        private void MoveToApple(FileInfo file)
        {
            Console.Write($"iOS {file.Name}: ");
            Copy(iosResourceDir, file);
        }

        private void Copy(string toDir, FileInfo fi, string newFilename = "")
        {
            try
            {
                if (string.IsNullOrEmpty(newFilename))
                    newFilename = fi.Name;
                fi.CopyTo(toDir + "/" + newFilename, OverwriteExisting);
                Console.WriteLine("Copied!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not copy, " + ex.Message);
            }
        }

        private string[] androidRes = { "xxxhdpi", "xxhdpi", "xhdpi", "hdpi", "ldpi", "mdpi" };

        private void MoveToAndroid(FileInfo file)
        {
            //var outputDir = droidResourceDir + "/drawable";
            if (file.Name.Contains("dpi"))
            {
                var keyvalue = GetDroidDir(file);
                if (keyvalue.Value != null)
                {
                    var outputName = file.Name.Replace(keyvalue.Key, "").Replace("@", "");
                    Console.Write($"DROID {file.Name} -> {outputName}, {keyvalue.Value.Name}: ");
                    Copy(keyvalue.Value.FullName, file, outputName);
                    //file.CopyTo(keyvalue.Value.FullName + "/" + outputName, OverwriteExisting);
                }
                else
                    Console.WriteLine($"DROID SKIP {file.Name}");
            }
        }

        private KeyValuePair<string, DirectoryInfo> GetDroidDir(FileInfo file)
        {
            var ret = droidDrawableDirs.FirstOrDefault(d => string.IsNullOrEmpty(d.Key));
            var toMatch = androidRes.FirstOrDefault(d => file.Name.Contains(d));
            if (toMatch != null)
            {
                return droidDrawableDirs.FirstOrDefault(d => d.Key == toMatch);
                //foreach (var dpi in droidDrawableDirs.Keys.Where(d => !string.IsNullOrEmpty(d)))
                //{
                //    if (file.Name.ToLower().Contains(toMatch))
                //        return droidDrawableDirs.FirstOrDefault(d => d.Key == dpi);
                //}
                //return new KeyValuePair<string, DirectoryInfo>(toMatch, ret.Value);
            }
            return ret;
        }

        private bool HandleFile(FileInfo file)
        {
            return handledExtensions.Contains(file.Extension.ToLower());
        }

        public void SetIosResourceDir(string dir)
        {
            iosResourceDir = dir;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var parser = new AssetParser();
            parser.SetIosResourceDir("/Users/mats/Projects/Coor.MyWorkplace/src/CMW.Mobile/CMW.Mobile/CMW.Mobile.iOS/Resources");
            parser.MatchDroidFolders("/Users/mats/Projects/Coor.MyWorkplace/src/CMW.Mobile/CMW.Mobile/CMW.Mobile.Droid/Resources");
            parser.ParseDirectory(new DirectoryInfo("/Users/mats/Downloads/App assets"), Platform.Unknown);
            Console.ReadKey();
        }
    }
}
