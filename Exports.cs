using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using static CitizenFX.Core.Native.API;
using FivePD.API;
using Newtonsoft.Json;

namespace fivepd.json_event_exports
{
    class exports : BaseScript
    {
        bool OnDuty = false;

        public exports()
        {
            EventHandlers["fivepd_json_exports:OnDuty"] += new Action<bool>(OnDutyChanged);
            EventHandlers["fivepd_json_exports:OffDuty"] += new Action(OffDuty);

            Events.OnDutyStatusChange += OnDutyChange;
        }
        
        private void OnDutyChanged(bool onDuty)
        {
            OnDuty = onDuty;
            Events.InvokeDutyEvent(onDuty);
        }
    }
}
