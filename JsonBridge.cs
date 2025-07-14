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

namespace fivepd.json
{
    [CalloutProperties("json-dynamic", "Mega Group", "1.0")]
    public class JsonBridge : Callout
    {
        private CalloutConfig config;
        private Vector3 finalLocation;
        private Ped suspect;
        private Vehicle suspectVehicle;
        private bool isCalloutFinished = false;

        private List<Ped> spawnedSuspects = new List<Ped>();
        private List<Ped> spawnedVictims = new List<Ped>();

        public JsonBridge()
        {
            InitInfo(Location);
            ShortName = "json-dynamic";
            CalloutDescription = "Default dynamic scenario.";
            ResponseCode = 2;

            finalLocation = GetRandomNearbyLocation();
            Location = finalLocation;

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
            Debug.WriteLine($"[JsonBridge] Selected config DATA: {config.shortName}, {config.description}, {config.responseCode}, {config.pedModel}, {config.weapon}, {config.behavior}, {config.vehicleModel}, {config.heading}");
        }

        public override async Task OnAccept()
        {
            base.InitBlip();

            Debug.WriteLine($"[JsonBridge] Using config: {config.shortName}");
            Debug.WriteLine($"[JsonBridge] Using config DATA: {config.shortName}, {config.description}, {config.responseCode}, {config.pedModel}, {config.weapon}, {config.behavior}, {config.vehicleModel}, {config.heading}");

            ShortName = config.shortName;
            ResponseCode = config.responseCode;
            CalloutDescription = config.description;
            Location = finalLocation;

            UpdateData();

            if (config.suspects?.Count > 0)
            {
                // Use the Logic.SpawnSuspects class to spawn suspects
                spawnedSuspects = await SpawnSuspects.FromConfig(config.suspects);

                // Assign main suspect for monitoring, e.g., the first spawned
                if (spawnedSuspects.Count > 0)
                    suspect = spawnedSuspects[0];
            }

            if (config.victims?.Count > 0)
            {
                spawnedVictims = await VictimSpawner.SpawnVictimsAsync(config.victims, finalLocation);
            }
        }

        private Func<Task> suspectMonitorTickHandler;

        public override void OnStart(Ped closest)
        {
            base.OnStart(closest);

            Debug.WriteLine("[JsonBridge] Callout started by player");

            if (config.autoEnd)
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

        public override void OnCancelBefore()
        {
            base.OnCancelBefore();
            Debug.WriteLine("[JsonBridge] Cleaning up all entities.");

            foreach (var ped in spawnedSuspects)
                if (ped.Exists()) ped.Delete();

            foreach (var ped in spawnedVictims)
                if (ped.Exists()) ped.Delete();

            if (suspectMonitorTickHandler != null)
            {
                Tick -= suspectMonitorTickHandler;
                suspectMonitorTickHandler = null;
            }
            Debug.WriteLine("[JsonBridge] Cleaning up all entities has completed.");
        }

        public override void OnCancelAfter()
        {
            // most of the spawned entities are null at this point
        }
    }
}
