using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using fivepd_json.Behavior;
using fivepd_json.Helpers;
using fivepd_json.models;

namespace fivepd_json.Logic
{
    public static class SpawnSuspects
    {
        public static Dictionary<Ped, string> BehaviorMap = new Dictionary<Ped, string>();
        public static async Task<List<SpawnedSuspect>> FromConfig(List<SuspectConfig> configs, Vector3 spawnLocation)
        {
            var suspects = new List<SpawnedSuspect>();

            foreach (var cfg in configs)
            {
                var pedModel = new Model(cfg.pedModel ?? GetRandomPedModel());
                await pedModel.Request(3000);
                if (!pedModel.IsLoaded) continue;

                var ped = await World.CreatePed(pedModel, NearbyLocation.GetRandomNearbyLocation(spawnLocation));
                ped.BlockPermanentEvents = true;
                ped.AlwaysKeepTask = true;
                ped.AttachBlip();

                if (!string.IsNullOrEmpty(cfg.weapon))
                {
                    var weaponHash = (WeaponHash)API.GetHashKey(cfg.weapon);
                    ped.Weapons.Give(weaponHash, 60, true, true);
                }

                if (!string.IsNullOrEmpty(cfg.vehicleModel))
                {
                    var vehicleModel = new Model(cfg.vehicleModel);
                    await vehicleModel.Request(3000);
                    if (vehicleModel.IsLoaded)
                    {
                        var vehicle = await Utilities.SpawnVehicle(vehicleModel, ped.Position, cfg.heading);
                        Utilities.ExcludeVehicleFromTrafficStop(vehicle.NetworkId, true);
                        ped.SetIntoVehicle(vehicle, VehicleSeat.Driver);
                    }
                }

                suspects.Add(new SpawnedSuspect { Ped = ped, Behavior = cfg.behavior });
            }

            return suspects;
        }

        public class SpawnedSuspect
        {
            public Ped Ped { get; set; }
            public string Behavior { get; set; }
        }

        private static string GetRandomPedModel()
        {
            string[] models = {
                "a_m_y_skater_01", "a_m_y_stlat_01", "a_m_m_business_01", "g_m_y_mexgang_01"
            };
            return models[new System.Random().Next(models.Length)];
        }
        public static async Task<SpawnedSuspect> SpawnSingleSuspect(CalloutConfig config, Vector3 spawnLocation)
        {
            var pedModel = new Model(config.pedModel ?? GetRandomPedModel());
            await pedModel.Request(3000);
            if (!pedModel.IsLoaded) return null;

            var ped = await World.CreatePed(pedModel, spawnLocation);

            ped.BlockPermanentEvents = true;
            ped.AlwaysKeepTask = true;
            ped.AttachBlip();

            if (!string.IsNullOrEmpty(config.weapon))
            {
                var weaponHash = (WeaponHash)API.GetHashKey(config.weapon);
                ped.Weapons.Give(weaponHash, 60, true, true);
            }

            if (!string.IsNullOrEmpty(config.vehicleModel))
            {
                var vehicleModel = new Model(config.vehicleModel);
                await vehicleModel.Request(3000);
                if (vehicleModel.IsLoaded)
                {
                    var vehicle = await Utilities.SpawnVehicle(vehicleModel, ped.Position, config.heading);
                    Utilities.ExcludeVehicleFromTrafficStop(vehicle.NetworkId, true);
                    ped.SetIntoVehicle(vehicle, VehicleSeat.Driver);
                }
            }

            return new SpawnedSuspect
            {
                Ped = ped,
                Behavior = config.behavior
            };
        }

    }
}
