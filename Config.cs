using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotfix.Common
{
	public class GameConfig
	{
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
		public const string HuanleBY = "HuanleBY";
		public const string Lobby = "Lobby";
		public string name;
		public string folder;
		public string entryClass;
		public ScriptType scriptType = ScriptType.CSharp;
		public Module module = Module.FLLU3d;
	}

	public partial class Config
	{
		public Dictionary<string, GameConfig> games = new Dictionary<string, GameConfig>();
		public Dictionary<string, int> hosts = new Dictionary<string, int>();
		public float timeOut = 5.0f;
		public void Start()
		{
			hosts.Add("47.101.62.170", 16000);

			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.HuanleBY;
				game.folder = GameConfig.HuanleBY;
				game.entryClass = "Hotfix.HuanleBY.MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.YuanSan;
				games.Add(game.name, game);
			}
			{
				GameConfig game = new GameConfig();
				game.name = GameConfig.Lobby;
				game.folder = GameConfig.Lobby;
				game.entryClass = "Hotfix.Lobby.MyApp";
				game.scriptType = GameConfig.ScriptType.CSharp;
				game.module = GameConfig.Module.FLLU3d;
				games.Add(game.name, game);
			}
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
			return "";
		}
	}
}
