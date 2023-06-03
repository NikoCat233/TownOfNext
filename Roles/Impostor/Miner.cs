﻿using AmongUs.GameOptions;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Impostor;
public sealed class Miner : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Miner),
            player => new Miner(player),
            CustomRoles.Miner,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            904190,
            null,
            "mn"
        );
    public Miner(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override bool OverrideAbilityButtonText(out string text)
    {
        text = Translator.GetString("MinerTeleButtonText");
        return Main.LastEnteredVent.ContainsKey(Player.PlayerId);
    }
    public override void OnShapeshift(PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (Main.LastEnteredVent.ContainsKey(Player.PlayerId))
        {
            Utils.TP(Player.NetTransform, Main.LastEnteredVentLocation[Player.PlayerId]);
            Logger.Msg($"矿工传送：{Player.GetNameWithRole()}", "Miner.OnShapeshift");
        }
    }
}