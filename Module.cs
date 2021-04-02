using ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using TwitchIRC;
using System.Collections;

namespace EnemyRenamerTwitch
{
    public class TwitchRenamerModule : ETGModule
    {
        public static readonly string MOD_NAME = "Enemy Renamer Mod Twitch Version";
        public static readonly string VERSION = "0.0.0";
        public static readonly string TEXT_COLOR = "#00FFFF";
        /// <summary>
        /// initilizes most variables from files,adds commands, initializes the GiveName event, and attaches a quit handler to the game's main game manager
        /// </summary>
        public override void Start()
        {
            GameManager.Instance.gameObject.AddComponent<QuitHandler>();
            Settings.LoadInfo();
            
            ETGModConsole.Commands.AddUnit("RenamerTwitch", new Action<string[]>(this.ToggleIntegration));
            ETGModConsole.Commands.AddUnit("RenamerMsg", new Action<string[]>(this.ToggleSpeech));

            ETGMod.AIActor.OnPostStart += GiveName;
            Log($"{MOD_NAME} v{VERSION} started successfully.", TEXT_COLOR);
        }

        public static void Log(string text, string color="#FFFFFF")
        {
            ETGModConsole.Log($"<color={color}>{text}</color>");
        }

        public static void GiveName(AIActor actor)
        {
            actor.gameObject.AddComponent<Larry>();
            
        }//attaches the "Larry" component to all aiActors
        //mostly stolen from kyle, but basically tries to load info from file and start listening to chat, or stop listening to chat to disable twitch mod. thanks kyle (:
        public void ToggleIntegration(string[] args)
        {
            if (!TwitchRenamerModule.integrationEnabled)
            {
                if (!Settings.LoadInfo())
                {
                    if (TwitchRenamerModule.listener != null && TwitchRenamerModule.listener.Connected)
                    {
                        TwitchRenamerModule.listener.StopListening();
                        TwitchRenamerModule.integrationEnabled = false;
                    }
                }
                else
                {
                    if (TwitchRenamerModule.listener == null)
                    {
                        TwitchRenamerModule.listener = new ChatListener(Settings.channel, Settings.oauth, Settings.channel);
                        TwitchRenamerModule.listener.Connect();
                        TwitchRenamerModule.listener.OnChatMessage += TwitchRenamerModule.HandleSentMessge;
                    }
                    TwitchRenamerModule.listener.StartListening();                    
                    TwitchRenamerModule.integrationEnabled = true;
                }
            }
            else
            {
                this.Disable();
            }
            TwitchRenamerModule.LogActiveStatus();
        }
        //disables twitch mod by stopping listening to chat. does not remove existing names from the database. it does stop messeges popping from enemies though.
        public void Disable()
        {
            if (TwitchRenamerModule.listener != null && TwitchRenamerModule.listener.Connected)
            {
                TwitchRenamerModule.listener.StopListening();
            }
            TwitchRenamerModule.integrationEnabled = false;
        }
        // fancy little status logger stolen from kyle. thanks kyle (:
        public static void LogActiveStatus()
        {
            string color = TwitchRenamerModule.integrationEnabled ? "<color=#00FF00FF>" : "<color=#FF0000FF>";
            string text = TwitchRenamerModule.integrationEnabled ? "enabled" : "disabled";
            ETGModConsole.Log("EnemyRenamer Twitch Mode " + color + text + "</color>", false);
        }
        //very simple toggler command for togglinf "SpeechEnabled" bool variable
        public void ToggleSpeech(string[] args)
        {
            TwitchRenamerModule.SpeechEnabled = !TwitchRenamerModule.SpeechEnabled;
            string color = TwitchRenamerModule.SpeechEnabled ? "<color=#00FF00FF>" : "<color=#FF0000FF>";
            string text = TwitchRenamerModule.SpeechEnabled ? "enabled" : "disabled";
            ETGModConsole.Log("EnemyRenamer Messege Mode " + color + text + "</color>", false);
        }
        //takes messeges from the listener, and handles adding names to the database. if speech is enabled, handles that.
        public static void HandleSentMessge(string nick, string msg, string channel)
        {
            if (!Larry.EnemyDictionary.ContainsKey(nick))
            {
                Larry.namesDB.Add(nick);
                Larry.EnemyDictionary.Add(nick, new List<Larry>());
            }
            else if(SpeechEnabled)
            {
                HandleEnemySpeak(nick, msg);
            }              
        }
        //loops through the list of enemies in the dictionary, and pushes messeges into them to be popped.
        public static void HandleEnemySpeak(string key,string msg)
        {
            if (Larry.EnemyDictionary.ContainsKey(key) && Larry.EnemyDictionary[key] != null)
            {
                List<Larry> list = Larry.EnemyDictionary[key];
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].PushMessegeToMessegeQueue(msg);
                }
            }
        }
       
        /// <summary>
        /// logs error to mtg console, and also dumps it to a file
        /// </summary>
        /// <param name="error"></param>
        public static void LogError(string error)
        {
            if (!File.Exists(TwitchRenamerModule.logFilePath))
            {
                File.Create(TwitchRenamerModule.logFilePath).Close();
            }
            using (StreamWriter streamWriter = new StreamWriter(TwitchRenamerModule.logFilePath, true))
            {
                streamWriter.WriteLine(error);
            }
            ETGModConsole.Log("<color=#FF0000FF>" + error + "</color>", false);
        }

        public override void Exit(){ }

        public override void Init() { }

        public static ChatListener listener = null;

        private static bool SpeechEnabled = false;

        public static bool integrationEnabled = false;
       
        public static TwitchRenamerModule instance;

        private static string logFilePath = Path.Combine(ETGMod.ResourcesDirectory, "enemy_renamer_twitch_error_log.txt");
    }
}
