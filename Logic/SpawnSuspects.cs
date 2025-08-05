using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using fivepd_json.Helpers;
using fivepd_json.models;

namespace fivepd_json.Logic
{
    public static class SpawnSuspects
    {
        public class SpawnedSuspect
        {
            public Ped Ped { get; set; }
            public string Behavior { get; set; }
            public Vehicle Vehicle { get; set; }

            public string Weapon { get; set; }
        }

        public static async Task<List<SpawnedSuspect>> FromConfig(List<SuspectConfig> configs, Vector3 spawnCenter)
        {
            var suspects = new List<SpawnedSuspect>();
            var rand = new Random();

            foreach (var cfg in configs)
            {
                // Generate small random offset within ~5 meters
                var angle = rand.NextDouble() * 2 * Math.PI;
                var distance = rand.NextDouble() * 5.0; // 0–5 meters
                var offsetX = Math.Cos(angle) * distance;
                var offsetY = Math.Sin(angle) * distance;

                var spawnPos = new Vector3(spawnCenter.X + (float)offsetX, spawnCenter.Y + (float)offsetY, spawnCenter.Z);
                float groundZ = World.GetGroundHeight(spawnPos);
                spawnPos = new Vector3(spawnPos.X, spawnPos.Y, groundZ);

                var result = await CreatePedWithConfig(cfg, spawnPos);
                if (result != null) suspects.Add(result);
            }

            return suspects;
        }

        public static async Task<SpawnedSuspect> SpawnSingleSuspect(CalloutConfig config, Vector3 spawnLocation)
        {
            var cfg = new SuspectConfig
            {
                pedModel = string.IsNullOrEmpty(config.pedModel) ? GetRandomPedModel() : config.pedModel,
                weapon = string.IsNullOrEmpty(config.weapon) ? "WEAPON_PISTOL" : config.weapon,
                vehicleModel = string.IsNullOrEmpty(config.vehicleModel) ? "SULTAN" : config.vehicleModel,
                heading = config.heading,
                behavior = string.IsNullOrEmpty(config.behavior) ? "idle" : config.behavior
            };

            return await CreatePedWithConfig(cfg, spawnLocation);
        }


        private static async Task<SpawnedSuspect> CreatePedWithConfig(SuspectConfig cfg, Vector3 position)
        {
            Vehicle vehicle = null;
            var pedModel = new Model(cfg.pedModel ?? GetRandomPedModel());
            await pedModel.Request(3000);
            if (!pedModel.IsLoaded) return null;

            var ped = await World.CreatePed(pedModel, position);
            if (ped == null) return null;

            ped.BlockPermanentEvents = true;
            ped.IsPersistent = true;
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
                    vehicle = await Utilities.SpawnVehicle(vehicleModel, ped.Position, cfg.heading);
                    Utilities.ExcludeVehicleFromTrafficStop(vehicle.NetworkId, true);
                    ped.SetIntoVehicle(vehicle, VehicleSeat.Driver);
                }
            }

            return new SpawnedSuspect
            {
                Ped = ped,
                Behavior = cfg.behavior,
                Vehicle = vehicle
            };
        }

        private static string GetRandomPedModel()
        {
            string[] models =
            {
                "a_m_y_skater_01",
                "a_m_y_stlat_01",
                "a_m_m_business_01",
                "g_m_y_mexgang_01"
            };
            return models[new Random().Next(models.Length)];
        }
    }
}