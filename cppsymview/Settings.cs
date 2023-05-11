using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace cppsymview
{
    public class Settings
    {
        public List<string> Files { get; set; } = new List<string>();

        public static Settings Load()
        {
            string jsonString = File.ReadAllText("settings.json");
            return JsonSerializer.Deserialize<Settings>(jsonString);
        }
        public void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText("settings.json", jsonString);            
        }
    }
}
