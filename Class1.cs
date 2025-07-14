using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace fivepd.json
{
        public class CalloutConfig
        {
            public string shortName { get; set; }
            public string description { get; set; }

            public int responseCode { get; set; }
            public string weapon { get; set; }
            public string pedModel { get; set; }
            public string behavior { get; set; }
            public string vehicleModel { get; set; }
            public float heading { get; set; }
            public bool autoEnd { get; set; } = true; // optional
        public List<SuspectConfig> suspects { get; set; }
        public List<VictimConfig> victims { get; set; }

    }
    public class SuspectConfig
    {
        public string pedModel { get; set; }
        public string weapon { get; set; }
        public string vehicleModel { get; set; }
        public float heading { get; set; } = 0f;
        public string behavior { get; set; } // fight, driveby, flee, etc
    }

    public class VictimConfig
    {
        public string pedModel { get; set; }
        public string dialogue { get; set; }
        public string behavior { get; set; } // cower, idle, etc.
    }

    // Static manager to load JSON configs once and provide random config
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


    [CalloutProperties("json-dynamic", "Mega Group", "1.0")]
    public class JsonBridge : Callout
    {
        private CalloutConfig config;
        private Vector3 finalLocation;
        private Ped suspect;
        private Vehicle suspectVehicle;
        private bool isCalloutFinished = false;

        public JsonBridge()
        {
            InitInfo(Location);
            ShortName = "json-dynamic";
            CalloutDescription = "Default dynamic scenario.";
            ResponseCode = 2;

            finalLocation = GetRandomNearbyLocation();
            Location = finalLocation;

            // Safe: Configs already loaded once by static constructor
            config = JsonConfigManager.GetRandomConfig() ?? new CalloutConfig
            {
                shortName = "json-dynamic",
                description = "Default fallback scenario",
                responseCode = 3,
                pedModel = "a_m_y_skater_01",
                weapon = "WEAPON_PISTOL",
                behavior = "fight",
                vehicleModel = "SULTAN",
                heading = 180f
            };

            Debug.WriteLine($"[JsonBridge] Selected config: {config.shortName}");
            Debug.WriteLine($"[JsonBridge] Selected config DATA: {config.shortName}, ${config.description}, ${config.responseCode}, {config.pedModel}, {config.weapon}, {config.behavior}, {config.vehicleModel}, {config.heading}");
        }

        public override async Task OnAccept()
        {
            base.InitBlip();

            Debug.WriteLine($"[JsonBridge] Using config: {config.shortName}");
            Debug.WriteLine($"[JsonBridge] Using config DATA: {config.shortName}, {config.description}, {config.responseCode}, {config.pedModel}, {config.weapon}, {config.behavior}, {config.vehicleModel}, {config.heading}");

            this.ShortName = config.shortName;
            this.ResponseCode = config.responseCode;
            this.CalloutDescription = config.description;
            this.Location = finalLocation;

            UpdateData();

            if (config.suspects?.Count > 0)
                await SpawnSuspects(config.suspects);

            if (config.victims?.Count > 0)
                await SpawnVictims(config.victims);
        }

        private List<Ped> spawnedSuspects = new List<Ped>();
        private List<Ped> spawnedVictims = new List<Ped>();

        private async Task SpawnSuspects(List<SuspectConfig> configs)
        {
            foreach (var cfg in configs)
            {
                var pedModel = new Model(cfg.pedModel ?? GetRandomPedModel());
                await pedModel.Request(3000);
                if (!pedModel.IsLoaded) continue;

                var ped = await SpawnPed(pedModel, GetRandomNearbyLocation());
                spawnedSuspects.Add(ped);

                ped.BlockPermanentEvents = true;
                ped.AlwaysKeepTask = true;
                ped.AttachBlip();

                if (!string.IsNullOrEmpty(cfg.weapon))
                {
                    var weaponHash = (WeaponHash)GetHashKey(cfg.weapon);
                    ped.Weapons.Give(weaponHash, 60, true, true);
                }

                if (!string.IsNullOrEmpty(cfg.vehicleModel))
                {
                    var vehicleModel = new Model(cfg.vehicleModel);
                    await vehicleModel.Request(3000);
                    if (vehicleModel.IsLoaded)
                    {
                        var vehicle = await SpawnVehicle(vehicleModel, ped.Position, cfg.heading);
                        Utilities.ExcludeVehicleFromTrafficStop(vehicle.NetworkId, true);
                        ped.SetIntoVehicle(vehicle, VehicleSeat.Driver);
                    }
                }

                HandleSuspectBehavior(ped, cfg.behavior);
            }
        }
        private void HandleSuspectBehavior(Ped ped, string behavior, Ped target = null)
        {
            var playerPed = target ?? Game.PlayerPed;

            switch ((behavior ?? "").ToLower())
            {
                case "fight":
                    ped.Task.FightAgainst(playerPed);
                    break;

                case "flee":
                    ped.Task.FleeFrom(playerPed);
                    break;

                case "driveby":
                    API.SetPedAsEnemy(ped.Handle, true);
                    API.GiveWeaponToPed(ped.Handle, (uint)API.GetHashKey("WEAPON_PISTOL"), 100, false, true);

                    if (ped.IsInVehicle())
                    {
                        int vehicle = API.GetVehiclePedIsIn(ped.Handle, false);
                        if (API.GetPedInVehicleSeat(vehicle, -1) != ped.Handle)
                        {
                            API.TaskWarpPedIntoVehicle(ped.Handle, vehicle, -1); // Driver seat
                        }

                        uint firingPattern = (uint)API.GetHashKey("FIRING_PATTERN_BURST_FIRE_DRIVEBY");

                        API.TaskDriveBy(
                            ped.Handle,
                            playerPed.Handle,
                            0,
                            0f, 0f, 0f,
                            15f,
                            100,
                            false,
                            firingPattern
                        );
                    }
                    else
                    {
                        ped.Task.ShootAt(playerPed, 10000);
                    }
                    break;

                default:
                    break;
            }
        }


        private async Task SpawnVictims(List<VictimConfig> configs)
        {
            foreach (var cfg in configs)
            {
                var pedModel = new Model(cfg.pedModel ?? "a_m_y_skater_01");
                await pedModel.Request(3000);
                if (!pedModel.IsLoaded) continue;

                var ped = await SpawnPed(pedModel, GetRandomNearbyLocation());
                spawnedVictims.Add(ped);

                ped.BlockPermanentEvents = true;
                ped.AlwaysKeepTask = true;
                ped.AttachBlip();

                if (cfg.behavior?.ToLower() == "cower")
                {
                    ped.Task.Cower(-1);
                }
            }
        }
        private async Task MonitorSuspectState()
        {
            if (isCalloutFinished || suspect == null) return;

            if (!isCalloutFinished && (suspect.IsDead || suspect.IsCuffed))
            {
                isCalloutFinished = true;
                Debug.WriteLine("[JsonBridge] Auto-ending callout due to suspect state.");
                EndCallout();
            }

            await Task.FromResult(0);
        }

        public override void OnStart(Ped closest)
        {
            base.OnStart(closest);

            Debug.WriteLine("[JsonBridge] Callout started by player");

            if (config.autoEnd)
            {
                Tick += MonitorSuspectState;
            }
        }

        private Vector3 GetRandomNearbyLocation()
        {
            var pos = Game.Player.Character?.Position ?? new Vector3(0f, 0f, 72f);
            var rand = new Random();
            return new Vector3(
                pos.X + rand.Next(100, 500),
                pos.Y + rand.Next(100, 500),
                pos.Z
            );
        }
        
        private string GetRandomPedModel()
        {
            string[] models = {
                "a_m_y_skater_01", "a_m_y_stlat_01", "a_m_m_business_01", "g_m_y_mexgang_01"
            };
            return models[new Random().Next(models.Length)];
        }

        public override void OnCancelBefore()
        {
            base.OnCancelBefore();
            Debug.WriteLine("[JsonBridge] Cleaning up all entities.");

            foreach (var ped in spawnedSuspects)
                if (ped.Exists()) ped.Delete();

            foreach (var ped in spawnedVictims)
                if (ped.Exists()) ped.Delete();

            Tick -= MonitorSuspectState;
        }

        public override void OnCancelAfter()
        {
            // most of the spawned entities are null at this point
        }
    }
}
