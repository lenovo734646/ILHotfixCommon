using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

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

	public static class Utils
	{
		public static string FormatGoldShow(long num)
		{
			return num.ToString();
		}
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
	}
}
