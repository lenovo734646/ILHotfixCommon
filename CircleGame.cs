using AssemblyCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Hotfix.Common.CircleGame
{
	public class CirleRollGameConfigBase
	{
		public int circles = 5;
		public int initPos = 0;
		public float speedDelta = 0.1f;
		public float initSpeed = 1.5f;
		public float minSpeed = 0.05f;
		//不使用加减速
		public float uniformSpeed = -1.0f;
	}

	public abstract class CirleRollItemBase : ControllerBase
	{
		public CirleRollItemBase(int d, int i)
		{
			data = d;
			index = i;
		}

		public abstract void Show();
		public abstract void Hide();
		public abstract void ShowWin();
		public int data;
		public int index;
	}

	public abstract class CircleRollGameBase : ControllerBase
	{
		public CircleRollGameBase(CirleRollGameConfigBase conf)
		{
			conf_ = conf;
			current = conf.initPos;
		}
		
		public void SetStartFrom(CirleRollItemBase itm)
		{
			startFromItem_ = itm;
		}

		public abstract List<CirleRollItemBase> CreateItems();

		public IEnumerator StartRoll(int toIndex, float timeScale)
		{
			List<CirleRollItemBase> items = CreateItems();
			if (startFromItem_ != null) {
				current = items.FindIndex(0, items.Count, (t) => { return t == startFromItem_; });
			}
			List<int> one = new List<int>();
			for (int i = 0; i < items.Count; i++) {
				one.Add(i);
			}

			List<int> rollIndex = new List<int>();
			for(int i = current; i < items.Count; i++) {
				rollIndex.Add(i);
			}

			for (int i = 0; i < conf_.circles; i++) {
				rollIndex.AddRange(one);
			}

			for (int i = 0; i <= toIndex; i++) {
				rollIndex.Add(i);
			}

			float wait = conf_.initSpeed;
			if (conf_.uniformSpeed > 0.0f) wait = conf_.uniformSpeed;

			int played = 0;
			int slowDownPos = (int)(conf_.initSpeed / conf_.speedDelta) + 2;
			foreach (var index in rollIndex) {
				if(items[index] == null) {
					MyDebug.LogErrorFormat("items[index] == null,{0}", index);
				}
				items[index].Show();

				if(index > 0)
					items[index - 1].Hide();
				else {
					items[items.Count - 1].Hide();
				}

				yield return new WaitForSeconds(wait * timeScale);
				if(conf_.uniformSpeed < 0.0f) {
					if (played >= rollIndex.Count - slowDownPos) {
						wait += conf_.speedDelta;
						if (wait > conf_.initSpeed) wait = conf_.initSpeed;
					}
					else {
						wait -= conf_.speedDelta;
						if (wait < conf_.minSpeed) wait = conf_.minSpeed;
					}
				}
				played++;
			}
			current = toIndex;
			items[current].ShowWin();
		}
		protected CirleRollItemBase startFromItem_;
		protected CirleRollGameConfigBase conf_;
		protected int current = 0;
	}
}
