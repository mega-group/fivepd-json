using System.Threading.Tasks;
using CitizenFX.Core;

namespace fivepd_json.Helpers
{
    public static class SuspectMonitor
    {
        public static async Task MonitorAsync(Ped suspect, System.Func<bool> isFinished, System.Action markFinished, System.Action endCallout)
        {
            DebugHelper.Log("[JsonBridge] Starting suspect monitor...");

            while (!isFinished())
            {
                if (suspect == null || !suspect.Exists())
                {
                    DebugHelper.Log("[JsonBridge] Suspect no longer exists.");
                    break;
                }

                if (suspect.IsDead || suspect.IsCuffed)
                {
                    DebugHelper.Log("[JsonBridge] Suspect is dead or cuffed. Ending callout.");
                    markFinished?.Invoke();
                    endCallout?.Invoke();
                    break;
                }

                await BaseScript.Delay(1000); // check every second
            }

            DebugHelper.Log("[JsonBridge] Suspect monitor ended.");
        }

    }
}