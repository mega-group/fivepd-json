using System;
using CitizenFX.Core;

namespace fivepd_json.Helpers
{
    public static class NearbyLocation
    {
        public static Vector3 GetRandomNearbyLocation()
        {
            var pos = Game.Player.Character?.Position ?? new Vector3(0f, 0f, 72f);
            var rand = new Random();
            return new Vector3(
                pos.X + rand.Next(100, 500),
                pos.Y + rand.Next(100, 500),
                pos.Z
            );
        }

        public static Vector3 GetRandomNearbyLocation(Vector3 origin)
        {
            var rand = new Random();
            return new Vector3(
                origin.X + rand.Next(100, 500),
                origin.Y + rand.Next(100, 500),
                origin.Z
            );
        }
    }
}
