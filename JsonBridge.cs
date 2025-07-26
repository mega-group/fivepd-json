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

            // Get ground height for Z
            float groundZ = World.GetGroundHeight(finalLocation);
            var spawnBase = new Vector3(finalLocation.X, finalLocation.Y, groundZ);

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

        private async Task SuspectMonitorTick()
        {
            await SuspectMonitor.MonitorAsync(
                suspect,
                () => isCalloutFinished,
                () => isCalloutFinished = true,
                EndCallout
            );
        }

        public override void OnStart(Ped closest)
{
    base.OnStart(closest);

    // Prevent FivePD internal crash
    if (!AssignedPlayers.Contains(Game.Player))
    {
        AssignedPlayers.Add(Game.Player);
    }

    Debug.WriteLine("[JsonBridge] OnStart triggered.");

    if (!suspectsInitialized)
    {
        Debug.WriteLine("[JsonBridge] suspects not initialized, skipping OnStart logic.");
        return;
    }

    foreach (var s in spawnedSuspects)
    {
        if (s?.Ped != null && s.Ped.Exists())
        {
            try
            {
                Debug.WriteLine($"[JsonBridge] Applying behavior {s.Behavior}");
                SuspectBehavior.HandleBehavior(s.Ped, s.Behavior);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JsonBridge] Error in HandleBehavior: {ex}");
            }
        }
        else
        {
            Debug.WriteLine("[JsonBridge] Suspect Ped is null or does not exist.");
        }
    }

    if (config.autoEnd && suspect != null)
    {
        suspectMonitorTickHandler = SuspectMonitorTick;
        Tick += suspectMonitorTickHandler;
    }
}
        public override void OnBackupReceived(Player player)
        {
            // FOR LATER USE
        }
        public override void OnBackupCalled(int code)
        {
            // event logic
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
        public override void OnPlayerRevokedBackup(Player player)
        {
            foreach (var s in spawnedSuspects)
            {
                if (s.Ped.Exists()) s.Ped.Delete();
                if (s.Vehicle != null && s.Vehicle.Exists()) s.Vehicle.Delete();
            }

            foreach (var v in spawnedVictims)
            {
                if (v.Exists()) v.Delete();
            }
        }
    }
}
