using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;


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
        public static Vector3 GetSafeRandomNearbyLocation(Vector3 origin)
        {
            var rand = new Random();

            for (int i = 0; i < 10; i++)
            {
                double angle = rand.NextDouble() * Math.PI * 2;
                double distance = rand.Next(30, 80); // realistic distance

                float x = origin.X + (float)(Math.Cos(angle) * distance);
                float y = origin.Y + (float)(Math.Sin(angle) * distance);
                float z = origin.Z;

                Vector3 safeCoord = new Vector3();
                bool found = API.GetSafeCoordForPed(x, y, z, true, ref safeCoord, 0);

                if (found)
                {
                    return safeCoord;
                }
            }

            return origin;
        }
        public static Vector3 GetSafeRandomLocationNearby()
        {
            var pos = Game.Player.Character?.Position ?? new Vector3(0f, 0f, 72f);
            return GetSafeRandomNearbyLocation(pos);
        }

        public static Vector3 GetSafeRandomLocationNearby(Vector3 origin)
        {
            return GetSafeRandomNearbyLocation(origin);
        }
        public static Vector3 GetSafeRandomLocationFarAway()
        {
            var origin = Game.Player.Character?.Position ?? new Vector3(0f, 0f, 72f);
            var rand = new Random();

            for (int i = 0; i < 10; i++)
            {
                double angle = rand.NextDouble() * Math.PI * 2;
                double distance = rand.Next(300, 700);

                float x = origin.X + (float)(Math.Cos(angle) * distance);
                float y = origin.Y + (float)(Math.Sin(angle) * distance);
                float z = origin.Z;

                Vector3 safeCoord = new Vector3();
                bool found = API.GetSafeCoordForPed(x, y, z, true, ref safeCoord, 0);

                if (found)
                    return safeCoord;
            }

            return origin + new Vector3(300, 0, 0);
        }
    }
}