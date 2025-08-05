using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API;
using FivePD.API.Utils;
using fivepd_json.Behavior;
using fivepd_json.Helpers;
using fivepd_json.Loader;
using fivepd_json.Logic;
using fivepd_json.models;
using fivepd_json.net.models;
using static CitizenFX.Core.Native.API;
using static fivepd_json.Logic.SpawnSuspects;

namespace fivepd.json
{
    [CalloutProperties("json-dynamic", "Mega Group", "1.0")]
    public class JsonBridge : Callout
    {
        private CalloutConfig config;
        private Vector3 finalLocation;
        private bool isCalloutFinished = false;

        private List<SpawnedSuspect> spawnedSuspects = new List<SpawnedSuspect>();
        private List<Ped> spawnedVictims = new List<Ped>();
        private bool suspectsInitialized = false;
        private Func<Task> suspectMonitorTickHandler;
        
        public JsonBridge()
        {
            config = JsonConfigManager.GetRandomConfig() ?? new CalloutConfig
            {
                shortName = "json-dynamic",
                description = "Default fallback scenario",
                responseCode = 3,
                pedModel = "a_m_y_skater_01",
                weapon = "WEAPON_PISTOL",
                behavior = "fight",
                vehicleModel = "SULTAN",
                heading = 180f,
                pursuit = true,
                debug = false
            };

            if (config.debug == true)
            {
                DebugHelper.EnableDebug(true, config.shortName);
            }

            if (config.location != null)
            {
                finalLocation = new Vector3(config.location.x, config.location.y, config.location.z);
            }
            else if (config.locations != null && config.locations.Count > 0)
            {
                var loc = config.locations[new Random().Next(config.locations.Count)];
                finalLocation = new Vector3(loc.x, loc.y, loc.z);
            }
            else
            {
                finalLocation = NearbyLocation.GetSafeRandomLocationFarAway();
            }
            Location = finalLocation;
            InitInfo(finalLocation);
            ShortName = config.shortName;
            CalloutDescription = config.description;
            ResponseCode = config.responseCode;
            StartDistance = (config.startDistance > 5) ? config.startDistance : 250f;

            DebugHelper.Log($"[JsonBridge] Selected config: {config.shortName}", "_");
        }

