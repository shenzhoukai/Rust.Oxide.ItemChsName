using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Linq;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ItemChsName", "CainSZK", "22.11.14")]
    [Description("A plugin to get rust item's Chinese name and with online update")]
    class ItemChsName : RustPlugin
    {
        /* 
            This Plugin is supported by XiuZheZhiJia. It contains updating Chinese dictionary online.
            For more content, please view XiuZheZhiJia's official website: https://rust.phellytech.com.
            本插件由锈者之家提供，支持联网同步更新最新中文名字典
            更多精彩内容，请访问锈者之家官网：https://rust.phellytech.com
        */
        private const string _jsonItemChsNameUri = "https://cdn.phellytech.com/rust/json/ItemChsName.json";
        private const string _jsonItemChsNameUpdateUri = "https://cdn.phellytech.com/rust/json/ItemChsNameUpdate.json";
        private const string CmdGetItemChsName = "itemchsname";
        private const string LangChatResp = "ItemCode: {0}, ItemChsName: {1}";
        private const string LangGetItemChsNameFailed = "# Get Failed #";
        private string strOnlineUpdateDateTime = "";
        private static ItemChsName _instance;
        #region Config
        private Configuration _config;
        private StoredData _storedData;
        private class Configuration
        {
            [JsonProperty("Update Config")]
            public UpdateConfig UpdateCfg = new UpdateConfig();
            public class UpdateConfig
            {
                [JsonProperty(PropertyName = "Update DateTime")]
                public string UpdateDateTime = "";
            }
        }
        private class StoredData
        {
            public static StoredData Load()
            {
                var data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(_instance.Name);
                if (data == null)
                {
                    _instance.PrintWarning($"Data file {_instance.Name}.json is invalid. Creating new data file.");
                    data = new StoredData();
                    data.Save();
                }
                return data;
            }

            [JsonProperty("ItemChsName List")]
            public string ItemChsNameList = string.Empty;

            public void Save() =>
                Interface.Oxide.DataFileSystem.WriteObject(_instance.Name, this);
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Chat Resp"] = LangChatResp,
                ["Get Item Chs Name Failed"] = LangGetItemChsNameFailed
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Chat Resp"] = "道具代码：{0}，道具中文名：{1}",
                ["Get Item Chs Name Failed"] = "# 获取失败 #"
            }, this, "zh-CN");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
            SaveConfig();
        }
        protected override void SaveConfig() => Config.WriteObject(_config);
        protected override void LoadDefaultConfig()
        {
            _config = new Configuration{};
            SaveConfig();
        }
        #endregion
        private void Init()
        {
            Puts("Plugin ItemChsName Debug Start");
            _instance = this;
            CheckItemChsNameUpdate();
            _storedData = StoredData.Load();
            CheckItemChsNameListCount();
            Puts("Plugin ItemChsName Debug End");
        }
        private void PlayerMsg(BasePlayer player,string strMsg)
        {
            if(!string.IsNullOrEmpty(strMsg))
            {
                IPlayer p = player.IPlayer;
                p.Reply(strMsg);
            }
        }
        private void PlayerMsgBySteamID(string strSteamID,string strMsg)
        {
            IEnumerable<BasePlayer> userOnline = GetOnlineUserList();
            foreach(BasePlayer player in userOnline)
            {
                if(player.UserIDString == strSteamID)
                {
                    PlayerMsg(player,strMsg);
                    break;
                }
            }
        }
        #region Commands
        [ConsoleCommand(CmdGetItemChsName)]
        private void ConsoleGetItemChsName(ConsoleSystem.Arg arg)
        {
            //---- Command Format ----//
            // /itemchsname {ItemCode}
            //---- Command Format ----//
            string[] args = arg.Args;
            if(args.Length != 1)
            {
                //Invalid Parameter Numbers
                return;
            }
            string strItemCode = args[0];
            BasePlayer player = arg.Player();
            if(player==null)
            {
                //Console Side Action
                ConsoleGetItemChsName(strItemCode);
                return;
            }
            //Client Side Action
            PlayerGetItemChsName(player, strItemCode);
        }
        [ChatCommand(CmdGetItemChsName)]
        private void ChatGetItemChsName(BasePlayer player, string command, string[] args)
        {
            //---- Command Format ----//
            // /itemchsname {ItemCode}
            //---- Command Format ----//
            if(args.Length != 1)
            {
                //Invalid Parameter Numbers
                return;
            }
            string strItemCode = args[0];
            if(player==null)
            {
                //Console Side Action
                ConsoleGetItemChsName(strItemCode);
                return;
            }
            //Client Side Action
            PlayerGetItemChsName(player, strItemCode);
        }
        #endregion
        #region Functions
        private void CheckItemChsNameListCount()
        {
            string strListCount = "Loaded Rust Item Chs Name. Count: {0}";
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(_storedData.ItemChsNameList);
                Puts(string.Format(strListCount,jo.Count));
            }
            catch
            {
                Puts(string.Format(strListCount,0));
            }
        }
        private IEnumerable<BasePlayer> GetOnlineUserList()
        {
            IEnumerable<BasePlayer> userOnline = Player.Players.Where(X => !X.IsNpc && !Player.IsBanned(X.userID));
            return userOnline.OrderBy(x => x.displayName).ThenBy(x => x.userID);
        }
        private void ConsoleGetItemChsName(string strItemCode)
        {
            string strItemChsName = ServerGetItemChsName(strItemCode);
            string strChatResp = string.Format(LangChatResp, strItemCode, strItemChsName);
            Puts(strChatResp);
        }
        private void PlayerGetItemChsName(BasePlayer player, string strItemCode)
        {
            string strItemChsName = ClientGetItemChsName(player, strItemCode);
            string strSteamID = player.UserIDString;
            string strChatResp = string.Format(lang.GetMessage("Chat Resp", this, player.IPlayer.Id), strItemCode, strItemChsName);
            PlayerMsgBySteamID(strSteamID,strChatResp);
        }
        /*
            Please use this function as API 
            Trial: 
                [PluginReference] Plugin ItemChsName;
                string strItemCode = "wood";
                string strItemChsName = ItemChsName?.Call("ServerGetItemChsName", strItemCode) as string;
                //then strItemChsName will be valued as "木头"
        */
        private string ServerGetItemChsName(string strItemCode)
        {
            string strItemChsName = string.Empty;
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(_storedData.ItemChsNameList);
                strItemChsName = jo[strItemCode].ToString();
            }
            catch
            {
                strItemChsName = LangGetItemChsNameFailed;
            }
            if(string.IsNullOrEmpty(strItemChsName))
            {
                strItemChsName = LangGetItemChsNameFailed;
            }
            return strItemChsName;
        }
        private string ClientGetItemChsName(BasePlayer player, string strItemCode)
        {
            string strItemChsName = string.Empty;
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(_storedData.ItemChsNameList);
                strItemChsName = jo[strItemCode].ToString();
            }
            catch
            {
                strItemChsName = lang.GetMessage("Get Item Chs Name Failed", this, player.IPlayer.Id);
            }
            if(string.IsNullOrEmpty(strItemChsName))
            {
                strItemChsName = lang.GetMessage("Get Item Chs Name Failed", this, player.IPlayer.Id);
            }
            return strItemChsName;
        }
        private void CheckItemChsNameUpdate()
        {
            try
            {
                webrequest.Enqueue(_jsonItemChsNameUpdateUri, null, ValidItemChsNameUpdate, this, RequestMethod.GET);
            }
            catch (Exception ex)
            {
                Puts($"Exception encountered while attempting to get itemChsName Update: {ex}");
            }
        }
        private void ValidItemChsNameUpdate(int code, string response)
        {
            if (code != 200 || response == null)
            {
                Puts($"Check itemChsName update failed. Code: {code}");
                return;
            }
            JObject jo = (JObject)JsonConvert.DeserializeObject(response);
            strOnlineUpdateDateTime = jo["UpdateDateTime"].ToString();
            if(string.IsNullOrEmpty(_config.UpdateCfg.UpdateDateTime))
            {
                //Plugin First Loaded.
                DownloadItemChsName();
            }
            else
            {
                if(strOnlineUpdateDateTime != _config.UpdateCfg.UpdateDateTime)
                {
                    DownloadItemChsName();
                }
            }
        }
        private void DownloadItemChsName()
        {
            try
            {
                webrequest.Enqueue(_jsonItemChsNameUri, null, SaveItemChsName, this, RequestMethod.GET);
            }
            catch (Exception ex)
            {
                Puts($"Exception encountered while attempting to get itemChsName: {ex}");
            }
        }
        private void SaveItemChsName(int code, string response)
        {
            if (code != 200 || response == null)
            {
                Puts($"Check itemChsName failed. Code: {code}");
                return;
            }
            _storedData.ItemChsNameList = response;
            _storedData.Save();
            _config.UpdateCfg.UpdateDateTime = strOnlineUpdateDateTime;
            SaveConfig();
        }
        #endregion
    }
}