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
        public static async Task<List<Ped>> FromConfig(List<SuspectConfig> configs)
        {
            List<Ped> suspects = new List<Ped>();

            foreach (var cfg in configs)
            {
                var pedModel = new Model(cfg.pedModel ?? GetRandomPedModel());
                await pedModel.Request(3000);
                if (!pedModel.IsLoaded) continue;

                var ped = await World.CreatePed(pedModel, NearbyLocation.GetRandomNearbyLocation());
                suspects.Add(ped);

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

                SuspectBehavior.HandleBehavior(ped, cfg.behavior);
            }

            return suspects;
        }

        private static string GetRandomPedModel()
        {
            string[] models = {
                "a_m_y_skater_01", "a_m_y_stlat_01", "a_m_m_business_01", "g_m_y_mexgang_01"
            };
            return models[new System.Random().Next(models.Length)];
        }
    }
}
