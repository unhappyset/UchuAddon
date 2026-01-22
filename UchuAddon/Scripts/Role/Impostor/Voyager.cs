using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hori.Core;
using Nebula;
using Nebula.Behavior;
using Nebula.Game;
using Nebula.Modules;
using Nebula.Modules.GUIWidget;
using Nebula.Modules.ScriptComponents;
using Nebula.Player;
using Nebula.Roles.Impostor;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Compat;
using Virial.Configuration;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Virial.Media;
using Virial.Text;
using Color = UnityEngine.Color;
using Image = Virial.Media.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Hori.Scripts.Role.Impostor;
public class VoyagerU : DefinedSingleAbilityRoleTemplate<VoyagerU.Ability>, DefinedRole,IAssignableDocument
{
    public VoyagerU() : base("voyagerU", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [DiveCooldown,DiveDuration,SpeedRatio])
    {
    }

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static private FloatConfiguration DiveCooldown = NebulaAPI.Configurations.Configuration("options.role.voyagerU.diveCooldown", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration DiveDuration = NebulaAPI.Configurations.Configuration("options.role.voyagerU.diveDuration", (2.5f, 30f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SpeedRatio = NebulaAPI.Configurations.Configuration("options.role.voyagerU.speedRatio", (1f, 5f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);
    public static TranslatableTag paradox = new("state.paradox");

    static public VoyagerU MyRole = new();

    static private readonly Virial.Media.Image DiveImage = NebulaAPI.AddonAsset.GetResource("PDiveButton.png")!.AsImage(115f)!;
    static private readonly Virial.Media.Image AwayImage = NebulaAPI.AddonAsset.GetResource("PAwayButton.png")!.AsImage(115f)!;

    bool IAssignableDocument.HasTips => false;
    bool IAssignableDocument.HasAbility => true;
    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new(DiveImage, "role.voyagerU.ability.dive");
        yield return new(AwayImage, "role.voyagerU.ability.away");
    }

    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public bool KillIgnoreTeam { get; private set; } = false;
        bool isDiving = false;
        float ParadoxTimer = DiveDuration;
        private TMPro.TextMeshPro? hudText;
        private SpriteRenderer? fullScreen;

        public Ability(GamePlayer player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                var diveButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, DiveCooldown, "pdiver.dive", DiveImage).SetAsUsurpableButton(this);
                var awayButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, 0f, "pdiver.away", AwayImage).SetAsUsurpableButton(this);
                diveButton.Visibility = (button) => !isDiving && !MyPlayer.IsDead;
                awayButton.Visibility = (button) => isDiving && !MyPlayer.IsDead;

                diveButton.Availability = (button) => MyPlayer.CanMove;
                awayButton.Availability = (button) => MyPlayer.CanMove;

                diveButton.OnClick = (button) => button.StartEffect();
                diveButton.OnEffectStart = (button) =>
                {
                    MyPlayer.GainAttribute(PlayerAttributes.InvisibleElseImpostor, 9999999999999f, false, 1, "pdiverU:invisible");
                    MyPlayer.GainSpeedAttribute(SpeedRatio, 9999999999999f, true, 0, "pdiverU:speed");
                    isDiving = true;
                    KillIgnoreTeam = true;
                    AmongUsUtil.PlayCustomFlash(Color.magenta, 0f, 0.25f, 0.4f);
                    NebulaAsset.PlaySE(NebulaAudioClip.SnatcherSuccess, false);
                    MyPlayer.GainAttribute(PlayerAttributes.Roughening, 1f, false, 5, "pdiverU:mosaic");

                    ShufflePlayerOutfits();
                };
                awayButton.OnClick = (button) => button.StartEffect();
                awayButton.OnEffectStart = (button) =>
                {
                    isDiving = false;
                    KillIgnoreTeam = false;
                    ParadoxTimer = DiveDuration;

                    MyPlayer.RemoveAttributeByTag("pdiverU:speed");
                    MyPlayer.RemoveAttributeByTag("pdiverU:invisible");

                    diveButton.StartCoolDown();

                    ResetShuffledOutfits();

                    if (fullScreen != null)
                    {
                        fullScreen.color = Color.clear; 
                    }
                };
                fullScreen = GameObject.Instantiate(HudManager.Instance.FullScreen, HudManager.Instance.transform);
                fullScreen.color = Color.magenta.AlphaMultiplied(0f); // 初期は透明
                fullScreen.gameObject.SetActive(true);
                this.BindGameObject(fullScreen.gameObject);

                hudText = Nebula.Utilities.Helpers.TextHudContent("ParadoxTimeHUD", this, (tmPro) =>
                {
                    tmPro.text = "";
                }, true);
            }
        }

        void Update(GameUpdateEvent ev)
        {
            if (!isDiving) return;

            if (fullScreen != null)
            {
                float targetAlpha = isDiving ? 0.25f : 0f;
                float currentAlpha = fullScreen.color.a;
                currentAlpha += (targetAlpha - currentAlpha) * Time.deltaTime * 4f; 
                fullScreen.color = Color.magenta.AlphaMultiplied(currentAlpha);
            }

            if (hudText != null)
            {
                string label = NebulaAPI.Language.Translate("role.voyagerU.paradoxTimerText");

                if (ParadoxTimer >= 1)
                {
                    hudText.text = $"{label}: {ParadoxTimer:0.0}";
                }
                else
                {
                    hudText.text = $"<color=#FF0000>{label}: {ParadoxTimer:0.0}";
                }
            }
            if (MyPlayer == null || MyPlayer.IsDead || !MyPlayer.CanMove || MeetingHud.Instance) return;
            ParadoxTimer -= Time.deltaTime;
            if (ParadoxTimer <= 0f)
            {
                MyPlayer.Suicide(PlayerState.Suicide, paradox, KillParameter.NormalKill, null);
                return;
            }
        }

        private void ShufflePlayerOutfits()
        {
            var alives = NebulaGameManager.Instance!.AllPlayerInfo.Where(p => !p.IsDead && !p.AmOwner).ToArray();
            var randomArray = Helpers.GetRandomArray(alives.Length);
            int maxPairs = Mathf.Min(4, alives.Length / 2);

            for (int i = 0; i < maxPairs; i++)
            {
                var player1 = alives[randomArray[i * 2]];
                var player2 = alives[randomArray[i * 2 + 1]];
                var outfit1 = player1.GetOutfit(50);
                var outfit2 = player2.GetOutfit(50);

                player1.Unbox().RemoveOutfit("Confused");
                player2.Unbox().RemoveOutfit("Confused");

                player1.Unbox().AddOutfit(new(outfit2, "Confused", OutfitPriority.Confused, false));
                player2.Unbox().AddOutfit(new(outfit1, "Confused", OutfitPriority.Confused, false));
            }
        }

        private void ResetShuffledOutfits()
        {
            var alives = NebulaGameManager.Instance!.AllPlayerInfo
                .Where(p => !p.IsDead && !p.AmOwner).ToArray();

            foreach (var player in alives)
            {
                player.Unbox().RemoveOutfit("Confused");
            }
        }
    }
}