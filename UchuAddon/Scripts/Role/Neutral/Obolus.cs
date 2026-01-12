using BepInEx.Unity.IL2CPP.Utils.Collections;
using Epic.OnlineServices.Presence;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Configuration;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles;
using Nebula.Roles.Complex;
using Nebula.Roles.Crewmate;
using Nebula.Roles.Modifier;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Nebula.VoiceChat;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using Virial;
using Virial;
using Virial.Assignable;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Components;
using Virial.Configuration;
using Virial.Configuration;
using Virial.DI;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Events.Role;
using Virial.Game;
using Virial.Game;
using Virial.Runtime;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Neutral;

public class ObolusU : DefinedRoleTemplate, DefinedRole,IAssignableDocument
{
    public static Team MyTeam = new Team("teams.obolusU", new Virial.Color(189, 135, 26), TeamRevealType.OnlyMe);
    private ObolusU() : base("obolusU", new(189, 135, 26), RoleCategory.NeutralRole, MyTeam, [KillCoolDownOption,KillShieldTurn,CanKillHidingPlayer,ImpostorVision,BlackOutVision,ExtraWin,
    new GroupConfiguration("options.role.obolusU.group.win",[JackalOptions,TyrantOptions,ObolusOptions],GroupConfigurationColor.ToDarkenColor(MyTeam.Color.ToUnityColor()))])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static private IRelativeCooldownConfiguration KillCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.obolusU.killCooldown", CoolDownType.Relative, (0f, 60f, 2.5f), 25f, (-40f, 40f, 2.5f), -5f, (0.125f, 2f, 0.125f), 1f);
    static private BoolConfiguration CanKillHidingPlayer = NebulaAPI.Configurations.Configuration("options.role.obolusU.canKillHidingPlayer", false);
    public static IntegerConfiguration KillShieldTurn = NebulaAPI.Configurations.Configuration("options.role.obolusU.killShieldTurn", (0, 7), 2);
    static private BoolConfiguration ImpostorVision = NebulaAPI.Configurations.Configuration("options.role.obolusU.impostorVision", true);
    static private BoolConfiguration BlackOutVision = NebulaAPI.Configurations.Configuration("options.role.obolusU.blackOutVision", true);
    static internal BoolConfiguration ExtraWin = NebulaAPI.Configurations.Configuration("options.role.obolusU.extraWin", false);

    static private BoolConfiguration JackalOptions = NebulaAPI.Configurations.Configuration("options.role.obolusU.jackalOptions", false);
    static private BoolConfiguration TyrantOptions = NebulaAPI.Configurations.Configuration("options.role.obolusU.tyrantOptions", false);
    static private BoolConfiguration ObolusOptions = NebulaAPI.Configurations.Configuration("options.role.obolusU.obolusOptions", false);
    static public float KillCooldown => KillCoolDownOption.Cooldown;

    static internal Image IconImage = NebulaAPI.AddonAsset.GetResource("RoleIcon/Obolus.png")!.AsImage(100f)!;
    Image? DefinedAssignable.IconImage => IconImage;

    static public ObolusU MyRole = new ObolusU();
    bool DefinedRole.IsKiller => true;

    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        static private readonly Virial.Media.Image KillImage = NebulaAPI.AddonAsset.GetResource("KillButtonOrange.png")!.AsImage(100f)!;
        bool WinBlock = false;
        bool Win = false;
        bool Extra = false;
        int leftTurnKill = 1;
        int killLock = 0;
        SpriteRenderer? lockSprite = null;

        public Instance(GamePlayer player) : base(player)
        {
        }



        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                leftTurnKill = 1;
                ModAbilityButton killButton = null!;