        public override async Task OnAccept()
        {
            spawnedSuspects.Clear();
            spawnedVictims.Clear();

            DebugHelper.Log("[JsonBridge] Callout Accepted:" +
                $"\n  shortName: {config.shortName}" +
                $"\n  description: {config.description}" +
                $"\n  responseCode: {config.responseCode}" +
                $"\n  weapon: {config.weapon}" +
                $"\n  pedModel: {config.pedModel}" +
                $"\n  behavior: {config.behavior}" +
                $"\n  vehicleModel: {config.vehicleModel}" +
                $"\n  heading: {config.heading}" +
                $"\n  autoEnd: {config.autoEnd}" +
                $"\n  pursuit: {config.pursuit}" +
                $"\n  startDistance: {config.startDistance}" +
                $"\n  debug: {config.debug}" +
                $"\n  suspects: {(config.suspects?.Count.ToString() ?? "null")}" +
                $"\n  victims: {(config.victims?.Count.ToString() ?? "null")}" +
                $"\n  location: {(config.location != null ? $"({config.location.x}, {config.location.y}, {config.location.z})" : "null")}" +
                $"\n  locations: {(config.locations?.Count.ToString() ?? "null")}", "SUCCESS"
            );

            float groundZ = World.GetGroundHeight(finalLocation);
            var spawnBase = new Vector3(finalLocation.X, finalLocation.Y, groundZ);

            InitBlip();

            try
            {
                if (config.suspects != null && config.suspects.Count > 0)
                {
                    spawnedSuspects = await SpawnSuspects.FromConfig(config.suspects, spawnBase);
                }
                else if (!string.IsNullOrEmpty(config.pedModel))
                {
                    var singleSuspect = await SpawnSuspects.SpawnSingleSuspect(config, spawnBase);
                    if (singleSuspect != null)
                        spawnedSuspects.Add(singleSuspect);
                }

                if (spawnedSuspects.Count == 0)
                {
                    DebugHelper.Log("[JsonBridge] No suspects spawned!", "WARN");
                    return;
                }

                if (spawnedSuspects.Count > 0)
                {
                    suspectsInitialized = true;
                }

                if (config.victims != null && config.victims.Count > 0)
                {
                    spawnedVictims = await VictimSpawner.SpawnVictimsAsync(config.victims, spawnBase);
                }

                foreach (var suspect in spawnedSuspects)
                {
                    if (suspect.PedData?.Count > 0)
                    {
                        var defaultPedData = await FivePD.API.Utilities.GetPedData(suspect.Ped.NetworkId);
                        var cfg = suspect.PedData[0];
                        var pedData = await suspect.Ped.GetData();

                        pedData.FirstName = !string.IsNullOrWhiteSpace(cfg.firstName)
    ? cfg.firstName
    : defaultPedData.FirstName;

                        pedData.LastName = !string.IsNullOrWhiteSpace(cfg.lastName)
                            ? cfg.lastName
                            : defaultPedData.LastName;

                        pedData.DateOfBirth = !string.IsNullOrWhiteSpace(cfg.dateOfBirth)
                            ? cfg.dateOfBirth
                            : defaultPedData.DateOfBirth;

                        pedData.Address = !string.IsNullOrWhiteSpace(cfg.address)
                            ? cfg.address
                            : defaultPedData.Address;

                        pedData.Warrant = !string.IsNullOrWhiteSpace(cfg.warrant)
                            ? cfg.warrant
                            : defaultPedData.Warrant;

                        pedData.BloodAlcoholLevel = cfg.bloodAlcoholLevel ?? defaultPedData.BloodAlcoholLevel;

                        pedData.Age = cfg.age ?? defaultPedData.Age;

                        if (!string.IsNullOrWhiteSpace(cfg.gender) &&
                            Enum.TryParse(cfg.gender, true, out Gender parsedGender))
                        {
                            pedData.Gender = parsedGender;
                        }
                        else
                        {
                            pedData.Gender = defaultPedData.Gender;
                        }

                        pedData.UsedDrugs = cfg.usedDrugs?.Length == 3
                            ? cfg.usedDrugs.Select((used, index) => used ? (PedData.Drugs)index : (PedData.Drugs?)null)
                                           .Where(drug => drug.HasValue)
                                           .Select(drug => drug.Value)
                                           .ToArray()
                            : defaultPedData.UsedDrugs ?? Array.Empty<PedData.Drugs>();

                        pedData.DriverLicense = cfg.driverLicense != null
                            ? CreateLicense(cfg.driverLicense)
                            : defaultPedData.DriverLicense;

                        pedData.WeaponLicense = cfg.weaponLicense != null
                            ? CreateLicense(cfg.weaponLicense)
                            : defaultPedData.WeaponLicense;

                        pedData.HuntingLicense = cfg.huntingLicense != null
                            ? CreateLicense(cfg.huntingLicense)
                            : defaultPedData.HuntingLicense;

                        pedData.FishingLicense = cfg.fishingLicense != null
                            ? CreateLicense(cfg.fishingLicense)
                            : defaultPedData.FishingLicense;


                        if (cfg.items != null)
                        {
                            foreach (var item in cfg.items)
                                pedData.Items.Add(new Item { Name = item.Name, IsIllegal = item.IsIllegal });
                        }

                        if (cfg.violations != null)
                        {
                            foreach (var v in cfg.violations)
                                pedData.Violations.Add(new Violation { Offence = v.Offence, Charge = v.Charge });
                        }

                        suspect.Ped.SetData(pedData);
                        DebugHelper.Log($"[JsonBridge] Applied PedData to {suspect.Ped.Handle}", "INFO");
                    }

                    if (suspect.Vehicle != null && suspect.VehicleData?.Count > 0)
                    {
                        var cfg = suspect.VehicleData[0];
                        var vehData = await suspect.Vehicle.GetData();

                        if (cfg.items != null)
                        {
                            foreach (var item in cfg.items)
                                vehData.Items.Add(new Item { Name = item.Name, IsIllegal = item.IsIllegal });
                        }

                        suspect.Vehicle.SetData(vehData);
                        DebugHelper.Log($"[JsonBridge] Applied VehicleData to vehicle {suspect.Vehicle.Handle}", "INFO");
                    }

                    if (suspect.Questions != null && suspect.Questions.Count > 0)
                    {
                        var pedQuestions = new List<PedQuestion>();

                        foreach (var q in suspect.Questions)
                        {
                            if (!string.IsNullOrWhiteSpace(q.question) && q.answers?.Count > 0)
                            {
                                pedQuestions.Add(new PedQuestion
                                {
                                    Question = q.question,
                                    Answers = q.answers
                                });
                            }
                        }

                        if (pedQuestions.Count == 1)
                            AddPedQuestion(suspect.Ped, pedQuestions[0]);
                        else if (pedQuestions.Count > 1)
                            AddPedQuestions(suspect.Ped, pedQuestions.ToArray());

                        foreach (var question in pedQuestions)
                        {
                            int answerCount = question.Answers?.Count ?? 0;
                            DebugHelper.Log($"Question: \"{question.Question}\" has {answerCount} answer(s)", "INFO");
                        }

                        DebugHelper.Log($"[JsonBridge] Added {pedQuestions.Count} question(s) to ped {suspect.Ped.Handle}", "INFO");
                    }
                }


            }
            catch (Exception ex)
            {
                DebugHelper.Log($"[JsonBridge] Exception during OnAccept: {ex}", "ERROR");
            }
        }

