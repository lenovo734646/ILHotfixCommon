﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hotfix.Common
{
	public class GameConfig
	{
		public enum Tag
		{
			Hot = 1,
			MultiPlayer = 2,
			Fishing = 4,
			Slots = 8,
			Compete = 16,
			Card = 32,
		}

		public enum ScriptType
		{
			CSharp,
			Lua,
		}

		public enum Module
		{
			FLLU3d,
			YuanSan,
		}

		public enum GameID
		{
			Lobby = -1,
			ShenLingWuHui92 = 14,
			HuanleBY = 564,
			BenChiBaoMa = 37,
			FeiQingZhouSou = 64,
			BaiJiaLe = 35,
			JiuXianLaWang = 41,
			ShuiHuZhuan = 42,
			ZJH = 12,
		}

		public const string HuanleBY = "HuanleBY";
		public const string Lobby = "Lobby";
		public const string BaiRenNiuniu = "BRNN";
		public const string BaiJiaLe = "BJL";
		public const string BenChiBaoMa = "BCBM";
		public const string FeiQingZhouSou = "FQZS";
		public const string HongHeiDaZhan = "HHDZ";
		public const string HuhuShengWei = "HHSW";
		public const string ShenLingWuHui = "SLWH";
		public const string DeZhouPuke = "DZPK";
		public const string DouDiZhu = "DDZN";
		public const string MaJiang2 = "MJ2";
		public const string QiangZhuangNiuniu = "QZNN";
		public const string ShiSanShui = "SSS";
		public const string TongBiNiuniu = "TBNN";
		public const string ZhaJingHua = "ZJH";
		public const string Bianlian2 = "BL2";
		public const string CaiShenDao = "CSD";
		public const string DaHuaXiYou = "DHXY";
		public const string DuoCaiDuofu = "DFDC";
		public const string HuoYuanLianji = "HYLJ";
		public const string JiuXianLaWang = "JXLW";
		public const string XingGanNvYou = "XGNY";
		public const string LuoMaDaMaoXian = "LMDMX";
		public const string ShuiGuoMali = "SGML";
		public const string ShuiHuZhuan = "SHZ";
		public const string JiJieHaoBY = "FishingJJH";
		public const string LingDianBY = "Fishing3D";
		public const string ShenHaiBY = "FishingSH";
		public const string CaoFangBY = "FishingCF";
		public const string LaBa3D = "LHJC";
		public string name, desc;
		public string folder;
		public string entryClass;
		public ScriptType scriptType = ScriptType.CSharp;
		public Module module = Module.FLLU3d;
		public int tag;
		public bool enabled = false, show = true;
		public string contentCatalog = "{0}/Games/{1}/{2}/catalog_{1}.json";
		public GameID gameID = GameID.Lobby;
		public bool isRunning = false;
		public string GetCatalogAddress(string host, string platform)
		{
			return string.Format(contentCatalog, host, folder, platform);
		}
	}

	public partial class Config
	{
		public Dictionary<string, GameConfig> games = new Dictionary<string, GameConfig>();
		public Dictionary<string, int> gameHosts = new Dictionary<string, int>();
		public float networkTimeout = 10.0f;

		public Dictionary<string, int> webRoots = new Dictionary<string, int>();

		//public string webRoots = "http://139.224.233.71:8083/koko-manage2/third/";

		public void Start()
		{
			gameHosts.Add("127.0.0.1", 8990);
			gameHosts.Add("192.168.102.110", 8990);
			gameHosts.Add("8.210.20.24", 8990);
			webRoots.Add("139.224.233.71", 8083);
			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.LingDianBY;
				game.folder = GameConfig.HuanleBY;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.YuanSan;
				game.tag = (int)GameConfig.Tag.Fishing;
				game.gameID = GameConfig.GameID.HuanleBY;
				game.desc = LangGame.LingDianBY;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.Lobby;
				game.folder = GameConfig.Lobby;
				game.entryClass = "Hotfix.Lobby.MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.contentCatalog = "{0}/{1}/{2}/catalog_{1}.json";
				games.Add(game.name, game);
				game.gameID = GameConfig.GameID.Lobby;
				game.desc = LangGame.Lobby;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.BaiRenNiuniu;
				game.folder = GameConfig.BaiRenNiuniu;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.MultiPlayer;
				games.Add(game.name, game);
				game.desc = LangGame.BaiRenNiuniu;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.BaiJiaLe;
				game.folder = GameConfig.BaiJiaLe;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.MultiPlayer | (int)GameConfig.Tag.Hot;
				game.gameID = GameConfig.GameID.BaiJiaLe;
				game.enabled = true;
				games.Add(game.name, game);
				game.desc = LangGame.BaiJiaLe;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.BenChiBaoMa;
				game.folder = GameConfig.BenChiBaoMa;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.MultiPlayer | (int)GameConfig.Tag.Hot;
				game.gameID = GameConfig.GameID.BenChiBaoMa;
				game.enabled = true;
				games.Add(game.name, game);
				game.desc = LangGame.BenChiBaoMa;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.FeiQingZhouSou;
				game.folder = GameConfig.FeiQingZhouSou;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.MultiPlayer | (int)GameConfig.Tag.Hot;
				game.gameID = GameConfig.GameID.FeiQingZhouSou;
				game.enabled = true;
				games.Add(game.name, game);
				game.desc = LangGame.FeiQingZhouSou;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.HongHeiDaZhan;
				game.folder = GameConfig.HongHeiDaZhan;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.MultiPlayer;
				game.show = false;
				games.Add(game.name, game);
				game.desc = LangGame.HongHeiDaZhan;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.HuhuShengWei;
				game.folder = GameConfig.HuhuShengWei;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.MultiPlayer | (int)GameConfig.Tag.Hot;
				game.show = false;
				games.Add(game.name, game);
				game.desc = LangGame.HuhuShengWei;
			}

			{
				GameConfig game = new GameConfig();
				game.gameID = GameConfig.GameID.ShenLingWuHui92;
				game.name = GameConfig.ShenLingWuHui;
				game.folder = GameConfig.ShenLingWuHui;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.MultiPlayer;

				game.enabled = true;
				games.Add(game.name, game);
				game.desc = LangGame.ShenLingWuHui;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.DeZhouPuke;
				game.folder = GameConfig.DeZhouPuke;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.MultiPlayer;
				game.show = false;
				games.Add(game.name, game);
				game.desc = LangGame.DeZhouPuke;
			}


			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.DouDiZhu;
				game.folder = GameConfig.DouDiZhu;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Compete;
				games.Add(game.name, game);
				game.desc = LangGame.DouDiZhu;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.MaJiang2;
				game.folder = GameConfig.MaJiang2;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Compete;
				games.Add(game.name, game);
				game.desc = LangGame.MaJiang2;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.QiangZhuangNiuniu;
				game.folder = GameConfig.QiangZhuangNiuniu;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Compete;
				games.Add(game.name, game);
				game.desc = LangGame.QiangZhuangNiuniu;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.ShiSanShui;
				game.folder = GameConfig.ShiSanShui;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Compete;
				game.show = false;
				games.Add(game.name, game);
				game.desc = LangGame.ShiSanShui;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.TongBiNiuniu;
				game.folder = GameConfig.TongBiNiuniu;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Compete;
				game.show = false;
				games.Add(game.name, game);
				game.desc = LangGame.TongBiNiuniu;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.ZhaJingHua;
				game.folder = GameConfig.ZhaJingHua;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Compete;
				game.gameID = GameConfig.GameID.ZJH;
				game.enabled = true;
				games.Add(game.name, game);
				game.desc = LangGame.ZhaJingHua;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.Bianlian2;
				game.folder = GameConfig.Bianlian2;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots;
				game.show = false;
				games.Add(game.name, game);
				game.desc = LangGame.Bianlian2;
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.CaiShenDao;
				game.folder = GameConfig.CaiShenDao;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots;
				game.show = false;
				game.desc = LangGame.CaiShenDao;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.DaHuaXiYou;
				game.folder = GameConfig.DaHuaXiYou;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots;
				game.show = false;
				game.desc = LangGame.DaHuaXiYou;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.DuoCaiDuofu;
				game.folder = GameConfig.DuoCaiDuofu;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots;
				game.show = false;
				game.desc = LangGame.DuoCaiDuofu;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.HuoYuanLianji;
				game.folder = GameConfig.HuoYuanLianji;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots;
				game.show = false;
				game.desc = LangGame.HuoYuanLianji;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.JiuXianLaWang;
				game.folder = GameConfig.JiuXianLaWang;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots | (int)GameConfig.Tag.Hot;
				game.gameID = GameConfig.GameID.JiuXianLaWang;
				game.enabled = true;
				game.desc = LangGame.JiuXianLaWang;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.XingGanNvYou;
				game.folder = GameConfig.XingGanNvYou;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots;
				game.desc = LangGame.XingGanNvYou;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.LuoMaDaMaoXian;
				game.folder = GameConfig.LuoMaDaMaoXian;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots;
				game.show = false;
				game.desc = LangGame.LuoMaDaMaoXian;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.ShuiGuoMali;
				game.folder = GameConfig.ShuiGuoMali;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots;
				game.desc = LangGame.ShuiGuoMali;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.ShuiHuZhuan;
				game.folder = GameConfig.ShuiHuZhuan;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots |(int)GameConfig.Tag.Hot;
				game.enabled = true;
				game.gameID = GameConfig.GameID.ShuiHuZhuan;
				game.desc = LangGame.ShuiHuZhuan;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.JiJieHaoBY;
				game.folder = GameConfig.JiJieHaoBY;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Fishing | (int)GameConfig.Tag.Hot;
				game.enabled = true;
				game.gameID = GameConfig.GameID.HuanleBY;
				game.desc = LangGame.JiJieHaoBY;
				games.Add(game.name, game);
			}

// 			{
// 				GameConfig game = new GameConfig();
// 				game.name = GameConfig.LingDianBY;
// 				game.folder = GameConfig.LingDianBY;
// 				game.entryClass = "Hotfix." + game.folder + ".MyApp";
// 				game.scriptType = GameConfig.ScriptType.CSharp;
// 				game.module = GameConfig.Module.FLLU3d;
// 				game.tag = (int)GameConfig.Tag.Fishing | (int)GameConfig.Tag.Hot;
// 				games.Add(game.name, game);
// 			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.ShenHaiBY;
				game.folder = GameConfig.ShenHaiBY;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Fishing | (int)GameConfig.Tag.Hot;
				game.desc = LangGame.ShenHaiBY;
				games.Add(game.name, game);
			}
			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.CaoFangBY;
				game.folder = GameConfig.CaoFangBY;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Fishing | (int)GameConfig.Tag.Hot;
				game.desc = LangGame.CaoFangBY;
				games.Add(game.name, game);
			}

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.LaBa3D;
				game.folder = GameConfig.LaBa3D;
				game.entryClass = "Hotfix." + game.folder + ".MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				game.tag = (int)GameConfig.Tag.Slots;
				game.desc = LangGame.LaBa3D;
				games.Add(game.name, game);
			}
		}

		public string defaultGameName = GameConfig.Lobby;
		public GameConfig defaultGame
		{
			get { return FindGameConfig(defaultGameName); }
		}

		public GameConfig FindGameConfig(string name)
		{
			foreach(var it in games) {
				if(it.Value.name == name) {
					return it.Value;
				}
			}
			return null;
		}

		public string GetDeviceID()
		{
			return SystemInfo.deviceUniqueIdentifier;
		}
	}
}
