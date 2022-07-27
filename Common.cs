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
		public const string Failed = "Falied";
		public const string ServiceAvailableCode = "6E6C51D9-6B7D-4373-875F-8188FCF1024B";
		public static string lastError;
		public static IEnumerator GetRequest(string uri)
		{
			string ret = Failed;
			yield return GetUseableWebService();
			if (usingWebHost_.Value < 0) {
				MyDebug.LogWarningFormat("http request Failed usingWebHost_.Value < 0");
				yield return ret;
				yield break;
			}
			var req = GetRequest(usingWebHost_.Key, usingWebHost_.Value, "koko-manage2/third/" + uri);
			yield return req;
			yield return req.Current;
		}

		static KeyValuePair<string, int> usingWebHost_ = new KeyValuePair<string, int>("", -1);

		static IEnumerator GetUseableWebService()
		{
			List<IEnumerator> lst = new List<IEnumerator>();
			List<int> ids = new List<int>();
			List<KeyValuePair<string, int>> lHosts = App.ins.conf.webRoots.ToArray();

			//同时访问网站
			foreach (var i in App.ins.conf.webRoots) {
				var handle = GetRequest(i.Key, i.Value, "koko-manage2/third/checkservice.htm");
				ids.Add(lst.StartCor(handle, false));
				lst.Add(handle);
			}

			//找最快回复的
			bool finded = false;
			TimeCounter tc = new TimeCounter("");
			while (!finded && tc.Elapse() < App.ins.conf.networkTimeout) { 
				for (int i = 0; i < ids.Count; i++) {
					if (!Globals.cor.isRuning(ids[i])) {
						if ((string)lst[i].Current == ServiceAvailableCode) {
							finded = true;
							usingWebHost_ = lHosts[i];
							break;
						}
					}
				}
				if(!finded) yield return new WaitForSeconds(0.1f);
			}
		}


		static IEnumerator GetRequest(string host, int port, string uri)
		{
			string ret = Failed;
			string url = string.Format("http://{0}:{1}/{2}", host, port, uri);
			MyDebug.LogWarningFormat("http request:{0}", url);
			using (UnityWebRequest webRequest = UnityWebRequest.Get(url)) {
				yield return webRequest.SendWebRequest();

				string[] pages = uri.Split('/');
				int page = pages.Length - 1;
				
				lastError = webRequest.error;

				switch (webRequest.result) {
					case UnityWebRequest.Result.ConnectionError:
					case UnityWebRequest.Result.DataProcessingError:
					break;
					case UnityWebRequest.Result.ProtocolError:
					break;
					case UnityWebRequest.Result.Success: {
						var contentType = webRequest.GetResponseHeader("content-type").ToLower();
						if (contentType.Contains("application/json") || contentType.Contains("text/plain")) {
							ret = Encoding.UTF8.GetString(webRequest.downloadHandler.data);
						}
					}

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

		public static long ToGold(this string sNum)
		{
			string ret = sNum.Replace(",", "");
			return long.Parse(ret);
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

	public class Waitor<T>
	{
		public void Complete(T val)
		{
			result_ = val;
			resultSetted = true;
		}

		public IEnumerator WaitResult()
		{
			while(!resultSetted) {
				yield return 0;
			}
			yield return 1;
		}
		
		public T result
		{
			get { return result_; }
		}

		T result_;
		bool resultSetted = false;
	}

	public static class Utils
	{
		
	}
}
