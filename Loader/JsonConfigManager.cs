using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fivepd_json.models;
using Newtonsoft.Json;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace fivepd_json.Loader
{
    public static class JsonConfigManager
    {
        public static List<CalloutConfig> Configs { get; private set; }

        static JsonConfigManager()
        {
            Configs = new List<CalloutConfig>();
            LoadConfigs();
        }

        public static void LoadConfigs()
        {
            if (Configs.Count > 0) return; // prevent double load

            var manifestJson = LoadResourceFile(GetCurrentResourceName(), "callouts/json_callouts/manifest.json");
            if (string.IsNullOrEmpty(manifestJson))
            {
                Debug.WriteLine("[JsonConfigManager] ⚠️ Could not load manifest.json");
                return;
            }

            List<string> files;
            try
            {
                files = JsonConvert.DeserializeObject<List<string>>(manifestJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JsonConfigManager] ⚠️ Failed to parse manifest.json: {ex.Message}");
                return;
            }

            if (files == null || files.Count == 0)
            {
                Debug.WriteLine("[JsonConfigManager] ⚠️ Manifest.json is empty or invalid");
                return;
            }

            foreach (var fileName in files)
            {
                var json = LoadResourceFile(GetCurrentResourceName(), $"callouts/json_callouts/{fileName}");
                if (string.IsNullOrEmpty(json))
                {
                    Debug.WriteLine($"[JsonConfigManager] ⚠️ Could not load {fileName}");
                    continue;
                }

                try
                {
                    var cfg = JsonConvert.DeserializeObject<CalloutConfig>(json);
                    if (cfg != null && !string.IsNullOrEmpty(cfg.shortName))
                    {
                        Configs.Add(cfg);
                        Debug.WriteLine($"[JsonConfigManager] 📥 Loaded config: {cfg.shortName}");
                    }
                    else
                    {
                        Debug.WriteLine($"[JsonConfigManager] ⚠️ Invalid config in {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[JsonConfigManager] 💥 Failed to parse {fileName}: {ex.Message}");
                }
            }
        }

        public static CalloutConfig GetRandomConfig()
        {
            if (Configs.Count == 0) return null;
            var rnd = new Random();
            return Configs[rnd.Next(Configs.Count)];
        }
    }
}