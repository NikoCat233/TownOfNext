﻿using AmongUs.GameOptions;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Impostor;
public sealed class Bard : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Bard),
            player => new Bard(player),
            CustomRoles.Bard,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            901234,
            null,
            "ba"
        );

    public Bard(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private float KillCooldown;
    public override void Add() => KillCooldown = Options.DefaultKillCooldown;
    public float CalculateKillCooldown() => KillCooldown;
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (exiled != null) KillCooldown /= 2;
    }
}