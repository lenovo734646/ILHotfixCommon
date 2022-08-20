using AssemblyCommon;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Hotfix.Common
{
	//每个小游戏都有一个MyApp,
	public class AppBase : ResourceMonitor
	{
		//每个小游戏都有一个GameController
		public GameControllerBase game = null;
		public virtual IEnumerator CoStart()
		{
			yield return game?.CoStart();
		}

		public override void Stop()
		{
			game?.Stop();
			base.Stop();
		}

		public override void Update()
		{
			game?.Update();
		}

		//本游戏即将退出,请清理需要清理的协程,事件回调之类,避免在游戏退出过程中报错.
		public virtual void AboutToStop()
		{
			
		}
	}
}
