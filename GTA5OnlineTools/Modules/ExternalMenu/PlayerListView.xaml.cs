﻿using GTA5OnlineTools.Common.Utils;
using GTA5OnlineTools.Features.SDK;
using GTA5OnlineTools.Features.Core;
using GTA5OnlineTools.Features.Data;

namespace GTA5OnlineTools.Modules.ExternalMenu;

/// <summary>
/// PlayerListView.xaml 的交互逻辑
/// </summary>
public partial class PlayerListView : UserControl
{
    // 显示玩家列表
    private List<NetPlayerData> NetPlayerDatas = new();

    public PlayerListView()
    {
        InitializeComponent();
        this.DataContext = this;
        ExternalMenuWindow.WindowClosingEvent += ExternalMenuWindow_WindowClosingEvent;
    }

    private void ExternalMenuWindow_WindowClosingEvent()
    {

    }

    private void Button_RefreshPlayerList_Click(object sender, RoutedEventArgs e)
    {
        AudioUtil.PlayClickSound();

        lock (this)
        {
            NetPlayerDatas.Clear();
            ListBox_PlayerList.Items.Clear();

            long pCNetworkPlayerMgr = Memory.Read<long>(Pointers.NetworkPlayerMgrPTR);

            for (int i = 0; i < 32; i++)
            {
                long pCNetGamePlayer = Memory.Read<long>(pCNetworkPlayerMgr + Offsets.CNetworkPlayerMgr_CNetGamePlayer + i * 0x08);
                if (!Memory.IsValid(pCNetGamePlayer))
                    continue;

                long pCPlayerInfo = Memory.Read<long>(pCNetGamePlayer + Offsets.CNetworkPlayerMgr_CNetGamePlayer_CPlayerInfo);
                if (!Memory.IsValid(pCPlayerInfo))
                    continue;

                long pCPed = Memory.Read<long>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_CPed);
                if (!Memory.IsValid(pCPed))
                    continue;

                byte[] relayIP = new byte[4];
                byte[] externalIP = new byte[4];
                byte[] internalIP = new byte[4];

                for (int j = 0; j < 4; j++)
                {
                    relayIP[j] = Memory.Read<byte>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_RelayIP + j);
                }
                short relayPort = Memory.Read<short>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_RelayPort);

