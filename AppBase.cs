using AssemblyCommon;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Hotfix.Common
{
	//每个小游戏都有一个MyApp,
	public abstract class AppBase : ResourceMonitor
	{
		GameControllerBase game_ = null;
		//每个小游戏都有一个GameController
		public GameControllerBase game {
			set { 
				AddChild(value);
				game_ = value;
			}
			get { return game_; }
		}
	}
}
