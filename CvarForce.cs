using System.Globalization;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Extensions;
using Microsoft.Extensions.Logging;

namespace CvarForce;

public class CvarForceConfig : BasePluginConfig
{
    public Dictionary<string, object> Cvars { get; set; } = new()
    {
        ["sv_cheats"] = false, ["mp_autoteambalance"] = true, ["mp_timelimit"] = 30, ["sv_password"] = "pass123"
    };
}

public class CvarForce : BasePlugin, IPluginConfig<CvarForceConfig>
{
    public override string ModuleName => "Cvar Force";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.1";

    public CvarForceConfig Config { get; set; } = new();

    public void OnConfigParsed(CvarForceConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventServerCvar>(OnServerCvarChanged);

        AddCommand("css_cf_reload", "Reload CvarForce config", (_, _) =>
        {
            Config.Reload();
            ForceAllCvars();
            Logger.LogInformation("Configuration reloaded");
        });

        if (hotReload) ForceAllCvars();
    }

    private void ForceAllCvars()
    {
        foreach (var cvar in Config.Cvars)
        {
            CheckAndSetCvar(cvar.Key, cvar.Value);
        }
    }

    private HookResult OnServerCvarChanged(EventServerCvar @event, GameEventInfo info)
    {
        if (!Config.Cvars.TryGetValue(@event.Cvarname, out var targetValue))
            return HookResult.Continue;

        var changed = CheckAndSetCvar(@event.Cvarname, targetValue);

        if (changed)
        {
            info.DontBroadcast = true;
        }

        return HookResult.Continue;
    }

    private static bool CheckAndSetCvar(string name, object configValue)
    {
        var conVar = ConVar.Find(name);
        if (conVar == null) return false;

        var targetStr = ValueToString(configValue);

        var currentStr = conVar.StringValue;

        if (IsValuesEqual(currentStr, targetStr))
        {
            return false;
        }

        Server.NextFrame(() =>
        {
            Server.ExecuteCommand($"{name} \"{targetStr}\"");
        });

        return true;
    }

    private static bool IsValuesEqual(string val1, string val2)
    {
        if (val1 == val2) return true;
        if (string.IsNullOrEmpty(val1) || string.IsNullOrEmpty(val2)) return false;

        var clean1 = CleanBool(val1);
        var clean2 = CleanBool(val2);

        if (string.Equals(clean1, clean2, StringComparison.OrdinalIgnoreCase)) return true;

        if (float.TryParse(clean1, NumberStyles.Any, CultureInfo.InvariantCulture, out var f1) &&
            float.TryParse(clean2, NumberStyles.Any, CultureInfo.InvariantCulture, out var f2))
        {
            return Math.Abs(f1 - f2) < 0.001f;
        }

        return false;
    }

    private static string CleanBool(string val)
    {
        if (string.Equals(val, "true", StringComparison.OrdinalIgnoreCase)) return "1";
        return string.Equals(val, "false", StringComparison.OrdinalIgnoreCase) ? "0" : val;
    }

    private static string ValueToString(object? value)
    {
        return value switch
        {
            null => "",
            JsonElement je => je.ValueKind switch
            {
                JsonValueKind.True => "1",
                JsonValueKind.False => "0",
                JsonValueKind.Number => je.GetRawText(),
                _ => je.GetString() ?? ""
            },
            bool b => b ? "1" : "0",
            int i => i.ToString(),
            float f => f.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
    }
}