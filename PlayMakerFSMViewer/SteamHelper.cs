using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace PlayMakerFSMViewer
{
    public static class SteamHelper
    {
        public static readonly int HOLLOWKNIGHT_APP_ID = 367520;
        public static readonly string HOLLOWKNIGHT_GAME_NAME = "Hollow Knight";

        public static string FindHollowKnightPath()
        {
            return FindSteamGamePath(HOLLOWKNIGHT_APP_ID, HOLLOWKNIGHT_GAME_NAME);
        }

        public static string FindSteamGamePath(int appid, string gameName)
        {
            if (ReadRegistrySafe("Software\\Valve\\Steam", "SteamPath") == null)
            {
                MessageBox.Show("You either don't have steam installed or your registry variable isn't set.");
                return "";
            }

            string appsPath = Path.Combine((string)ReadRegistrySafe("Software\\Valve\\Steam", "SteamPath"), "steamapps");

            if (File.Exists(Path.Combine(appsPath, $"appmanifest_{appid}.acf")))
            {
                return Path.Combine(Path.Combine(appsPath, "common"), gameName);
            }

            string path = SearchAllInstallations(Path.Combine(appsPath, "libraryfolders.vdf"), appid, gameName);
            if (path == null)
            {
                MessageBox.Show("Couldn't find installation automatically. Please pick the location manually.");
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.ValidateNames = false;
                ofd.CheckFileExists = false;
                ofd.CheckPathExists = true;
                if (ofd.ShowDialog() == true)
                {
                    return Path.GetDirectoryName(ofd.FileName);
                    //MessageBox.Show($"It appears you don't have {gameName} installed anywhere. The game files are needed to find the game.");
                }
            }
            else
            {
                return path;
            }

            return "";
        }

        private static string SearchAllInstallations(string libraryfolders, int appid, string gameName)
        {
            if (!File.Exists(libraryfolders))
            {
                return null;
            }
            StreamReader file = new StreamReader(libraryfolders);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                line.Trim();
                line.Trim('\t');
                line = Regex.Unescape(line);
                Match regMatch = Regex.Match(line, "\"(.*)\"\t*\"(.*)\"");
                string key = regMatch.Groups[1].Value;
                string value = regMatch.Groups[2].Value;
                int number;
                if (int.TryParse(key, out number))
                {
                    if (File.Exists(Path.Combine(value, $"steamapps/appmanifest_{appid}.acf")))
                    {
                        return Path.Combine(Path.Combine(value, "steamapps/common"), gameName);
                    }
                }
            }

            return null;
        }

        private static object ReadRegistrySafe(string path, string key)
        {
            using (RegistryKey subkey = Registry.CurrentUser.OpenSubKey(path))
            {
                if (subkey != null)
                {
                    return subkey.GetValue(key);
                }
            }

            return null;
        }
    }
}
