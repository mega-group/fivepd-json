using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace fivepd_json.Behavior
{
    public static class SuspectBehavior
    {
        public static void HandleBehavior(Ped ped, string behavior, Ped target = null)
        {
            var playerPed = target ?? Game.PlayerPed;

            switch ((behavior ?? "").ToLower())
            {
                case "fight":
                    ped.Task.FightAgainst(playerPed);
                    break;

                case "flee":
                    ped.Task.FleeFrom(playerPed);
                    break;

                case "driveby":
                    API.SetPedAsEnemy(ped.Handle, true);
                    API.GiveWeaponToPed(ped.Handle, (uint)API.GetHashKey("WEAPON_PISTOL"), 100, false, true);

                    if (ped.IsInVehicle())
                    {
                        int vehicle = API.GetVehiclePedIsIn(ped.Handle, false);
                        if (API.GetPedInVehicleSeat(vehicle, -1) != ped.Handle)
                        {
                            API.TaskWarpPedIntoVehicle(ped.Handle, vehicle, -1); // Driver seat
                        }

                        uint firingPattern = (uint)API.GetHashKey("FIRING_PATTERN_BURST_FIRE_DRIVEBY");

                        API.TaskDriveBy(
                            ped.Handle,
                            playerPed.Handle,
                            0,
                            0f, 0f, 0f,
                            15f,
                            100,
                            false,
                            firingPattern
                        );
                        ped.Task.FleeFrom(playerPed);
                    }
                    else
                    {
                        ped.Task.ShootAt(playerPed, 10000);
                    }
                    break;
                case "random":
                    break;

                default:
                    break;
            }
        }
    }
}
