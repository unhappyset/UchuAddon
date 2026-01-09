using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Hori.Scripts.Abilities;
using Nebula;
using Nebula.Behavior;
using Nebula.Extensions;
using Nebula.Game;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Modules.ScriptComponents;
using Nebula.Roles;
using Nebula.Utilities;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Text;
using static Nebula.Roles.Impostor.Cannon;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Crewmate;

public class OperatorU : DefinedSingleAbilityRoleTemplate<OperatorU.Ability>, DefinedRole
{
    public OperatorU() : base("operatorU", new Virial.Color(107, 242, 154), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfOperate,OperateAddTime,SkipBan])
    {
        ConfigurationHolder?.AddTags(AddonConfigurationTags.TagUchuAddon);
    }
    public static IntegerConfiguration NumOfOperate = NebulaAPI.Configurations.Configuration("options.role.operatorU.numOfOperate", (1, 15), 1);
    public static FloatConfiguration OperateAddTime = NebulaAPI.Configurations.Configuration("options.role.operatorU.operateAddTime", (10f, 160f, 5f), 50f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration  SkipBan = NebulaAPI.Configurations.Configuration("options.role.operatorU.skipBan", true);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public OperatorU MyRole = new();

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        static private Image OperateImage = NebulaAPI.AddonAsset.GetResource("OperateButton.png")!.AsImage(115f)!;
        int leftOperate = NumOfOperate;
        int AddTimer = (int)OperateAddTime;
        bool usedInTheMeeting = false;
        bool OperateSkill = false;
        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var ability = new EmergencyButtonAbility().Register(this);

                if (!OperateSkill) Nebula.Utilities.Helpers.TextHudContent("OperatorUText", this, (tmPro) => tmPro.text = NebulaAPI.Language.Translate("role.operatorU.hudText") + ": " + leftOperate, true);
                var OperateButton = new Nebula.Modules.ScriptComponents.ModAbilityButtonImpl(alwaysShow: true).Register(this);
                OperateButton.SetSprite(OperateImage.GetSprite());
                OperateButton.Availability = (button) => MeetingHud.Instance && MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted && leftOperate > 0;
                OperateButton.Visibility = (button) => !MyPlayer.IsDead && MeetingHud.Instance && (MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted || MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.Discussion) && !usedInTheMeeting && leftOperate > 0;
                OperateButton.SetLabel("operatorU.operate");
                OperateButton.OnClick = (button) =>
                {
                    usedInTheMeeting = true;
                    OperateSkill = true;
                    leftOperate--;
                    MeetingHud.Instance.ResetPlayerState();
                    NebulaAPI.CurrentGame!.CurrentMeeting?.EditMeetingTime(AddTimer);
                    if (SkipBan)
                    {
                        RpcOperateSkipBan.Invoke(MyPlayer);
                    }
                };

                OperateButton.OnMeeting = _ =>
                {
                    usedInTheMeeting = false;
                };
            }
        }

        void MeetingStart(MeetingPreStartEvent ev)
        {
            RpcOperateSkip.Invoke(MyPlayer);
        }

        void MeetingEnd(MeetingPreEndEvent ev)
        {
            OperateSkill = false;
        }

        RemoteProcess<GamePlayer> RpcOperateSkipBan= new("OperatorSkipBan", (message, _) =>
        {
            MeetingHud.Instance.SkipVoteButton.gameObject.SetActive(false);
        });

        RemoteProcess<GamePlayer> RpcOperateSkip = new("OperatorSkipRepair", (message, _) =>
        {
            MeetingHud.Instance.SkipVoteButton.gameObject.SetActive(true);
        });
    }
}