using System;
using System.Collections.Generic;
using System.Text;
using CitizenFX.Core;
using fivepd_json.models;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace fivepd_json.Loader
{
    public static class JsonConfigManager
    {
        public static List<CalloutConfig> Configs { get; private set; }
        private static HashSet<string> checkedCallouts = new HashSet<string>();
        static JsonConfigManager()
        {
            Configs = new List<CalloutConfig>();
            LoadConfigs();
        }

        public static void LoadConfigs()
        {
            if (Configs.Count > 0) return;

            var manifestJson = LoadResourceFile(GetCurrentResourceName(), "callouts/json_callouts/manifest.json");
            if (string.IsNullOrEmpty(manifestJson))
            {
                Debug.WriteLine("[JsonConfigManager] Could not load manifest.json");
                return;
            }

            List<string> files;
            try
            {
                files = JsonConvert.DeserializeObject<List<string>>(manifestJson);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JsonConfigManager] Failed to parse manifest.json: {ex.Message}");
                return;
            }

            if (files == null || files.Count == 0)
            {
                Debug.WriteLine("[JsonConfigManager] Manifest.json is empty or invalid");
                return;
            }

            foreach (var fileName in files)
            {
                var json = LoadResourceFile(GetCurrentResourceName(), $"callouts/json_callouts/{fileName}");
                if (string.IsNullOrEmpty(json))
                {
                    Debug.WriteLine($"[JsonConfigManager] Could not load {fileName}");
                    continue;
                }

                try
                {
                    var cfg = JsonConvert.DeserializeObject<CalloutConfig>(json);
                    if (cfg != null && !string.IsNullOrEmpty(cfg.shortName))
                    {
                        Configs.Add(cfg);
                        Debug.WriteLine($"[JsonConfigManager] Loaded config: {cfg.shortName}");
                    }
                    else
                    {
                        Debug.WriteLine($"[JsonConfigManager] Invalid config in {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[JsonConfigManager] Failed to parse {fileName}: {ex.Message}");
                }
            }

            foreach (var cfg in Configs)
            {
                if (checkedCallouts.Contains(cfg.shortName))
                    continue;

                if (string.IsNullOrEmpty(cfg.updateURL) || string.IsNullOrEmpty(cfg.version)) continue;

                var argsArray = new object[] { cfg.shortName, cfg.version, cfg.updateURL };

                string payload = JsonConvert.SerializeObject(argsArray);
                int byteLen = Encoding.UTF8.GetBytes(payload).Length;
                checkedCallouts.Add(cfg.shortName);

                BaseScript.TriggerServerEvent("json:checkUpdate", cfg.shortName, cfg.version, cfg.updateURL);
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