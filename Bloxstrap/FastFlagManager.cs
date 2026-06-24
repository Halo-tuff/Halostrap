using Bloxstrap.Enums.FlagPresets;

namespace Bloxstrap
{
    public class FastFlagManager : JsonManager<Dictionary<string, object>>
    {
        private Dictionary<string, object> OriginalProp = new();

        public override string ClassName => nameof(FastFlagManager);

        public override string LOG_IDENT_CLASS => ClassName;

        public override string FileName => "ClientAppSettings.json";

        public override string FileLocation => Path.Combine(Paths.Modifications, "ClientSettings", FileName);

        public bool Changed => !OriginalProp.SequenceEqual(Prop);

        public static IReadOnlyDictionary<string, string> PresetFlags = new Dictionary<string, string>
        {
            { "Rendering.ManualFullscreen", "FFlagHandleAltEnterFullscreenManually" },
            { "Rendering.DisableScaling", "DFFlagDisableDPIScale" },
            { "Rendering.MSAA", "FIntDebugForceMSAASamples" },

            { "Rendering.TextureQuality.OverrideEnabled", "DFFlagTextureQualityOverrideEnabled" },
            { "Rendering.TextureQuality.Level", "DFIntTextureQualityOverride" },

            { "Performance.GraphicsMode", "FFlagDebugGraphicsPreferD3D11" },
            { "Performance.FRMQualityOverride", "DFIntDebugFRMQualityLevelOverride" },
            { "Performance.MinGrassDistance", "FIntFRMMinGrassDistance" },
            { "Performance.MaxGrassDistance", "FIntFRMMaxGrassDistance" },
            { "Performance.GrassMotionFactor", "FIntGrassMovementReducedMotionFactor" },
            { "Performance.CSGDistance", "DFIntCSGLevelOfDetailSwitchingDistance" },
            { "Performance.CSGDistanceL12", "DFIntCSGLevelOfDetailSwitchingDistanceL12" },
            { "Performance.CSGDistanceL23", "DFIntCSGLevelOfDetailSwitchingDistanceL23" },
            { "Performance.CSGDistanceL34", "DFIntCSGLevelOfDetailSwitchingDistanceL34" },
        };

        public static IReadOnlyDictionary<MSAAMode, string?> MSAAModes => new Dictionary<MSAAMode, string?>
        {
            { MSAAMode.Default, null },
            { MSAAMode.x1, "1" },
            { MSAAMode.x2, "2" },
            { MSAAMode.x4, "4" }
        };

        public static IReadOnlyDictionary<TextureQuality, string?> TextureQualityLevels => new Dictionary<TextureQuality, string?>
        {
            { TextureQuality.Default, null },
            { TextureQuality.Level0, "0" },
            { TextureQuality.Level1, "1" },
            { TextureQuality.Level2, "2" },
            { TextureQuality.Level3, "3" },
        };

        public static IReadOnlyDictionary<PerformanceMode, Dictionary<string, object>> PerformancePresets => new Dictionary<PerformanceMode, Dictionary<string, object>>
        {
            {
                PerformanceMode.Default,
                new Dictionary<string, object>()
            },
            {
                PerformanceMode.Balanced,
                new Dictionary<string, object>
                {
                    { "FFlagDebugGraphicsPreferD3D11", "true" },
                    { "DFIntDebugFRMQualityLevelOverride", "1" },
                }
            },
            {
                PerformanceMode.PerformanceTurbo,
                new Dictionary<string, object>
                {
                    { "FFlagDebugGraphicsPreferD3D11", "true" },
                    { "DFFlagTextureQualityOverrideEnabled", "true" },
                    { "DFIntTextureQualityOverride", "0" },
                    { "FIntDebugForceMSAASamples", "1" },
                    { "DFIntDebugFRMQualityLevelOverride", "1" },
                    { "FIntFRMMinGrassDistance", "0" },
                    { "FIntFRMMaxGrassDistance", "0" },
                    { "FIntGrassMovementReducedMotionFactor", "0" },
                    { "DFIntCSGLevelOfDetailSwitchingDistance", "250000" },
                    { "DFIntCSGLevelOfDetailSwitchingDistanceL12", "250000" },
                    { "DFIntCSGLevelOfDetailSwitchingDistanceL23", "250000" },
                    { "DFIntCSGLevelOfDetailSwitchingDistanceL34", "250000" },
                }
            }
        };

