using AssemblyCommon;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Hotfix.Common
{
	public static class Http
	{
		public static IEnumerator GetRequest(string uri)
		{
			string ret = "Failed";
			using (UnityWebRequest webRequest = UnityWebRequest.Get(uri)) {
				yield return webRequest.SendWebRequest();

				string[] pages = uri.Split('/');
				int page = pages.Length - 1;

				switch (webRequest.result) {
					case UnityWebRequest.Result.ConnectionError:
					case UnityWebRequest.Result.DataProcessingError:
					break;
					case UnityWebRequest.Result.ProtocolError:
					break;
					case UnityWebRequest.Result.Success:
					ret = webRequest.downloadHandler.text;
					break;
				}
			}
			yield return ret;
		}
	}
	public class LongPressData
	{
		public float startTime, duration;
		public System.Action callback;
		public bool triggered = false;
		public bool IsTimeout()
		{
			return Time.time - startTime >= duration;
		}
		public void Trigger()
		{
			callback();
			triggered = true;
		}
	}
	public static class extensions
	{
		public static void SetKeyVal<TKey, TVal>(this Dictionary<TKey, TVal> dic, TKey key, TVal val)
		{
			if (dic.ContainsKey(key)) {
				dic[key] = val;
			}
			else {
				dic.Add(key, val);
			}
		}

		public static TVal GetVal<TKey, TVal>(this Dictionary<TKey, TVal> dic, TKey key)
		{
			if (!dic.ContainsKey(key)) {
				dic.Add(key, default(TVal));
			}
			return dic[key];
		}

		public static void RemovePhysic(this GameObject obj)
		{
			var rgd = obj.GetComponent<Rigidbody2D>();
			GameObject.Destroy(rgd);
			var collider = obj.GetComponent<Collider2D>();
			GameObject.Destroy(collider);
		}

		public static void DoPopup(this GameObject obj)
		{
			obj.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 1.0f), 0.2f);
		}

		public static string ShowAsGold(this long num)
		{
			string s = num.ToString();
			return ShowAsGold(s);
		}

		public static string ShowAsGold(this string s)
		{
			string ret = "";
			for (int i = 0; i < s.Length; i++) {
				if(i % 4 == 0 && i > 0) {
					ret = ret.Insert(0, ",");
				}
				ret = ret.Insert(0, new string(s[s.Length - i - 1], 1));
			}
			return ret;
		}

		public static void LongPress(this GameObject obj, System.Action act, float duration = 3.0f)
		{
			var trigger = obj.AddComponent<EventTrigger>();
			var watcher = obj.AddComponent<OnDestryWatcher>();
			watcher.toDo = () => {
				App.ins.longPress.Remove(obj);
			};

			{
				EventTrigger.Entry enter = new EventTrigger.Entry();
				enter.eventID = EventTriggerType.PointerDown;
				enter.callback.AddListener((evt) => {
					LongPressData data = new LongPressData();
					data.callback = act;
					data.duration = duration;
					data.startTime = Time.time;
					App.ins.longPress.Add(obj, data);
				});
				trigger.triggers.Add(enter);
			}

			{
				EventTrigger.Entry enter = new EventTrigger.Entry();
				enter.eventID = EventTriggerType.PointerExit;
				enter.callback.AddListener((evt) => {
					App.ins.longPress.Remove(obj);
				});
				trigger.triggers.Add(enter);
			}

			{
				EventTrigger.Entry enter = new EventTrigger.Entry();
				enter.eventID = EventTriggerType.PointerUp;
				enter.callback.AddListener((evt) => {
					LongPressData lp;
					App.ins.longPress.TryGetValue(obj, out lp);
					if(lp != null && lp.triggered) {
						evt.Use();
					}
					App.ins.longPress.Remove(obj);
				});
				trigger.triggers.Add(enter);
			}
		}
	}

	public static class Utils
	{
		
	}
}
