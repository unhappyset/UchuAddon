using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Role.Crewmate;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Map;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Roles.Impostor;
using Nebula.Roles.Perks;
using Nebula.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Components;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using static Nebula.Roles.Impostor.Hadar;
using static Sentry.MeasurementUnit;
using static UnityEngine.ProBuilder.UvUnwrapping;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Perk;

internal class RepairU : PerkFunctionalInstance
{
    const float Cooldown = 5f;

    static PerkFunctionalDefinition Def = new("repairU", PerkFunctionalDefinition.Category.Standard, new PerkDefinition("repairU", 3, 52, new Virial.Color(209, 209, 209)).CooldownText("%CD%", () => Cooldown), (def, instance) => new RepairU(def, instance));

    bool used = false;
    public RepairU(PerkDefinition def, PerkInstance instance) : base(def, instance)
    {
        cooldownTimer = NebulaAPI.Modules.Timer(this, Cooldown);
        cooldownTimer.Start(Cooldown);
        PerkInstance.BindTimer(cooldownTimer);
    }
    private GameTimer cooldownTimer;

    public override bool HasAction => true;

    public override void OnClick()
    {
        if (used) return;
        if (cooldownTimer.IsProgressing) return;
        if (!AmongUsUtil.InAnySab) return;

        used = true;
        EngineerFix();
    }

    void OnUpdate(GameHudUpdateEvent ev)
    {
        PerkInstance.SetDisplayColor(used ? Color.gray : Color.white);
    }

    public void EngineerFix()
    {
        switch (AmongUsUtil.CurrentMapId)
        {
            case 0:
            case 3:
                if (ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive)
                {
                    FixComms();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().IsActive)
                {
                    FixReactor(SystemTypes.Reactor);
                }
                if (ShipStatus.Instance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().IsActive)
                {
                    FixOxygen();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                {
                    RpcFix(MyPlayer);
                    return;
                }
                break;
            case 1:
                if (ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HqHudSystemType>().IsActive)
                {
                    FixMiraComms();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().IsActive)
                {
                    FixReactor(SystemTypes.Reactor);
                }
                if (ShipStatus.Instance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().IsActive)
                {
                    FixOxygen();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                {
                    RpcFix(MyPlayer);
                    return;
                }
                break;
            case 2:
                if (ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive)
                {
                    FixComms();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Laboratory].Cast<ReactorSystemType>().IsActive)
                {
                    FixReactor(SystemTypes.Laboratory);
                }
                if (ShipStatus.Instance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>().IsActive)
                {
                    FixOxygen();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                {
                    RpcFix(MyPlayer);
                    return;
                }
                break;
            case 4:
                if (ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>().IsActive)
                {
                    FixComms();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.HeliSabotage].Cast<HeliSabotageSystem>().IsActive)
                {
                    FixAirshipReactor();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                {
                    RpcFix(MyPlayer);
                    return;
                }
                break;
            case 5:
                if (ShipStatus.Instance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>().IsActive)
                {
                    FixReactor(SystemTypes.Reactor);
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Comms].Cast<HqHudSystemType>().IsActive)
                {
                    FixMiraComms();
                }
                if (ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>().IsActive)
                {
                    RpcFix(MyPlayer);
                    return;
                }
                break;
            default:
                return;
        }
    }

    private static void FixComms()
    {
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 0);
    }

    private static void FixMiraComms()
    {
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 16);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, 17);
    }

    private static void FixAirshipReactor()
    {
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, 16);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, 17);
    }

    private static void FixReactor(SystemTypes system)
    {
        ShipStatus.Instance.RpcUpdateSystem(system, 16);
    }

    private static void FixOxygen()
    {
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, 16);
    }
    [NebulaRPC]
    public static void RpcFix(GamePlayer engineer)
    {
        SwitchSystem switchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
        switchSystem.ActualSwitches = switchSystem.ExpectedSwitches;
    }
}