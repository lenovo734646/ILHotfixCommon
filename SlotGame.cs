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
	public enum RollState
	{
		Stopped,
		WaitingResult,
		Rolling,
	}

	public abstract class RollingConfigBase
	{
		public float rollTime = 2.0f, instanceShowPercent = 0.95f;
		public int simPages = 10, rowCount = 3, colCount = 5;
		public Vector2 cellSize;
		public float initY = -214, space = 0;
		public float colDelay = 0.3f;
		public int defaultI = 0;
		public bool addtionalRow = false, stopRollSoundEff = true;
	}

	public class SlotGameResult
	{
		//主要中奖图标
		public int mainPid;
		////有额外翻牌的机会次数. 小马丽的机会次数,免费游戏次数
		public int result;
		//主要中奖图标对应的数据
		public int resultData;
		public long win;
		public int maxRate;
		public List<int> icons = new List<int>();
		public List<KeyValuePair<int, int>> winIcons = new List<KeyValuePair<int, int>>();
		public List<KeyValuePair<int, int>> iconXY = new List<KeyValuePair<int, int>>();
		public List<int> winLines = new List<int>();
	}

	public abstract class SlotRollItemBase
	{
		public enum State
		{
			Normal,
			Rolling,
			Gray,
			Win,
		}

		public SlotRollItemBase(GameObject obj, int data, int idx)
		{
			obj_ = obj;
			data_ = data;
			index = idx;
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

		public void RestoreState()
		{ 
			if(resultSt_ == State.Win) {
				SetState(State.Normal);
			}
		}

		protected abstract void OnInit();
		public abstract void SetState(State st);

		public int index;

		protected GameObject obj_;
		protected State st_, resultSt_;
		protected GameObject gray, normal, rolling, animation;
		protected int data_;
		
	}

	public abstract class SlotRollGameBase : ControllerBase
	{
		public SlotRollGameBase(List<GameObject> cols, RollingConfigBase conf)
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
			SetRollItems(RandomAPage(null), SlotRollItemBase.State.Normal, false);
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
		protected abstract SlotRollItemBase CreateRollItem(int data, int index);
		protected abstract IEnumerator PlayStartEffect();
		protected abstract void PlayCompleteEffect();

		public List<SlotRollItemBase> SetRollItems(List<int> items, SlotRollItemBase.State st, bool isResult)
		{
			List<SlotRollItemBase> ret = new List<SlotRollItemBase>();
			for (int i = items.Count - 1; i >= 0; i--) {
				int col = i % conf_.colCount;
				SlotRollItemBase itm = CreateRollItem(items[i], i);
				itm.Init();
				itm.SetState(st);
				if(isResult) itm.gameObject.name = "Result_" + i;
				cols_[col].AddChild(itm.gameObject);
				rollItems_.Add(itm);
				ret.Add(itm);
			}
			return ret;
		}

		public virtual IEnumerator StartRoll()
		{
			foreach(var it in rollItems_) {
				it.RestoreState();
			}

			skipType_ = eSkipTo.None;
			float offset = 0;
			List<SlotRollItemBase> sims = new List<SlotRollItemBase>(rollItems_);
			//先随机N页,显示模糊图标
			for (int i = 0; i < conf_.simPages; i++) {
				var lst = RandomAPage(result_);
				sims.AddRange(SetRollItems(lst, SlotRollItemBase.State.Rolling, false));
				offset += (conf_.cellSize.y + conf_.space) * (lst.Count / conf_.colCount);
			}

			//再显示结果页
			var ret = SetRollItems(result_, SlotRollItemBase.State.Normal, true);
			offset += (conf_.cellSize.y + conf_.space) * (ret.Count / conf_.colCount);
			//再随机1页,显示正常图显
			if (conf_.addtionalRow){
				var lst = RandomAPage(result_);
				sims.AddRange(SetRollItems(lst, SlotRollItemBase.State.Normal, false));
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

				if(skipType_ == eSkipTo.None)
					yield return new WaitForSeconds(conf_.colDelay);
			}

			TimeCounter tc = new TimeCounter("");
			while ((tc.Elapse() < (conf_.rollTime)) && skipType_ == eSkipTo.None) {
				yield return new WaitForSeconds(0.1f);
			}

			if (skipType_ != eSkipTo.SkipAll) {
				//强制动画完成
				for (int i = 0; i < conf_.colCount; i++) {
					var it = tweens[i];
					if (tc.Elapse() < conf_.rollTime * conf_.instanceShowPercent) {
						float goTime = (conf_.rollTime * conf_.instanceShowPercent - i * conf_.colDelay);
						float playedPercent = it.ElapsedPercentage();

						if ((goTime / it.Duration()) >= playedPercent) {
							it.Goto(goTime);
							it.Play();
						}
					}
				}
			}
			AudioSource aus = (AudioSource)(eff.Current);
			if (aus != null && conf_.stopRollSoundEff) {
				if (eff.Current != null) {
					aus.Stop();
				}
			}

			if (skipType_ != eSkipTo.SkipAll) {
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
			}

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

		protected override void OnStop()
		{
			foreach (var it in rollItems_) {
				it.Close();
			}
			rollItems_.Clear();
		}

		public void SkipToResult(eSkipTo show)
		{
			skipType_ = show;
		}

		public eSkipTo SkipTo
		{
			get { return skipType_; }
		}

		public List<SlotRollItemBase> ResultItems
		{
			get { return rollItems_; }
		}

		public enum eSkipTo
		{
			None,
			SkipRolling,
			SkipAll,
		}

		protected RollingConfigBase conf_;
		protected List<int> result_ = new List<int>();

		eSkipTo skipType_ = eSkipTo.None;
		List<GameObject> cols_;
		List<SlotRollItemBase> rollItems_ = new List<SlotRollItemBase>();
		
	}

	public abstract class SlotRollBase : SlotRollGameBase
	{
		public SlotRollBase(List<GameObject> cols, RollingConfigBase conf) : base(cols, conf)
		{
		}
		public Waitor<int> waitOther = null;
		public abstract int GetResultType(SlotGameResult result);
		protected abstract void PlayHitLineSoundEffect();
		protected abstract void PlayWinIconSoundEffect(int ico);
		protected virtual void OnShowResultFinished() { }
		public override IEnumerator StartRoll()
		{
			yield return base.StartRoll();

			if (waitOther != null)
				yield return waitOther.WaitResult();

			var ret = ResultItems;
			
			if(SkipTo != eSkipTo.SkipAll) {
				if (gameResult_.iconXY.Count > 0) {
					//显示中奖线条
					foreach (var it in gameResult_.winLines) {
						int line = it;
						lines_[line].SetActive(true);
						lines_[line].StartAnim();
						PlayHitLineSoundEffect();
						yield return new WaitForSeconds(0.3f);
					}
					yield return new WaitForSeconds(0.3f);
					//中奖图标播放动画
					ret.Reverse();
					foreach (var it in gameResult_.iconXY) {
						int x = it.Key, y = it.Value;
						ret[y * conf_.colCount + x].SetState(SlotRollItemBase.State.Win);
					}

					List<int> played = new List<int>();
					foreach (var it in gameResult_.winIcons) {
						if (!played.Contains(it.Key)) {
							PlayWinIconSoundEffect(it.Key);
							played.Add(it.Key);
						}
					}

					//未中奖的图标灰掉
					foreach (var it in ret) {
						if (it.state != SlotRollItemBase.State.Win) {
							it.SetState(SlotRollItemBase.State.Gray);
						}
					}
					OnShowResultFinished();

					if (gameResult_.winLines.Count > 0) {
						yield return new WaitForSeconds(1.0f);
					}
				}
				else {
					//未中奖的图标灰掉
					foreach (var it in ret) {
						it.SetState(SlotRollItemBase.State.Gray);
					}
				}
			}
		}

		public void SetGameResult(SlotGameResult r)
		{
			gameResult_ = r;
			SetResultPage(r.icons);
		}

		public void SetLines(List<GameObject> r)
		{
			lines_ = r;
		}

		protected SlotGameResult gameResult_;
		protected List<GameObject> lines_ = new List<GameObject>();
	}
}
