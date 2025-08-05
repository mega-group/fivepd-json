using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using fivepd_json.models;
using fivepd_json.Helpers;

namespace fivepd_json.Logic
{
    public static class VictimSpawner
    {
        public static async Task<List<Ped>> SpawnVictimsAsync(List<VictimConfig> configs, Vector3 origin)
        {
            List<Ped> victims = new List<Ped>();

            foreach (var cfg in configs)
            {
                var pedModel = new Model(cfg.pedModel ?? "a_m_y_skater_01");
                await pedModel.Request(3000);
                if (!pedModel.IsLoaded) continue;

                var spawnPos = NearbyLocation.GetRandomNearbyLocation(origin);
                var ped = await World.CreatePed(pedModel, spawnPos);
                DebugHelper.Log("Spawned Victim: " + (ped != null ? ped.Handle.ToString() : "null"));
                if (ped == null) continue;

                victims.Add(ped);

                ped.BlockPermanentEvents = true;
                ped.AlwaysKeepTask = true;
                ped.AttachBlip();

                if (cfg.behavior?.ToLower() == "cower")
                {
                    ped.Task.Cower(-1);
                }
            }

            return victims;
        }
    }
}