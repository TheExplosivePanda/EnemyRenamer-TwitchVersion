using System;
using System.IO;

namespace EnemyRenamerTwitch
{
    public static class Settings
    {   
        //this class is almost entirely takes from kyle's twitch integration, stripped down to only fit the basic needs this mod requires.
        //handles reading and generating files, mostly.
        //yes, this mod is about 89% KyleTheScientist, what about it?
        public static bool LoadInfo()
        {
            bool result;
            if (!File.Exists(Settings.filePath))
            {
                TwitchRenamerModule.LogError("Could not find " + Settings.fileName + ", generating a new one.");
                TwitchRenamerModule.LogError("Please fill out the fields in the file: " + Settings.filePath);
                TwitchRenamerModule.LogError(Settings.filePath);
                TwitchRenamerModule.LogError("You can find your oauth code at https://twitchapps.com/tmi/");
                Settings.Generate();
                result = false;
            }
            else
            {
                string[] array = File.ReadAllLines(Settings.filePath);
                bool flag2 = array == null || array.Length == 0;
                if (flag2)
                {
                    TwitchRenamerModule.LogError("Invalid " + Settings.fileName);
                }
                result = Settings.GetData(array);
            }
            return result;
        }

        private static bool GetData(string[] lines)
        {
            Settings.oauth = null;
            Settings.channel = null;
            foreach (string text in lines)
            {
                if (text.Contains(Settings.oauthString))
                {
                    string text2 = ETGMod.RemovePrefix(text, Settings.oauthString).Trim();
                    if (!text2.Contains("oauth:"))
                    {
                        text2 = "oauth:" + text2;
                    }
                    Settings.oauth = text2;
                }
                else
                {
                    
                    if (text.Contains(Settings.channelString))
                    {
                        Settings.channel = ETGMod.RemovePrefix(text, Settings.channelString).Trim();
                    }
                    
                }
            }

            bool result;
            if (Settings.oauth == null || Settings.oauth.Length == 0)
            {
                TwitchRenamerModule.LogError("No o-auth found in " + Settings.fileName);
                result = false;
            }
            else if (Settings.channel == null || Settings.channel.Length == 0)
            {
                TwitchRenamerModule.LogError("No channel found in " + Settings.fileName);
                result = false;
            }
            else
            {
                result = true;
            }




            return result;
        }

        private static void Generate()
        {
            File.Create(Settings.filePath).Close();
            using (StreamWriter streamWriter = new StreamWriter(Settings.filePath))
            {
                streamWriter.WriteLine(Settings.channelString);
                streamWriter.WriteLine(Settings.oauthString);
            }
        }

        
        public static string channel;

        public static string oauth;

        private static string fileName = "RenamerModTwitchSettings.txt";

        private static string filePath = Path.Combine(ETGMod.ResourcesDirectory, Settings.fileName);

        private static string channelString = "channel=";

        private static string oauthString = "o-auth=";
    }
}