                var myTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p), null, CanKillHidingPlayer);
                myTracker.SetColor(MyRole.RoleColor);

                killButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, Virial.Compat.VirtualKeyInput.Kill,
                    KillCooldown, "kill", ModAbilityButton.LabelType.Impostor, null!,
                    (target, _) =>
                    {

                        var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                        if (!(cancelable?.IsCanceled ?? false))
                        {
                            
                            if (target.IsImpostor)
                            {
                                if(!ExtraWin)NebulaAPI.CurrentGame?.TriggerGameEnd(UchuGameEnd.ObolusWin, GameEndReason.Special, BitMasks.AsPlayer(1u << MyPlayer.PlayerId));
                                if (ExtraWin) Extra = true;
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);                        
                            }
                            else
                            {
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                            }
                            if (JackalOptions && target.Role.Role is Jackal)
                            {
                                if (!ExtraWin) NebulaAPI.CurrentGame?.TriggerGameEnd(UchuGameEnd.ObolusWin, GameEndReason.Special, BitMasks.AsPlayer(1u << MyPlayer.PlayerId));
                                if (ExtraWin) Extra = true;
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                            }
                            if (TyrantOptions && target.Role.Role is Tyrant)
                            {
                                if (!ExtraWin) NebulaAPI.CurrentGame?.TriggerGameEnd(UchuGameEnd.ObolusWin, GameEndReason.Special, BitMasks.AsPlayer(1u << MyPlayer.PlayerId));
                                if (ExtraWin) Extra = true;
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                            }
                            if (ObolusOptions && target.Role.Role is ObolusU)
                            {
                                if (!ExtraWin) NebulaAPI.CurrentGame?.TriggerGameEnd(UchuGameEnd.ObolusWin, GameEndReason.Special, BitMasks.AsPlayer(1u << MyPlayer.PlayerId));
                                if (ExtraWin) Extra = true;
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                            }

                            leftTurnKill--;
                            killButton.UpdateUsesIcon(leftTurnKill.ToString());
                        }
                        if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    }, 
                    null,
                    _ => myTracker.CurrentTarget != null && !MyPlayer.IsDived,
                    _ => MyPlayer.AllowToShowKillButtonByAbilities
                    );
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                killButton.OnUpdate = (button) =>
                {
                    killButton.UpdateUsesIcon(leftTurnKill.ToString());
                };
                killButton.ShowUsesIcon(2, leftTurnKill.ToString());
                killButton.Visibility = (button) => leftTurnKill > 0;
                killButton.Availability = (button) => myTracker.CurrentTarget != null && this.MyPlayer.CanMove && lockSprite == null;
                killButton.SetImage(KillImage);
                if (KillShieldTurn > killLock)
                {
                    lockSprite = (killButton as ModAbilityButtonImpl)!.VanillaButton.AddLockedOverlay();
                }
                (killButton as ModAbilityButtonImpl)!.OnMeeting = (button) =>
                {
                    if (lockSprite != null)
                    {
                        killLock++;
                        if (KillShieldTurn <= killLock)
                        {
                            UnityEngine.Object.Destroy(lockSprite.gameObject);
                            lockSprite = null;
                        }
                    }
                };
            }
        }

        void Leset(MeetingPreEndEvent ev)
        {
            leftTurnKill = 1;
        }

        [Local]
        void CheckAliceExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if (!ExtraWin) return;
            if (!Extra) return;
            if (MyPlayer.IsDead) return;
            ev.SetWin(true);
            ev.ExtraWinMask.Add(UchuGameEnd.ObolusExtra);
        }
        bool RuntimeRole.HasImpostorVision => ImpostorVision;
        bool RuntimeRole.IgnoreBlackout => BlackOutVision;

        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.CanMoveInVent => true;
    }
}

/*[NebulaPreprocess(PreprocessPhase.PostRoles)]
internal class ObolusTeamAllocator : AbstractModule<Game>, IGameOperator
{
    static private void Preprocess(NebulaPreprocessor preprocessor)
    {
        preprocessor.DIManager.RegisterModule(() => new ObolusTeamAllocator());
    }

    private ObolusTeamAllocator()
    {
        this.RegisterPermanently();
    }

    void OnFixAssignmentTable(PreFixAssignmentEvent ev)
    {
        var oboluses = ev.RoleTable.GetPlayers(ObolusU.MyRole).ToArray();

        for (int i = 0; i < oboluses.Length; i++)
        {
            ev.RoleTable.EditRole(oboluses[i], (last) =>
            {
                // argument[0] = TeamId
                if (last.argument.Length < 1)
                    return (last.role, [i]);

                last.argument[0] = i;
                return (last.role, last.argument);
            });
        }
    }
}*/