        private PedData.License CreateLicense(LicenseConfig cfg)
        {
            if (cfg == null) return null;

            return new PedData.License
            {
                ExpirationDate = cfg.expiration,
                LicenseStatus = Enum.TryParse(cfg.licenseStatus, out PedData.License.Status status)
                    ? status : PedData.License.Status.Valid
            };
        }



        private async Task SuspectMonitorTick()
        {
            foreach (var suspect in spawnedSuspects)
            {
                if (suspect?.Ped != null && suspect.Ped.Exists())
                {
                    await SuspectMonitor.MonitorAsync(
                        suspect.Ped,
                        () => isCalloutFinished,
                        () => isCalloutFinished = true,
                        EndCallout
                    );
                }
            }
        }
        public override void OnStart(Ped closest)
        {
            base.OnStart(closest);

            if (!AssignedPlayers.Contains(Game.PlayerPed))
            {
                AssignedPlayers.Add(Game.PlayerPed);
            }

            DebugHelper.Log("[JsonBridge] OnStart triggered.", "SUCCESS");

            if (!suspectsInitialized)
            {
                DebugHelper.Log("[JsonBridge] suspects not initialized, skipping OnStart logic.", "WARN");
                return;
            }

            foreach (var s in spawnedSuspects)
            {
                if (s?.Ped != null && s.Ped.Exists())
                {
                    try
                    {
                        DebugHelper.Log($"[JsonBridge] Applying behavior {s.Behavior}");
                        SuspectBehavior.HandleBehavior(s.Ped, s.Behavior);
                        if (config.pursuit == true)
                        {
                            var pursuit = Pursuit.RegisterPursuit(s.Ped);
                            bool isVehiclePursuit = s.Ped.IsInVehicle();

                            pursuit.Init(isVehiclePursuit, 35f, 50f, true);
                            pursuit.ActivatePursuit();

                            DebugHelper.Log($"[JsonBridge] Registered {(isVehiclePursuit ? "vehicle" : "foot")} pursuit for ped {s.Ped.Handle}", "INFO");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugHelper.Log($"[JsonBridge] Error in HandleBehavior: {ex}", "ERROR");
                    }
                }
                else
                {
                    DebugHelper.Log("[JsonBridge] Suspect Ped is null or does not exist.", "WARN");
                }
            }

            if (config.autoEnd && spawnedSuspects != null)
            {
                suspectMonitorTickHandler = SuspectMonitorTick;
                Tick += suspectMonitorTickHandler;
            }
        }

        public override void OnCancelBefore()
        {
            base.OnCancelBefore();
            DebugHelper.Log("[JsonBridge] Cleaning up all entities.");

            try
            {
                foreach (var s in spawnedSuspects)
                {
                    if (s?.Ped != null && s.Ped.Exists())
                        s.Ped.Delete();
                    if (s?.Vehicle != null && s.Vehicle.Exists())
                        s.Vehicle.Delete();
                }

                foreach (var v in spawnedVictims)
                {
                    if (v != null && v.Exists())
                        v.Delete();
                }
            }
            catch (Exception ex)
            {
                DebugHelper.Log($"[JsonBridge] Exception during cleanup: {ex}", "ERROR");
            }

            isCalloutFinished = true;
            Tick -= suspectMonitorTickHandler;
            DebugHelper.Log("[JsonBridge] Entity cleanup complete.", "SUCCESS");
            DebugHelper.EnableDebug(false);
        }

        public override void OnCancelAfter()
        {
            // additional cleanup if needed
        }

        public override void OnBackupReceived(Player player)
        {
            // optional backup logic
        }

        public override void OnBackupCalled(int code)
        {
            // optional backup called logic
        }

        public override void OnPlayerRevokedBackup(Player player)
        {
            // optional player revoked backup logic
        }
    }
}