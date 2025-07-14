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
        private bool isCalloutFinished = false;

        private List<Ped> spawnedSuspects = new List<Ped>();
        private List<Ped> spawnedVictims = new List<Ped>();

        private Func<Task> suspectMonitorTickHandler;

        public JsonBridge()
        {
            finalLocation = GetRandomNearbyLocation();
            InitInfo(finalLocation);

            ShortName = "json-dynamic";
            CalloutDescription = "Default dynamic scenario.";
            ResponseCode = 2;

            InitBlip(); // Show marker immediately

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
        }

        public override async Task OnAccept()
        {
            // These values will be shown in the callout UI
            ShortName = config.shortName;
            ResponseCode = config.responseCode;
            CalloutDescription = config.description;
            Location = finalLocation;

            UpdateData();

            Debug.WriteLine($"[JsonBridge] Callout accepted with config: {config.shortName}");

            if (config.suspects?.Count > 0)
            {
                spawnedSuspects = await SpawnSuspects.FromConfig(config.suspects);
                if (spawnedSuspects.Count > 0)
                    suspect = spawnedSuspects[0];
            }

            if (config.victims?.Count > 0)
            {
                spawnedVictims = await VictimSpawner.SpawnVictimsAsync(config.victims, finalLocation);
            }
        }

        public override void OnStart(Ped closest)
        {
            base.OnStart(closest);

            Debug.WriteLine("[JsonBridge] Player has arrived on scene.");

            // No spawning or main logic here
            // You could remove the blip or trigger animations/dialogue

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

            Debug.WriteLine("[JsonBridge] Entity cleanup complete.");
        }

        public override void OnCancelAfter()
        {
            // Typically not needed unless you want to clean up later things
        }

        private Vector3 GetRandomNearbyLocation()
        {
            var pos = Game.Player.Character?.Position ?? new Vector3(0f, 0f, 72f);
            var rand = new Random();

            double angle = rand.NextDouble() * Math.PI * 2;
            double distance = rand.Next(20, 60);

            float x = pos.X + (float)(Math.Cos(angle) * distance);
            float y = pos.Y + (float)(Math.Sin(angle) * distance);
            float z = pos.Z;

            return new Vector3(x, y, z);
        }
    }
}
