using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.IO;
using TShockAPI;

namespace StaffChatPlugin
{
	public class Config
    {
        public static string SavePath = Path.Combine(TShock.SavePath, "StaffChat.json");
        public static Color DefaultChatColor = new Color(200, 50, 150);
        public Color ChatColor = DefaultChatColor;
        public string StaffChatPrefix = "[StaffChat]";
        public string StaffChatGuestTag = "<Guest>";

        public static Config Load()
        {
            using (StreamReader sr = new StreamReader(File.Open(SavePath, FileMode.Open)))
            {
                return JsonConvert.DeserializeObject<Config>(sr.ReadToEnd());
            }
        }

        public void Save()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(File.Open(SavePath, FileMode.Create)))
                {
                    sw.Write(JsonConvert.SerializeObject(this));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[StaffChatPlugin] - Error loading config file!");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
