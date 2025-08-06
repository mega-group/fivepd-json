using System.Collections.Generic;
using fivepd_json.Helpers;
using fivepd_json.models;
using Newtonsoft.Json;

public static class JsonTemplateGenerator
{
    public static string GenerateBlankCalloutTemplate()
    {
        var template = new CalloutConfig
        {
            shortName = "",
            description = "",
            responseCode = 0,
            weapon = "",
            pedModel = "",
            behavior = "",
            vehicleModel = "",
            heading = 0f,
            autoEnd = false,
            startDistance = 0f,
            debug = false,
            suspects = new List<SuspectConfig>
            {
                new SuspectConfig
                {
                    pedModel = "",
                    weapon = "",
                    vehicleModel = "",
                    heading = 0f,
                    behavior = "",
                    pursuit = false,
                    vehicleId = "",
                    seatIndex = 0,
                    excludeFromTrafficStop = false,
                    questions = new List<PedQuestionConfig> { new PedQuestionConfig() },
                    vehicleData = new VehicleDataConfig
                    {
                        items = new List<VehicleItems> { new VehicleItems { Name = "", IsIllegal = false } },
                        licensePlate = "",
                        insurance = false,
                        registration = false
                    },
                    PedData = new PedDataConfig
                    {
                        firstName = "",
                        lastName = "",
                        dateOfBirth = "",
                        warrant = "",
                        address = "",
                        gender = "",
                        age = 0,
                        bloodAlcoholLevel = 0.0,
                        usedDrugs = new bool[0],
                        driverLicense = new LicenseConfig { expiration = "", licenseStatus = "" },
                        weaponLicense = new LicenseConfig { expiration = "", licenseStatus = "" },
                        huntingLicense = new LicenseConfig { expiration = "", licenseStatus = "" },
                        fishingLicense = new LicenseConfig { expiration = "", licenseStatus = "" },
                        items = new List<ItemConfig> { new ItemConfig { Name = "", IsIllegal = false } },
                        violations = new List<ViolationConfig> { new ViolationConfig { Offence = "", Charge = "" } }
                    }
                }
            },
            victims = new List<VictimConfig> { new VictimConfig { pedModel = "", behavior = "" } },
            location = new LocationData { x = 0f, y = 0f, z = 0f, mode = "" },
            locations = new List<LocationData> { new LocationData { x = 0f, y = 0f, z = 0f, mode = "" } },
            questions = new List<PedQuestionConfig> { new PedQuestionConfig { question = "", answers = new List<string> { "" } } }
        };

        var logger = JsonConvert.SerializeObject(template, Formatting.Indented);
        DebugHelper.Log($"[JsonTemplate]\n{logger}");
        return logger;
    }
}