                for (int j = 0; j < 4; j++)
                {
                    externalIP[j] = Memory.Read<byte>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_ExternalIP + j);
                }
                short externalPort = Memory.Read<short>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_ExternalPort);

                for (int j = 0; j < 4; j++)
                {
                    internalIP[j] = Memory.Read<byte>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_InternalIP + j);
                }
                short internalPort = Memory.Read<short>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_InternalPort);

                ////////////////////////////////////////////

                NetPlayerDatas.Add(new NetPlayerData()
                {
                    RockstarId = Memory.Read<long>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_RockstarID),
                    PlayerName = Memory.ReadString(pCPlayerInfo + Offsets.CPed_CPlayerInfo_Name, 20),

                    Health = Memory.Read<float>(pCPed + Offsets.CPed_Health),
                    MaxHealth = Memory.Read<float>(pCPed + Offsets.CPed_HealthMax),
                    Armor = Memory.Read<float>(pCPed + Offsets.CPed_Armor),
                    GodMode = Memory.Read<byte>(pCPed + Offsets.CPed_God) != 0x00,
                    NoRagdoll = Memory.Read<byte>(pCPed + Offsets.CPed_Ragdoll) != 0x20,

                    Position = Memory.Read<Vector3>(pCPed + Offsets.CPed_VisualX),

                    WantedLevel = Memory.Read<byte>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_WantedLevel),
                    WalkSpeed = Memory.Read<float>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_WalkSpeed),
                    RunSpeed = Memory.Read<float>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_RunSpeed),
                    SwimSpeed = Memory.Read<float>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_SwimSpeed),
                    WantedCanChange = Memory.Read<float>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_WantedCanChange) != 0x00,
                    NPCIgnore = Memory.Read<int>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_NPCIgnore),
                    FrameFlags = Memory.Read<int>(pCPlayerInfo + Offsets.CPed_CPlayerInfo_FrameFlags),

                    ClanName = Memory.ReadString(pCNetGamePlayer + Offsets.CNetworkPlayerMgr_CNetGamePlayer_ClanName, 20),
                    ClanTag = Memory.ReadString(pCNetGamePlayer + Offsets.CNetworkPlayerMgr_CNetGamePlayer_ClanTag, 20),

                    IsSpectating = Memory.Read<byte>(pCNetGamePlayer + Offsets.CNetworkPlayerMgr_CNetGamePlayer_IsSpectating) != 0x00,
                    IsRockStarDev = Memory.Read<byte>(pCNetGamePlayer + Offsets.CNetworkPlayerMgr_CNetGamePlayer_IsRockStarDev) != 0x00,
                    IsRockStarQA = Memory.Read<byte>(pCNetGamePlayer + Offsets.CNetworkPlayerMgr_CNetGamePlayer_IsRockStarQA) != 0x00,
                    IsCheater = Memory.Read<byte>(pCNetGamePlayer + Offsets.CNetworkPlayerMgr_CNetGamePlayer_IsCheater) != 0x00,

                    RelayIP = $"{relayIP[3]}.{relayIP[2]}.{relayIP[1]}.{relayIP[0]} : {relayPort}",
                    ExternalIP = $"{externalIP[3]}.{externalIP[2]}.{externalIP[1]}.{externalIP[0]} : {externalPort}",
                    InternalIP = $"{internalIP[3]}.{internalIP[2]}.{internalIP[1]}.{internalIP[0]} : {internalPort}",

                });
            }

            var index = 0;
            foreach (var item in NetPlayerDatas)
            {
                var url = "https://prod.cloud.rockstargames.com/members/sc/5605/" + item.RockstarId + "/publish/gta5/mpchars/0.png";

                ListBox_PlayerList.Items.Add(new NetPlayer()
                {
                    Index = ++index,
                    Avatar = url,
                    Name = string.IsNullOrEmpty(item.ClanTag) ? item.PlayerName : $"{item.PlayerName} [{item.ClanTag}]",
                    RID = item.RockstarId,
                    GodMode = item.GodMode ? "无敌" : ""
                });
            }
        }
    }

    private void Button_TeleportSelectedPlayer_Click(object sender, RoutedEventArgs e)
    {
        AudioUtil.PlayClickSound();

        if (ListBox_PlayerList.SelectedItem != null)
        {
            var index = ListBox_PlayerList.SelectedIndex;
            if (index != -1)
            {
                Teleport.SetTeleportPosition(NetPlayerDatas[index].Position);
            }
        }
    }

    private void PlayerInfoAppend(string title, string value)
    {
        ListBox_PlayerInfo.Items.Add(new EmojiMenu()
        {
            Emoji = string.Empty,
            Title = title,
            Value = value
        });
    }

    private string BoolToString(bool value)
    {
        return value ? "ON" : "OFF";
    }

    private void ListBox_PlayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ListBox_PlayerInfo.Items.Clear();

        var index = ListBox_PlayerList.SelectedIndex;
        if (index != -1)
        {
            var item = NetPlayerDatas[index];

            PlayerInfoAppend("玩家昵称", $"{item.PlayerName}");
            PlayerInfoAppend("玩家RID", $"{item.RockstarId}");
            PlayerInfoAppend("玩家帮会", $"{item.ClanTag}");
            PlayerInfoAppend("玩家帮会名称", $"{item.ClanName}");

            PlayerInfoAppend("护甲值", $"{item.Armor:0.0}");
            PlayerInfoAppend("生命值", $"{item.Health:0.0}");
            PlayerInfoAppend("最大生命值", $"{item.MaxHealth:0.0}");

            PlayerInfoAppend("无敌状态", BoolToString(item.GodMode));
            PlayerInfoAppend("无布娃娃", BoolToString(item.NoRagdoll));
            PlayerInfoAppend("通缉等级", $"{item.WantedLevel}");

            PlayerInfoAppend("步行速度", $"{item.WalkSpeed:0.0}");
            PlayerInfoAppend("奔跑速度", $"{item.RunSpeed:0.0}");
            PlayerInfoAppend("游泳速度", $"{item.SwimSpeed:0.0}");
            PlayerInfoAppend("永不通缉", BoolToString(item.WantedCanChange));
            PlayerInfoAppend("NPC忽略", $"{item.NPCIgnore}");
            PlayerInfoAppend("帧标志", $"{item.FrameFlags}");

            PlayerInfoAppend("坐标数据", $"{item.Position.X:0.000}, {item.Position.Y:0.000}, {item.Position.Z:0.000}");

            PlayerInfoAppend("是否在观看", BoolToString(item.IsSpectating));
            PlayerInfoAppend("是否为R星开发者", BoolToString(item.IsRockStarDev));
            PlayerInfoAppend("是否为R星QA", BoolToString(item.IsRockStarQA));
            PlayerInfoAppend("是否为作弊者", BoolToString(item.IsCheater));

            PlayerInfoAppend("中继IP地址", $"{item.RelayIP}");
            PlayerInfoAppend("外部IP地址", $"{item.ExternalIP}");
            PlayerInfoAppend("内部IP地址", $"{item.InternalIP}");

        }
    }
}
