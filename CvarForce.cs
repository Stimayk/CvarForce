using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Extensions;
using Microsoft.Extensions.Logging;

namespace CvarForce;

public class CvarForceConfig : BasePluginConfig
{
    public Dictionary<string, bool?> Cvars { get; set; } = new()
    {
        ["sv_cheats"] = false
    };
}

public class CvarForce : BasePlugin, IPluginConfig<CvarForceConfig>
{
    public override string ModuleName => "Cvar Force";
    public override string ModuleDescription => "Forces cvars to specific values";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

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
            Logger.LogInformation("Configuration reloaded");
        });
    }

    private HookResult OnServerCvarChanged(EventServerCvar @event, GameEventInfo info)
    {
        if (!Config.Cvars.TryGetValue(@event.Cvarname, out var expectedValue) || expectedValue is null)
            return HookResult.Continue;

        var isCurrentValueTrue = IsTruthy(@event.Cvarvalue);
        var isExpectedValueTrue = expectedValue.Value;

        if (isCurrentValueTrue == isExpectedValueTrue)
            return HookResult.Continue;

        var cvarName = @event.Cvarname;

        Server.NextFrame(() =>
        {
            SetCvar(cvarName, isExpectedValueTrue);
        });

        info.DontBroadcast = true;
        return HookResult.Continue;
    }

    private static void SetCvar(string name, bool value)
    {
        var cvar = ConVar.Find(name);
        var intValue = value ? 1 : 0;

        if (cvar is not null)
        {
            if (cvar.GetPrimitiveValue<bool>() != value)
            {
                cvar.SetValue(intValue);
            }
        }
        else
        {
            Server.ExecuteCommand($"{name} {intValue}");
        }
    }

    private static bool IsTruthy(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}