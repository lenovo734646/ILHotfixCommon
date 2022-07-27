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
	}

	public abstract class CirleRollItemBase
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

	public abstract class CircleRollGameBase
	{
		public CircleRollGameBase(CirleRollGameConfigBase conf)
		{
			conf_ = conf;
			current = conf.initPos;
		}
		public abstract void CreateItems();
		public abstract CirleRollItemBase CreateRollItem(int data, int index);
		
		public IEnumerator StartRoll(int toIndex)
		{
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

				yield return new WaitForSeconds(wait);
				
				if(played >= rollIndex.Count - slowDownPos) {
					wait += conf_.speedDelta;
					if (wait > conf_.initSpeed) wait = conf_.initSpeed;
				}
				else {
					wait -= conf_.speedDelta;
					if (wait < conf_.minSpeed) wait = conf_.minSpeed;
				}
				played++;
			}
			current = toIndex;
			items[current].ShowWin();
		}

		protected CirleRollGameConfigBase conf_;
		protected List<CirleRollItemBase> items = new List<CirleRollItemBase>();
		protected int current = 0;
	}
}
