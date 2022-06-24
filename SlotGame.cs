using AssemblyCommon;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hotfix.Common.Slot
{
	public abstract class RollingConfigBase
	{
		public float rollTime = 2.0f, instanceShowPercent = 0.9f;
		public int simPages = 10, rowCount = 3, colCount = 5;
		public Vector2 cellSize;
		public float initY = -214;
		public float colDelay = 0.15f;
		public int defaultI = 0;
		public bool addtionalRow = false;
	}

	public class SlotGameResult
	{
		public int mainPid;
		public int result, resultData;
		public long win, maxRate;
		public List<int> icons = new List<int>();
		public List<KeyValuePair<int, int>> winIcons = new List<KeyValuePair<int, int>>();
		public List<KeyValuePair<int, int>> iconXY = new List<KeyValuePair<int, int>>();
		public List<int> winLines = new List<int>();
	}

	public abstract class RollItemBase
	{
		public enum State
		{
			Normal,
			Rolling,
			Gray,
			Win,
		}

		public RollItemBase(GameObject obj, int data)
		{
			obj_ = obj;
			data_ = data;
		}

		public void Close()
		{
			GameObject.Destroy(obj_);
		}

		public GameObject gameObject
		{
			get { return obj_; }
		}

		public State state
		{
			get { return st_; }
		}

		public void Init()
		{
			OnInit();
		}

		protected abstract void OnInit();
		public abstract void SetState(State st);

		protected GameObject obj_;
		protected State st_;
		protected GameObject gray, normal, rolling, animation;
		protected int data_;
	}

	public abstract class RollGameBase
	{
		public RollGameBase(List<GameObject> cols, RollingConfigBase conf)
		{
			conf_ = conf;
			cols_ = cols;
			foreach (var col in cols) {
				var vlg = col.GetComponent<VerticalLayoutGroup>();
				if (vlg == null) {
					vlg = col.AddComponent<VerticalLayoutGroup>();
					vlg.reverseArrangement = true;
					vlg.childForceExpandWidth = true;
					vlg.childForceExpandHeight = true;
					vlg.childControlWidth = false;
					vlg.childControlHeight = false;
					vlg.childAlignment = TextAnchor.UpperLeft;
				}

				var csf = col.GetComponent<ContentSizeFitter>();
				if (csf == null) {
					csf = col.AddComponent<ContentSizeFitter>();
					csf.verticalFit = ContentSizeFitter.FitMode.MinSize;
				}
			}
		}

		public void ShowInitPage()
		{
			SetRollItems(RandomAPage(null), RollItemBase.State.Normal);
		}

		public bool SetResultPage(List<int> result)
		{
			bool bSame = result_.Count == result.Count;
			if (bSame) {
				for(int i = 0; i < result_.Count; i++) {
					if (result_[i] != result[i]) {
						bSame = false;
						break;
					}
				}
			}
			result_ = new List<int>(result);
			return !bSame;
		}

		protected abstract List<int> RandomAPage(List<int> to);
		protected abstract RollItemBase CreateRollItem(int data);
		protected abstract IEnumerator PlayStartEffect();
		protected abstract void PlayCompleteEffect();

		protected List<RollItemBase> SetRollItems(List<int> items, RollItemBase.State st)
		{
			List<RollItemBase> ret = new List<RollItemBase>();
			for (int i = items.Count - 1; i >= 0; i--) {
				int col = i % conf_.colCount;
				RollItemBase itm = CreateRollItem(items[i]);
				itm.Init();
				itm.SetState(st);
				cols_[col].AddChild(itm.gameObject);
				rollItems_.Add(itm);
				ret.Add(itm);
			}
			return ret;
		}

		public virtual IEnumerator StartRoll()
		{
			TimeCounter tc1 = new TimeCounter("");
			instanceShowResult_ = false;
			float offset = 0;
			List<RollItemBase> sims = new List<RollItemBase>(rollItems_);
			//先随机N页,显示模糊图标
			for (int i = 0; i < conf_.simPages; i++) {
				var lst = RandomAPage(result_);
				sims.AddRange(SetRollItems(lst, RollItemBase.State.Rolling));
				offset += conf_.cellSize.y * (lst.Count / conf_.colCount);
			}

			//再显示结果页
			var ret = SetRollItems(result_, RollItemBase.State.Normal);
			offset += conf_.cellSize.y * (ret.Count / conf_.colCount);
			//再随机1页,显示正常图显
			if (conf_.addtionalRow){
				var lst = RandomAPage(result_);
				sims.AddRange(SetRollItems(lst, RollItemBase.State.Normal));
			}

			var eff = PlayStartEffect();
			yield return eff;

			List<TweenerCore<Vector3, Vector3, VectorOptions>> tweens = new List<TweenerCore<Vector3, Vector3, VectorOptions>>();
			for (int i = 0; i < conf_.colCount; i++) {
				var it = cols_[i];
				var trans = cols_[i].GetComponent<RectTransform>();
				var tween = trans.DOLocalMove(new Vector3(0, -offset, 0), conf_.rollTime);
				tween.SetEase(Ease.InOutSine);
				tween.SetRelative(true);
				tween.SetAutoKill(false);
				tweens.Add(tween);

				tween.onComplete = () => {
					PlayCompleteEffect();
				};

				yield return new WaitForSeconds(conf_.colDelay);
			}

			MyDebug.LogFormat("roll init time cost:{0}", tc1.Elapse());
			TimeCounter tc = new TimeCounter("");
			while ((tc.Elapse() < (conf_.rollTime)) && !instanceShowResult_) {
				yield return new WaitForSeconds(0.1f);

			}

			//强制动画完成
			for (int i = 0; i < conf_.colCount; i++) {
				var it = tweens[i];
				if (tc.Elapse() < conf_.rollTime * conf_.instanceShowPercent) {
					float goTime = conf_.rollTime * conf_.instanceShowPercent - i * conf_.colDelay;
					it.Goto(goTime);
					it.Play();
				}
			}

			AudioSource aus = (AudioSource)(eff.Current);
			if (aus != null) {
				if (eff.Current != null) {
					aus.Stop();
				}
			}

			//等DoTween完成, 为什么不直接WaitForCompletion?因为这个会导致协程Stop失败.
			bool allComplete = true;
			do {
				allComplete = true;
				for (int i = 0; i < conf_.colCount; i++) {
					var it = tweens[i];
					bool completed = it.IsComplete();
					allComplete &= completed;
				}
				if (!allComplete) {
					yield return 0;
				}
			} while (!allComplete);

			for (int i = 0; i < conf_.colCount; i++) {
				var it = tweens[i];
				it.Kill();
			}

			//删除之前模拟图标
			foreach (var it in sims) {
				it.Close();
			}

			//保存当前结果图标
			rollItems_.Clear();
			rollItems_.AddRange(ret);

			//位置恢复.
			for (int i = 0; i < conf_.colCount; i++) {
				var trans = cols_[i].GetComponent<RectTransform>();
				trans.offsetMin = new Vector2(trans.offsetMin.x, conf_.initY);
				trans.offsetMax = new Vector2(trans.offsetMax.x, conf_.initY + trans.sizeDelta.y);
			}
		}

		public virtual void Close()
		{
			foreach (var it in rollItems_) {
				it.Close();
			}
			rollItems_.Clear();
		}

		public void ShowResult()
		{
			instanceShowResult_ = true;
		}

		public List<RollItemBase> ResultItems
		{
			get { return rollItems_; }
		}

		protected RollingConfigBase conf_;
		protected List<int> result_ = new List<int>();

		bool instanceShowResult_ = false;
		List<GameObject> cols_;
		List<RollItemBase> rollItems_ = new List<RollItemBase>();
		
	}

	public abstract class SlotRollBase : RollGameBase
	{
		public SlotRollBase(List<GameObject> cols, RollingConfigBase conf) : base(cols, conf)
		{
		}

		public abstract int GetResultType(SlotGameResult result);
		
		public override IEnumerator StartRoll()
		{
			yield return base.StartRoll();

			var ret = ResultItems;

			if (gameResult_.iconXY.Count > 0) {
				//中奖图标播放动画
				ret.Reverse();
				foreach (var it in gameResult_.iconXY) {
					int x = it.Key, y = it.Value;
					ret[y * conf_.colCount + x].SetState(RollItemBase.State.Win);
				}
				//未中奖的图标灰掉
				foreach (var it in ret) {
					if (it.state != RollItemBase.State.Win) {
						it.SetState(RollItemBase.State.Gray);
					}
				}

				//显示中奖线条
				foreach (var it in gameResult_.winLines) {
					int line = it;
					lines_[line].SetActive(true);
					lines_[line].StartAnim();
				}

			}
			else {
				//未中奖的图标灰掉
				foreach (var it in ret) {
					it.SetState(RollItemBase.State.Gray);
				}
			}
		}
		public void SetGameResult(SlotGameResult r)
		{
			gameResult_ = r;
		}
		public void SetLines(List<GameObject> r)
		{
			lines_ = r;
		}

		protected SlotGameResult gameResult_;
		protected List<GameObject> lines_ = new List<GameObject>();
	}
}
