using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using fivepd_json.Loader;
using fivepd_json.models;

namespace fivepd_json
{
    [CalloutProperties("json-dynamic", "Mega Group", "1.0")]
    public class JsonBridge : Callout
    {
        private CalloutConfig config;
        private Vector3 finalLocation;
        private bool isCalloutFinished = false;

        //private List<SpawnedSuspect> spawnedSuspects = new List<SpawnedSuspect>();
        private List<Ped> spawnedVictims = new List<Ped>();
        private bool suspectsInitialized = false;
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
                heading = 180f,
                pursuit = true
            };

            /*if (config.location != null)
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
            }*/
            Location = finalLocation;

            ShortName = config.shortName;
            CalloutDescription = config.description;
            ResponseCode = config.responseCode;
            StartDistance = 200f;

            Debug.WriteLine($"[JsonBridge] Selected config: {config.shortName}");
        }
    }
}
