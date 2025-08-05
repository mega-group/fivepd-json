using System.Threading.Tasks;
using CitizenFX.Core;

namespace fivepd_json.Helpers
{
    public static class SuspectMonitor
    {
        public static async Task MonitorAsync(Ped suspect, System.Func<bool> isFinished, System.Action markFinished, System.Action endCallout)
        {
            if (isFinished()) return;

            if (suspect == null || !suspect.Exists()) return;

            if (suspect.IsDead || suspect.IsCuffed)
            {
                markFinished?.Invoke();
                DebugHelper.Log("[JsonBridge] Auto-ending callout due to suspect state.");
                endCallout?.Invoke();
                return;
            }

            await Task.FromResult(0);

        }
    }
}