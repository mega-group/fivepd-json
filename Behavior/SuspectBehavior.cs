using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;

namespace fivepd_json.Behavior
{
    public static class SuspectBehavior
    {
        public static void HandleBehavior(Ped ped, string behavior, Ped target = null)
        {
            if (ped == null || !ped.Exists())
            {
                Debug.WriteLine("[SuspectBehavior] ❌ Suspect ped is null or doesn't exist.");
                return;
            }

            Ped playerPed = target;

            if (playerPed == null || !playerPed.Exists())
            {
                try
                {
                    playerPed = Game.PlayerPed;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuspectBehavior] ❌ Failed to access Game.PlayerPed: {ex.Message}");
                    return;
                }
            }

            if (playerPed == null || !playerPed.Exists())
            {
                Debug.WriteLine("[SuspectBehavior] ❌ playerPed is still null or does not exist.");
                return;
            }

            Debug.WriteLine($"[JsonBridge] Handling behavior '{behavior}' for ped {ped.Handle} (target: {playerPed.Handle})");

            switch ((behavior ?? "").ToLower())
            {
                case "fight":
                    ped.Task.FightAgainst(playerPed);
                    break;

                case "flee":
                    ped.Task.FleeFrom(playerPed);
                    break;

                case "flee&shoot":
                    API.GiveWeaponToPed(ped.Handle, (uint)API.GetHashKey("WEAPON_PISTOL"), 100, false, true);
                    ped.Task.FleeFrom(playerPed);

                    if (ped.IsInVehicle())
                    {
                        API.TaskVehicleShootAtPed(ped.Handle, playerPed.Handle, 3.0f);
                    }
                    else
                    {
                        ped.Task.ShootAt(playerPed);
                    }
                    break;

                case "random":
                    Debug.WriteLine("[SuspectBehavior] Behavior 'random' is not implemented.");
                    break;

                default:
                    Debug.WriteLine($"[SuspectBehavior] ⚠️ Unknown behavior: {behavior}");
                    break;
            }
        }

    }
}