        // all fflags are stored as strings
        // to delete a flag, set the value as null
        public void SetValue(string key, object? value)
        {
            const string LOG_IDENT = "FastFlagManager::SetValue";

            if (value is null)
            {
                if (Prop.ContainsKey(key))
                    App.Logger.WriteLine(LOG_IDENT, $"Deletion of '{key}' is pending");

                Prop.Remove(key);
            }
            else
            {
                if (Prop.ContainsKey(key))
                {
                    if (key == Prop[key].ToString())
                        return;

                    App.Logger.WriteLine(LOG_IDENT, $"Changing of '{key}' from '{Prop[key]}' to '{value}' is pending");
                }
                else
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Setting of '{key}' to '{value}' is pending");
                }

                Prop[key] = value.ToString()!;
            }
        }

        // this returns null if the fflag doesn't exist
        public string? GetValue(string key)
        {
            // check if we have an updated change for it pushed first
            if (Prop.TryGetValue(key, out object? value) && value is not null)
                return value.ToString();

            return null;
        }

        public void SetPreset(string prefix, object? value)
        {
            foreach (var pair in PresetFlags.Where(x => x.Key.StartsWith(prefix)))
                SetValue(pair.Value, value);
        }

        public void SetPresetEnum(string prefix, string target, object? value)
        {
            foreach (var pair in PresetFlags.Where(x => x.Key.StartsWith(prefix)))
            {
                if (pair.Key.StartsWith($"{prefix}.{target}"))
                    SetValue(pair.Value, value);
                else
                    SetValue(pair.Value, null);
            }
        }

        public void SetPerformanceMode(PerformanceMode mode)
        {
            if (mode == PerformanceMode.Default)
            {
                // Clear all performance flags
                foreach (var flag in PerformancePresets[PerformanceMode.PerformanceTurbo].Keys)
                    SetValue(flag, null);
            }
            else if (PerformancePresets.TryGetValue(mode, out var flags))
            {
                foreach (var pair in flags)
                    SetValue(pair.Key, pair.Value);
            }
        }

        public PerformanceMode GetPerformanceMode()
        {
            var turboPreset = PerformancePresets[PerformanceMode.PerformanceTurbo];
            int matchCount = 0;

            foreach (var flag in turboPreset)
            {
                if (GetValue(flag.Key) == flag.Value.ToString())
                    matchCount++;
            }

            if (matchCount == turboPreset.Count)
                return PerformanceMode.PerformanceTurbo;

            var balancedPreset = PerformancePresets[PerformanceMode.Balanced];
            matchCount = 0;

            foreach (var flag in balancedPreset)
            {
                if (GetValue(flag.Key) == flag.Value.ToString())
                    matchCount++;
            }

            if (matchCount == balancedPreset.Count)
                return PerformanceMode.Balanced;

            return PerformanceMode.Default;
        }

        public string? GetPreset(string name)
        {
            if (!PresetFlags.ContainsKey(name))
            {
                App.Logger.WriteLine("FastFlagManager::GetPreset", $"Could not find preset {name}");
                Debug.Assert(false, $"Could not find preset {name}");
                return null;
            }

            return GetValue(PresetFlags[name]);
        }

        public T GetPresetEnum<T>(IReadOnlyDictionary<T, string> mapping, string prefix, string value) where T : Enum
        {
            foreach (var pair in mapping)
            {
                if (pair.Value == "None")
                    continue;

                if (GetPreset($"{prefix}.{pair.Value}") == value)
                    return pair.Key;
            }

            return mapping.First().Key;
        }

        public override void Save()
        {
            // convert all flag values to strings before saving

            foreach (var pair in Prop)
                Prop[pair.Key] = pair.Value.ToString()!;

            base.Save();

            // clone the dictionary
            OriginalProp = new(Prop);
        }

        public override bool Load(bool alertFailure = true)
        {
            bool result = base.Load(alertFailure);

            // clone the dictionary
            OriginalProp = new(Prop);

            if (GetPreset("Rendering.ManualFullscreen") != "False")
                SetPreset("Rendering.ManualFullscreen", "False");

            return result;
        }
    }
}
