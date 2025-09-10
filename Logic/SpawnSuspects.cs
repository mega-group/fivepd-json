using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using FivePD.API.Utils;
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
            public bool pursuit { get; set; }
            public List<PedQuestionConfig> Questions { get; set; }
            public List<PedDataConfig> PedData { get; set; }
            public List<VehicleDataConfig> VehicleData { get; set; }
            public Blip PedBlip { get; set; }
            public Blip VehBlip { get; set; }
        }

        public static async Task<List<SpawnedSuspect>> FromConfig(List<SuspectConfig> configs, Vector3 spawnCenter)
        {
            var suspects = new List<SpawnedSuspect>();
            var rand = new Random();

            foreach (var cfg in configs)
            {
                var angle = rand.NextDouble() * 2 * Math.PI;
                var distance = rand.NextDouble() * 5.0;
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
                behavior = string.IsNullOrEmpty(config.behavior) ? "idle" : config.behavior,
                questions = config.questions
            };

            return await CreatePedWithConfig(cfg, spawnLocation);
        }


        private static async Task<SpawnedSuspect> CreatePedWithConfig(SuspectConfig cfg, Vector3 position, Dictionary<string, Vehicle> sharedVehicles = null)
        {

            var pedModel = new Model(cfg.pedModel ?? GetRandomPedModel());
            await pedModel.Request(3000);
            if (!pedModel.IsLoaded) return null;

            var ped = await World.CreatePed(pedModel, position);
            if (ped == null) return null;

            ped.BlockPermanentEvents = true;
            ped.IsPersistent = true;
            ped.AlwaysKeepTask = true;

            Blip pedBlip = null;
            Blip vehBlip = null;

            if (cfg.hasBlip == true && cfg.pursuit == false)
            {
                pedBlip = ped.AttachBlip();
                pedBlip.Color = (BlipColor)Enum.Parse(typeof(BlipColor), cfg.blipColor, true);
                //pedBlip.IsShortRange = true;
                pedBlip.Name = "Suspect";
            }

            if (!string.IsNullOrEmpty(cfg.weapon))
            {
                var weaponHash = (WeaponHash)API.GetHashKey(cfg.weapon);
                ped.Weapons.Give(weaponHash, 60, true, true);
            }

            Vehicle vehicle = null;

            if (!string.IsNullOrEmpty(cfg.vehicleId) && sharedVehicles?.ContainsKey(cfg.vehicleId) == true)
            {
                vehicle = sharedVehicles[cfg.vehicleId];
            }
            else
            {
                string modelName = cfg.vehicleModel;

                if (string.Equals(modelName, "random", StringComparison.OrdinalIgnoreCase))
                {
                    modelName = GetRandomVehicleModel();
                }

                if (!string.IsNullOrEmpty(modelName))
                {
                    var vehicleModel = new Model(modelName);
                    await vehicleModel.Request(3000);

                    if (vehicleModel.IsLoaded)
                    {
                        vehicle = await Utilities.SpawnVehicle(vehicleModel, ped.Position, cfg.heading);
                        
                        if (cfg.vehiclehasBlip == true && cfg.pursuit == false) 
                        {
                            vehBlip = vehicle.AttachBlip();
                            vehBlip.Color = BlipColor.Red;
                            vehBlip.Name = "Suspect Vehicle";
                        }
                        bool exclude = cfg.excludeFromTrafficStop ?? true;
                        Utilities.ExcludeVehicleFromTrafficStop(vehicle.NetworkId, exclude);
                    }
                }
            }

            if (vehicle != null)
            {
                VehicleSeat targetSeat = cfg.seatIndex.HasValue ? (VehicleSeat)cfg.seatIndex.Value : VehicleSeat.Any;

                if (targetSeat != VehicleSeat.Any && vehicle.IsSeatFree(targetSeat))
                {
                    ped.SetIntoVehicle(vehicle, targetSeat);
                }
                else
                {
                    VehicleSeat[] fallbackSeats = {
            VehicleSeat.RightFront,
            VehicleSeat.LeftRear,
            VehicleSeat.RightRear,
            VehicleSeat.ExtraSeat1,
            VehicleSeat.ExtraSeat2,
            VehicleSeat.ExtraSeat3,
            VehicleSeat.ExtraSeat4
        };

                    bool assigned = false;
                    foreach (var seat in fallbackSeats)
                    {
                        if (vehicle.IsSeatFree(seat))
                        {
                            ped.SetIntoVehicle(vehicle, seat);
                            assigned = true;
                            break;
                        }
                    }

                    if (!assigned)
                    {
                        DebugHelper.Log($"No available seats for ped {ped.Handle} in vehicle {vehicle.Handle}", "WARN");
                    }
                }
            }

            return new SpawnedSuspect
            {
                Ped = ped,
                Behavior = cfg.behavior,
                Vehicle = vehicle,
                Questions = cfg.questions,
                PedData = cfg.PedData != null ? new List<PedDataConfig> { cfg.PedData } : null,
                VehicleData = cfg.vehicleData != null ? new List<VehicleDataConfig> { cfg.vehicleData } : null,
                pursuit = cfg.pursuit,
                PedBlip = pedBlip,
                VehBlip = vehBlip
            };
        }


        private static string GetRandomPedModel()
        {
            PedHash pedHash = RandomUtils.GetRandomPed();
            return pedHash.ToString();
        }
        private static string GetRandomVehicleModel()
        {
            VehicleHash vehicleHash = RandomUtils.GetRandomVehicle();
            return vehicleHash.ToString();
        }
    }
}