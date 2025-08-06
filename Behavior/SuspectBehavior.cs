using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using fivepd_json.Helpers;

namespace fivepd_json.Behavior
{
    public static class SuspectBehavior
    {
        public static void HandleBehavior(Ped ped, string behavior, Ped target = null)
        {
            if (ped == null || !ped.Exists())
            {
                DebugHelper.Log("[SuspectBehavior] Suspect ped is null or doesn't exist.");
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
                    DebugHelper.Log($"[SuspectBehavior] Failed to access Game.PlayerPed: {ex.Message}");
                    return;
                }
            }

            if (playerPed == null || !playerPed.Exists())
            {
                DebugHelper.Log("[SuspectBehavior] playerPed is still null or does not exist.");
                return;
            }

            DebugHelper.Log($"[JsonBridge] Handling behavior '{behavior}' for ped {ped.Handle} (target: {playerPed.Handle})");

            switch ((behavior ?? "").ToLower())
            {
                case "fight":
                    ped.Task.FightAgainst(playerPed);
                    break;

                case "flee":
                    ped.Task.FleeFrom(playerPed);
                    break;

                case "flee&shoot":
                    ped.Task.FleeFrom(playerPed);

                    if (ped.IsInVehicle())
                    {
                        API.TaskVehicleShootAtPed(ped.Handle, playerPed.Handle, 9000f);
                    }
                    else
                    {
                        ped.Task.ShootAt(playerPed);
                    }
                    break;

                case "follow":
                        ped.Task.GoTo(playerPed);
                    break;

                case "wander":
                    ped.Task.WanderAround();
                    break;

                case "chase":
                    if (ped.IsInVehicle())
                    {
                        API.TaskVehicleChase(ped.Handle, playerPed.Handle);
                    }
                    break;

                case "random":
                    DebugHelper.Log("[SuspectBehavior] Behavior 'random' is not implemented.");
                    break;

                default:
                    DebugHelper.Log($"[SuspectBehavior] Unknown behavior: {behavior}");
                    break;
            }
        }

    }
}