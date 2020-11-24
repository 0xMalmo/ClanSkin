using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;

namespace Oxide.Plugins
{

    [Info("Clan Skin", "Malmo", "1.0.0")]
    [Description("Allows players to set default skins for it's team or clan")]
    class ClanSkin : RustPlugin
    {

        #region References

        [PluginReference] Plugin Clans;

        #endregion

        #region Initialization

        private const string permallow = "clanskin.save";

        void Init()
        {
            config = Config.ReadObject<Configuration>();

            permission.RegisterPermission(permallow, this);

            cmd.AddChatCommand("clanskin", this, "CmdDress");
        }

        #endregion

        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Exclude Items (these items will not be affected by this plugin)")]
            public string[] ExcludedItems = new string[] {
                "hat.beenie"
            };
        }

        protected override void SaveConfig()
        {
            PrintToConsole($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["HelpText1"] = "usage: /clanskin [argument]",
                ["HelpText2"] = "<color=orange>/clanskin edit</color> will activate edit mode (any clothes you put on will not be skinned automatically)",
                ["HelpText3"] = "<color=orange>/clanskin save</color> will save the current skins you're wearing to your team/clan profile",
                ["EditModeEntered"] = "You can now edit your dress. When you're done run <color=orange>/clanskin save</color> to save",
                ["NotInEditMode"] = "You're not in edit mode. run <color=orange>/clanskin edit</color> first",
                ["Saved"] = "Clan skins saved to profile",
                ["NothingToSave"] = "You are not wearing any skinnable attire",
                ["NoPerms"] = "You don't have permission to use this command"
            }, this);
        }

        #endregion

        #region Hooks

        void Loaded()
        {
            ReadDataFile();
        }

        object CanWearItem(PlayerInventory inventory, Item item, int targetSlot)
        {
            TryApplySkin(inventory._baseEntity, item);

            return null;
        }

        void OnClanDestroy(string tag)
        {
            DeleteTeamObject(tag);
        }

        void OnTeamDisbanded(RelationshipManager.PlayerTeam team)
        {
            DeleteTeamObject(team.teamID.ToString());
        }

        void OnServerSave()
        {
            WriteDataFile();
        }

        #endregion

        #region Command

        private void CmdDress(BasePlayer player, string command, string[] args)
        {

            if (!HasPerm(player.UserIDString, permallow))
            {
                Message(player.IPlayer, "NoPerms");
                return;
            }

            if (args.Length != 1)
            {
                PrintHelp(player);
                return;
            }

            var arg = args[0];

            var id = GetTeamId(player);

            switch (arg)
            {
                case "save":
                    SaveSkins(player);
                    break;

                case "edit":
                    SetEdit(id, true);
                    Message(player.IPlayer, "EditModeEntered");
                    break;

                case "help":
                default:
                    PrintHelp(player);
                    break;
            }

        }

        private void PrintHelp(BasePlayer player)
        {
            var message = new string[] {
                "HelpText1",
                "HelpText2",
                "HelpText3"
            };

            foreach (var row in message)
            {
                Message(player.IPlayer, row);
            }
        }

        private void SaveSkins(BasePlayer player)
        {
            var id = GetTeamId(player);

            if (!IsEditMode(id))
            {
                Message(player.IPlayer, "NotInEditMode");
                return;
            }

            var itemsWritten = 0;

            foreach (var item in player.inventory.containerWear.itemList)
            {
                var itemName = GetName(item);

                if (IsSkinnable(item))
                {
                    SetSkin(id, itemName, item.skin);
                    itemsWritten++;
                }
            }

            if (itemsWritten > 0)
            {
                SetEdit(id, false);
                Message(player.IPlayer, "Saved");
            }
            else
            {
                Message(player.IPlayer, "NothingToSave");
            }
        }

        #endregion

        #region Utility methods

        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        private bool IsSkinnable(Item item) => item.info.HasSkins && !config.ExcludedItems.Contains(GetName(item));

        private string GetName(Item item) => item.info.shortname;

        private string GetLang(string langKey, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(langKey, this, playerId), args);
        }

        private void Message(IPlayer player, string langKey, params object[] args)
        {
            if (player.IsConnected) player.Message(GetLang(langKey, player.Id, args));
        }

        private void SetEdit(string id, bool value)
        {
            var team = EnsureTeamObject(id);

            team.EditMode = value;
        }

        private bool IsEditMode(string id)
        {
            var team = GetTeamObject(id);

            if (team == null)
            {
                return true;
            }

            return team.EditMode;
        }

        private string GetTeamId(BasePlayer player)
        {
            var playerClan = Clans?.Call<string>("GetClanOf", player);
            if (playerClan != null)
            {
                return playerClan;
            }

            if (player.Team != null)
            {
                return player.Team.teamID.ToString();
            }

            return player.userID.ToString();
        }

        private void DeleteTeamObject(string teamId)
        {
            if (data.Teams.ContainsKey(teamId))
            {
                data.Teams.Remove(teamId);
            }
        }

        private void SetSkin(string id, string item, ulong skin)
        {
            var team = EnsureTeamObject(id);

            if (team.Skins.ContainsKey(item))
            {
                team.Skins[item] = skin;
            }
            else
            {
                team.Skins.Add(item, skin);
            }
        }

        private ulong GetSkin(string id, Item item)
        {
            var team = EnsureTeamObject(id);
            var itemName = GetName(item);

            if (team.Skins.ContainsKey(itemName))
            {
                return team.Skins[itemName];
            }

            return item.skin;
        }

        private void TryApplySkin(BasePlayer player, Item item)
        {
            if (!IsSkinnable(item))
            {
                return;
            }

            var id = GetTeamId(player);

            if (IsEditMode(id))
            {
                return;
            }

            var team = GetTeamObject(id);

            if (team == null)
            {
                return;
            }

            var skinToApply = GetSkin(id, item);

            item.skin = skinToApply;
        }

        private TeamData EnsureTeamObject(string id)
        {
            if (!data.Teams.ContainsKey(id))
            {
                data.Teams.Add(id, new TeamData { EditMode = true, Skins = new Dictionary<string, ulong> { } });
            }

            return data.Teams[id];
        }

        private TeamData GetTeamObject(string id)
        {
            if (!data.Teams.ContainsKey(id))
            {
                return null;
            }

            return data.Teams[id];
        }

        #endregion

        #region Data Management

        private StorageData data;

        void ReadDataFile()
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<StorageData>(Name + "_data");

                if (data.Teams == null)
                {
                    data.Teams = new Dictionary<string, TeamData> { };
                }
            }
            catch { }
        }

        void WriteDataFile()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "_data", data);
        }

        private class StorageData
        {
            public Dictionary<string, TeamData> Teams { get; set; }
        }

        private class TeamData
        {
            public bool EditMode { get; set; }

            public Dictionary<string, ulong> Skins;
        }

        #endregion

    }
}
