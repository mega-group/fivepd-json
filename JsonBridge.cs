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
        private Ped suspect;
        private bool isCalloutFinished = false;

        private List<SpawnedSuspect> spawnedSuspects = new List<SpawnedSuspect>();
        private List<Ped> spawnedVictims = new List<Ped>();

        private Func<Task> suspectMonitorTickHandler;

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
                heading = 180f
            };
            finalLocation = NearbyLocation.GetSafeRandomLocationFarAway();
            Location = finalLocation;

            ShortName = config.shortName;
            CalloutDescription = config.description;
            ResponseCode = config.responseCode;
            StartDistance = 200f;

            Debug.WriteLine($"[JsonBridge] Selected config: {config.shortName}");
        }

        public override async Task OnAccept()
        {
            Debug.WriteLine($"[JsonBridge] Callout Accepted:" +
    $"\n  shortName: {config.shortName}" +
    $"\n  description: {config.description}" +
    $"\n  responseCode: {config.responseCode}" +
    $"\n  weapon: {config.weapon}" +
    $"\n  pedModel: {config.pedModel}" +
    $"\n  behavior: {config.behavior}" +
    $"\n  vehicleModel: {config.vehicleModel}" +
    $"\n  heading: {config.heading}" +
    $"\n  autoEnd: {config.autoEnd}" +
    $"\n  suspects: {(config.suspects != null ? config.suspects.Count.ToString() : "null")}" +
    $"\n  victims: {(config.victims != null ? config.victims.Count.ToString() : "null")}" +
    $"\n  location: {(config.location != null ? $"({config.location.x}, {config.location.y}, {config.location.z})" : "null")}" +
    $"\n  locations: {(config.locations != null ? config.locations.Count.ToString() : "null")}"
);
            // Determine spawnBase with ground height adjustment
            Vector3 baseLoc;

            if (config.location != null)
                baseLoc = new Vector3(config.location.x, config.location.y, config.location.z);
            else if (config.locations != null && config.locations.Count > 0)
            {
                var loc = config.locations[new Random().Next(config.locations.Count)];
                baseLoc = new Vector3(loc.x, loc.y, loc.z);
            }
            else
                baseLoc = NearbyLocation.GetSafeRandomLocationFarAway();

            // Get ground height for Z
            float groundZ = World.GetGroundHeight(baseLoc);
            var spawnBase = new Vector3(baseLoc.X, baseLoc.Y, groundZ);

            // Update circle on map to correct location
            ShortName = config.shortName;
            Location = spawnBase;
            UpdateData();
            InitBlip();

            // Spawn suspects near spawnBase
            if (config.suspects != null && config.suspects.Count > 0)
            {
                spawnedSuspects = await SpawnSuspects.FromConfig(config.suspects, spawnBase);
                if (spawnedSuspects.Count > 0)
                    suspect = spawnedSuspects[0].Ped;
            }
            else if (!string.IsNullOrEmpty(config.pedModel))
            {
                var singleSuspect = await SpawnSuspects.SpawnSingleSuspect(config, spawnBase);
                if (singleSuspect != null)
                {
                    spawnedSuspects.Add(singleSuspect);
                    suspect = singleSuspect.Ped;
                }
            }

            // Spawn victims near spawnBase as well
            if (config.victims?.Count > 0)
            {
                spawnedVictims = await VictimSpawner.SpawnVictimsAsync(config.victims, spawnBase);
            }
        }


        public override void OnStart(Ped closest)
        {
            base.OnStart(closest);

            if (closest.NetworkId != Game.PlayerPed.NetworkId)
            {
                this.AssignedPlayers.Add(closest);
            }

            Debug.WriteLine("[JsonBridge] Player has arrived on scene.");

            // Activate behavior now that player is on scene
            foreach (var s in spawnedSuspects)
            {
                if (!string.IsNullOrEmpty(s.Behavior))
                {
                    SuspectBehavior.HandleBehavior(s.Ped, s.Behavior);
                }
            }

            /*if (config.autoEnd && suspect != null)
            {
                suspectMonitorTickHandler = async () =>
                {
                    await SuspectMonitor.MonitorAsync(
                        suspect,
                        () => isCalloutFinished,
                        () => isCalloutFinished = true,
                        EndCallout
                    );
                };

                Tick += suspectMonitorTickHandler;
            }*/
        }


        public override void OnCancelBefore()
        {
            base.OnCancelBefore();
            Debug.WriteLine("[JsonBridge] Cleaning up all entities.");

            foreach (var s in spawnedSuspects)
            {
                if (s.Ped.Exists()) s.Ped.Delete();
                if (s.Vehicle != null && s.Vehicle.Exists()) s.Vehicle.Delete();
            }

            foreach (var v in spawnedVictims)
            {
                if (v.Exists()) v.Delete();
            }

            if (suspectMonitorTickHandler != null)
            {
                Tick -= suspectMonitorTickHandler;
                suspectMonitorTickHandler = null;
            }

            Debug.WriteLine("[JsonBridge] Entity cleanup complete.");
        }

        public override void OnCancelAfter()
        {
            // Might use this later for additional cleanup if needed
        }
    }
}
