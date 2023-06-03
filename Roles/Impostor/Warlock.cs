using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hazel;

using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;
using static TOHE.Translator;
using TOHE.Modules;

namespace TOHE.Roles.Impostor;
public sealed class Warlock : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Warlock),
            player => new Warlock(player),
            CustomRoles.Warlock,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1400,
            SetupOptionItem,
            "wa"
        );
    public Warlock(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    public override void OnDestroy()
    {
        CursedPlayer = null;
    }
    static OptionItem OptionCanKillAllies;
    static OptionItem OptionCanKillSelf;

    PlayerControl CursedPlayer;
    bool IsCursed;
    bool Shapeshifting;
    private static void SetupOptionItem()
    {
        OptionCanKillAllies = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanKillAllies, false, false);
        OptionCanKillSelf = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanKillSelf, false, false);
    }
    public override void Add()
    {
        CursedPlayer = null;
        IsCursed = false;
        Shapeshifting = false;
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SyncWarlock);
        sender.Writer.Write(IsCursed);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SyncWarlock) return;
        IsCursed = reader.ReadBoolean();
    }
    public bool OverrideKillButtonText(out string text)
    {
        if (!Shapeshifting)
        {
            text = GetString("WarlockCurseButtonText");
            return true;
        }
        else
        {
            text = default;
            return false;
        }
    }
    public override bool OverrideAbilityButtonText(out string text)
    {
        text = GetString("WarlockShapeshiftButtonText");
        return !Shapeshifting && IsCursed;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = IsCursed ? 1f : Options.DefaultKillCooldown;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        //自殺なら関係ない
        if (info.IsSuicide) return true;

        var (killer, target) = info.AttemptTuple;
        if (!Shapeshifting)
        {//変身してない
            if (!IsCursed)
            {//まだ呪っていない
                IsCursed = true;
                SendRPC();
                CursedPlayer = target;
                //呪える相手は一人だけなのでキルボタン無効化
                killer.SetKillCooldown(255f);
            }
            //どちらにしてもキルは無効
            return false;
        }
        //変身中は通常キル
        return true;
    }
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (Shapeshifting)
        {///変身時
            if (CursedPlayer != null && CursedPlayer.IsAlive())
            {//呪っていて対象がまだ生きていたら
                Vector2 cpPos = CursedPlayer.transform.position;
                Dictionary<PlayerControl, float> candidateList = new();
                float distance;
                foreach (PlayerControl candidatePC in Main.AllAlivePlayerControls)
                {
                    if (candidatePC.PlayerId == CursedPlayer.PlayerId) continue;
                    if (Is(candidatePC) && !OptionCanKillSelf.GetBool()) continue;
                    if ((candidatePC.Is(CustomRoleTypes.Impostor) || candidatePC.Is(CustomRoles.Madmate)) && !OptionCanKillAllies.GetBool()) continue;
                    distance = Vector2.Distance(cpPos, candidatePC.transform.position);
                    candidateList.Add(candidatePC, distance);
                    Logger.Info($"{candidatePC?.Data?.PlayerName}の位置{distance}", "Warlock.OnShapeshift");
                }
                if (candidateList.Count >= 1)
                {
                    var nearest = candidateList.OrderBy(c => c.Value).FirstOrDefault();
                    var killTarget = nearest.Key;

                    var killed = false;
                    CustomRoleManager.OnCheckMurder(
                        Player, killTarget,
                        CursedPlayer, killTarget,
                        () => killed = true
                        );

                    if (killed)
                    {
                        killTarget.SetRealKiller(Player);
                        RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                    }
                    else
                    {
                        Player.Notify(GetString("WarlcokKillFaild"));
                    }

                    Logger.Info($"{killTarget.GetNameWithRole()} 被操控击杀", "Warlock.OnShapeshift");
                    
                }
                else
                {
                    Player.Notify(GetString("WarlockNoTarget"));
                }
                Player.SetKillCooldownV2();
                CursedPlayer = null;
            }
        }
        else
        {
            if (IsCursed)
            {
                //ShapeshifterCooldownを通常に戻す
                IsCursed = false;
                SendRPC();
                Player.SyncSettings();
                Player.RpcResetAbilityCooldown();
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        CursedPlayer = null;
    }
}