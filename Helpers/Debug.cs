using System;
using CitizenFX.Core;
using fivepd_json.models;

namespace fivepd_json.Helpers
{
    public static class DebugHelper
    {
        private static bool _debugEnabled = false;
        private static string _calloutName = "Unknown";

        public static void EnableDebug(bool enabled, string calloutName = "Unknown")
        {
            _debugEnabled = enabled;
            _calloutName = calloutName;
        }

        public static void Log(string message, string level)
        {
            if (!_debugEnabled) return;
            {
                string timestamp = DateTime.Now.ToString("hh:mm:ss tt");
                string color;

                switch (level.ToUpper())
                {
                    case "INFO":
                        color = "^4"; // Blue
                        break;
                    case "SUCCESS":
                        color = "^2"; // Green
                        break;
                    case "WARN":
                        color = "^3"; // Yellow
                        break;
                    case "ERROR":
                        color = "^1"; // Red
                        break;
                    case "DEBUG":
                        color = "^6"; // Purple
                        break;
                    default:
                        color = "^7"; // Default (white)
                        break;
                }

                Debug.WriteLine($"{color}[{timestamp}] [{level}] {message}");
            }
        }
    }
}
