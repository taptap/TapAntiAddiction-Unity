﻿using System;
using System.Collections;
using System.Threading.Tasks;
using TapTap.Common;
using TapTap.Login;
using UnityEngine;
using Network = TapTap.AntiAddiction.Internal.Network;

namespace TapTap.AntiAddiction 
{
    /// <summary>
    /// 防沉迷轮询器
    /// </summary>
    internal class AntiAddictionPoll : MonoBehaviour 
    {
        static readonly string ANTI_ADDICTION_POLL_NAME = "AntiAddictionPoll";

        static AntiAddictionPoll current;

        /// <summary>
        /// 轮询间隔，单位：秒
        /// </summary>
        private static int pollInterval = 2 * 60;

        private static Coroutine _pollCoroutine;

        private static float? _elpased;

        public static bool StartPoll;

        internal static void StartUp(int inverval = 0) 
        {
            TapLogger.Debug("StartUp " );
            if(inverval > 0){
                pollInterval = inverval;
            }
            if (current == null) 
            {
                GameObject pollGo = new GameObject(ANTI_ADDICTION_POLL_NAME);
                DontDestroyOnLoad(pollGo);
                current = pollGo.AddComponent<AntiAddictionPoll>();
                _elpased = null;
            }

            if (_pollCoroutine == null)
            {
                _pollCoroutine = current.StartCoroutine(current.Poll());
                StartPoll = true;
            }
        }
        
        internal static void StartCountdownRemainTime() 
        {
            TapLogger.Debug("StartCountdownRemainTime  " );
            if (current == null) 
            {
                GameObject pollGo = new GameObject(ANTI_ADDICTION_POLL_NAME);
                DontDestroyOnLoad(pollGo);
                current = pollGo.AddComponent<AntiAddictionPoll>();
                _elpased = null;
            }
            else
            {
                return;
            }

            _elpased = 0;
        }

        internal static void Logout()
        {
            StartPoll = false;
            _elpased = null;
            current?.StopAllCoroutines();
            _pollCoroutine = null;
        }

        private void Update()
        {
            if (_elpased != null)
            {
                _elpased += Time.unscaledDeltaTime;
                if (_elpased >= 1)
                {
                    _elpased = 0;
                    if (TapTapAntiAddictionManager.CurrentRemainSeconds != null)
                        TapLogger.Debug("Poll update CurrentRemainSeconds" );
                        TapTapAntiAddictionManager.CurrentRemainSeconds--;
                }
            }
        }

        IEnumerator Poll() 
        {
            while (true) 
            {
                TapLogger.Debug("Poll  checkPlay" );
                // 上报/检查可玩
                Task<PlayableResult> checkPlayableTask = TapTapAntiAddictionManager.CheckPlayableOnPolling();
                yield return new WaitUntil(() => checkPlayableTask.IsCompleted);
                TapLogger.Debug($"{DateTime.Now:hh:mm:ss ddd} 剩余时间(秒): {checkPlayableTask.Result.RemainTime}");
                if (checkPlayableTask.Result.RemainTime <= 0)
                {
                    _elpased = null;
                    break;
                }
                if(checkPlayableTask.Result.RemainTime > 0 && checkPlayableTask.Result.RemainTime < pollInterval){
                    pollInterval = checkPlayableTask.Result.RemainTime;
                }
                if (_elpased == null)
                    _elpased = 0;
                
                yield return new WaitForSeconds(pollInterval);
            }
        }

        /// <summary>
        /// 切换后台
        /// </summary>
        /// <param name="pauseStatus"></param>
        void OnApplicationPause(bool pauseStatus)
        {
            TapLogger.Debug("Anti OnApplicationPause " + pauseStatus);
           if(pauseStatus){
                TapTapAntiAddictionManager.LeaveGame();
           }else{
                TapTapAntiAddictionManager.EnterGame();
           } 
        }


        private static void SendPlayableRequest()
        {
#pragma warning disable CS4014
            Network.CheckPlayable();
#pragma warning restore CS4014
        }
    }
}
