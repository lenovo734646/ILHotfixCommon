﻿using AssemblyCommon;
using AssemblyCommon.Bridges;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Hotfix.Common
{
	public class AudioManager:ResourceMonitor
	{
		protected override IEnumerator OnStart()
		{
			//加入音乐播放
			audioMusic = BridgeToHotfix.ins.gameObject.AddComponent<AudioSource>();

			var ust = Globals.GetFromFile<UserSettingSaved>("userSetting.txt");
			enableMusic = ust.audioMusicEnabled;
			enableEffect = ust.audioEffectEnabled;

			yield return 0;
		}

		//停止所有声音
		public void StopAll()
		{
			audioMusic.Stop();

			foreach (var eff in audioEffectPool) {
				eff.Stop();
			}
			
			ClearResource();
		}

		//停止这个模块
		protected override void OnStop()
		{
			StopAll();
		}

		public void PlayMusicOneShot(string path)
		{
			this.StartCor(PlayMusic(path), false);
		}

		public void PlayEffOneShot(string path, float delay = 0.0f)
		{
			this.StartCor(PlayEffect(path, delay), false);
		}

		public IEnumerator PlayEffect(string path, float delay = 0.0f)
		{
			if (!enableEffect_) yield break;
			if (delay > 0.0f) yield return new WaitForSeconds(delay);
			var handle = DoPlayAudioClip_(path, true);
			yield return handle;
			yield return handle.Current;
		}

		public IEnumerator PlayMusic(string path)
		{
			if (!enableEffect_) yield break;
			var handle = DoPlayAudioClip_(path, false);
			yield return handle;
			yield return handle.Current;
		}

		public bool enableMusic
		{
			get { return enableMusic_; }
			set {
				enableMusic_ = value;
				OnMusicEnableChanged_();
			}
		}

		public bool enableEffect
		{
			get { return enableEffect_; }
			set {
				enableEffect_ = value;
				if (enableEffect_) {
					OnEffectEnableChanged_();
				}
			}
		}

		IEnumerator DoPlayAudioClip_(string path, bool isEffect)
		{
			AddressablesLoader.LoadTask<AudioClip> clip = null;
			foreach (var tsk in resourceLoader_) {
				if(tsk.path == path && tsk.status != AsyncOperationStatus.None) {
					clip = (AddressablesLoader.LoadTask<AudioClip>)tsk;
					break;
				}
			}

			AudioSource used = null;
			if (clip == null) {
				LoadAssets<AudioClip>(path, (t) => {
					if (t.status == AsyncOperationStatus.Succeeded && t != null) {
						if (isEffect) {
							used = GetEffectSource_();
							used.clip = t.Result;
							used.Play();
						}
						else {
							audioMusic.clip = t.Result;
							audioMusic.loop = true;
							audioMusic.Play();
							used = audioMusic;
						}
					}
				});
				yield return WaitingForReady();
			}
			else {
				if (isEffect) {
					used = GetEffectSource_();
					used.clip = clip.Result;
					used.Play();
				}
				else {
					audioMusic.clip = clip.Result;
					audioMusic.loop = true;
					audioMusic.Play();
					used = audioMusic;
				}
			}
			yield return used;
		}

		AudioSource GetEffectSource_()
		{
			foreach (var eff in audioEffectPool) {
				if (!eff.isPlaying) {
					return eff;
				}
			}
			var aus = BridgeToHotfix.ins.gameObject.AddComponent<AudioSource>();
			audioEffectPool.Add(aus);
			return aus;
		}


		void OnEffectEnableChanged_()
		{
			if (!enableEffect_) {
				foreach (var eff in audioEffectPool) {
					eff.Stop();
				}
			}else if (enableEffect_)
            {
				foreach (var eff in audioEffectPool)
				{
					eff.Play();
				}
			}
		}

		void OnMusicEnableChanged_()
		{
			if (enableMusic_) {
				audioMusic.Play();
			}
			else {
				audioMusic.Stop();
			}
		}

		public override string GetDebugInfo()
		{
			return "AudioManager";
		}

		bool enableEffect_ = true, enableMusic_ = true;
		AudioSource audioMusic;
		List<AudioSource> audioEffectPool = new List<AudioSource>();
	}
}
