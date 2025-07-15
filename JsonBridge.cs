using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using fivepd_json.Behavior;
using fivepd_json.Helpers;
using fivepd_json.Loader;
using fivepd_json.Logic;
using fivepd_json.models;
using static CitizenFX.Core.Native.API;
using static fivepd_json.Logic.SpawnSuspects;

namespace fivepd.json
{
    [CalloutProperties("json-dynamic", "Mega Group", "1.0")]
    public class JsonBridge : Callout
    {
        private CalloutConfig config;
        private Vector3 finalLocation;
        private bool isCalloutFinished = false;

        private List<SpawnedSuspect> spawnedSuspects = new List<SpawnedSuspect>();
        private List<Ped> spawnedVictims = new List<Ped>();

        public JsonBridge()
        {
            config = JsonConfigManager.GetRandomConfig() ?? new CalloutConfig
            {
                shortName = "json-dynamic",
                description = "Default fallback scenario",
                responseCode = 3,
                pedModel = "a_m_y_skater_01",
                weapon = "WEAPON_PISTOL",
                behavior = "fight",
                vehicleModel = "SULTAN",
                heading = 180f,
                pursuit = true
            };

            if (config.location != null)
            {
                finalLocation = new Vector3(config.location.x, config.location.y, config.location.z);
            }
            else if (config.locations != null && config.locations.Count > 0)
            {
                var loc = config.locations[new Random().Next(config.locations.Count)];
                finalLocation = new Vector3(loc.x, loc.y, loc.z);
            }
            else
            {
                finalLocation = NearbyLocation.GetSafeRandomLocationFarAway();
            }
            Location = finalLocation;

            ShortName = config.shortName;
            CalloutDescription = config.description;
            ResponseCode = config.responseCode;
            StartDistance = 200f;

            Debug.WriteLine($"[JsonBridge] Selected config: {config.shortName}");
        }

        public override async Task OnAccept()
        {
            spawnedSuspects.Clear();
            spawnedVictims.Clear();

            Debug.WriteLine("[JsonBridge] Callout Accepted:" +
                $"\n  shortName: {config.shortName}" +
                $"\n  description: {config.description}" +
                $"\n  responseCode: {config.responseCode}" +
                $"\n  weapon: {config.weapon}" +
                $"\n  pedModel: {config.pedModel}" +
                $"\n  behavior: {config.behavior}" +
                $"\n  vehicleModel: {config.vehicleModel}" +
                $"\n  heading: {config.heading}" +
                $"\n  autoEnd: {config.autoEnd}" +
                $"\n  pursuit: {config.pursuit}" +
                $"\n  suspects: {(config.suspects?.Count.ToString() ?? "null")}" +
                $"\n  victims: {(config.victims?.Count.ToString() ?? "null")}" +
                $"\n  location: {(config.location != null ? $"({config.location.x}, {config.location.y}, {config.location.z})" : "null")}" +
                $"\n  locations: {(config.locations?.Count.ToString() ?? "null")}"
            );

            float groundZ = World.GetGroundHeight(finalLocation);
            var spawnBase = new Vector3(finalLocation.X, finalLocation.Y, groundZ);

            InitBlip();

            try
            {
                if (config.suspects != null && config.suspects.Count > 0)
                {
                    spawnedSuspects = await SpawnSuspects.FromConfig(config.suspects, spawnBase);
                }
                else if (!string.IsNullOrEmpty(config.pedModel))
                {
                    var singleSuspect = await SpawnSuspects.SpawnSingleSuspect(config, spawnBase);
                    if (singleSuspect != null)
                        spawnedSuspects.Add(singleSuspect);
                }

                if (spawnedSuspects.Count == 0)
                {
                    Debug.WriteLine("[JsonBridge] ❌ No suspects spawned!");
                    return;
                }

                if (config.victims != null && config.victims.Count > 0)
                {
                    spawnedVictims = await VictimSpawner.SpawnVictimsAsync(config.victims, spawnBase);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JsonBridge] Exception during OnAccept spawn: {ex}");
            }
        }

        public override async void OnStart(Ped player)
        {
            base.OnStart(player);
            Debug.WriteLine("[JsonBridge] Player has arrived on scene.");

            if (spawnedSuspects == null || spawnedSuspects.Count == 0)
            {
                Debug.WriteLine("[JsonBridge] ❌ No suspects to process.");
                return;
            }

            foreach (var s in spawnedSuspects)
            {
                try
                {
                    if (s?.Ped != null && s.Ped.Exists() && !string.IsNullOrEmpty(s.Behavior))
                    {
                        SuspectBehavior.HandleBehavior(s.Ped, s.Behavior);
                        Debug.WriteLine($"[JsonBridge] Ped {s.Ped.Handle} spawned with behavior: {s.Behavior}");

                        if (config.pursuit)
                        {
                            var pursuit = Pursuit.RegisterPursuit(s.Ped);
                            Debug.WriteLine($"[JsonBridge] 🚓 Registered pursuit for ped {s.Ped.Handle}");
                            await BaseScript.Delay(100); // small delay between pursuits
                        }
                    }
                    else
                    {
                        Debug.WriteLine("[JsonBridge] Skipping suspect behavior — invalid Ped or behavior.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[JsonBridge] Exception in OnStart suspect handling: {ex}");
                }
            }
        }

        public override void OnCancelBefore()
        {
            base.OnCancelBefore();
            Debug.WriteLine("[JsonBridge] Cleaning up all entities.");

            try
            {
                foreach (var s in spawnedSuspects)
                {
                    if (s?.Ped != null && s.Ped.Exists())
                        s.Ped.Delete();
                    if (s?.Vehicle != null && s.Vehicle.Exists())
                        s.Vehicle.Delete();
                }

                foreach (var v in spawnedVictims)
                {
                    if (v != null && v.Exists())
                        v.Delete();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JsonBridge] Exception during cleanup: {ex}");
            }

            isCalloutFinished = true;
            Debug.WriteLine("[JsonBridge] Entity cleanup complete.");
        }

        public override void OnCancelAfter()
        {
            // additional cleanup if needed
        }

        public override void OnBackupReceived(Player player)
        {
            // optional backup logic
        }

        public override void OnBackupCalled(int code)
        {
            // optional backup called logic
        }

        public override void OnPlayerRevokedBackup(Player player)
        {
            OnCancelBefore(); // cleanup when backup revoked
        }
    }
}
