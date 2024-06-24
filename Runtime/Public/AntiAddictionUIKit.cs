﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CheckPayResult = Plugins.AntiAddictionUIKit.CheckPayResult;
using TapTap.AntiAddiction.Model;
using TapTap.Common;
using TapTap.Login;
using System.Runtime.CompilerServices;

namespace TapTap.AntiAddiction
{
    public enum Region
    {
        // NOTE:不要随便改枚举的int值,已经被序列化保存到本地过.
        China = 1
    }
    
    public static class AntiAddictionUIKit
    {
        
        private static IAntiAddictionJob _job;

        private static IAntiAddictionJob Job
        {
            get
            {
                if (_job == null)
                {
                    InitJob();
                }
                return _job;
            }
        }
        
        private static bool _isInit = false;

        private static Region _region = Region.China;
        
        public static Action<int, string> ExternalCallback
        {
            get => Job?.ExternalCallback;
        }
        
        private static IAntiAddictionJob CreateJob(bool isNewJob)
        {
            if (isNewJob)
            {
                var result = Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblies()
                    .Where(asssembly => asssembly.GetName().FullName.StartsWith("TapTap.AntiAddiction"))
                    .SelectMany(assembly => assembly.GetTypes())
                    .SingleOrDefault((clazz) => typeof(IAntiAddictionJob).IsAssignableFrom(clazz) && clazz.IsClass 
                    && clazz.Name.Contains("AntiAddictionNewJob")));
                return result as IAntiAddictionJob;
            }
            else
            {
                var result = Activator.CreateInstance(AppDomain.CurrentDomain.GetAssemblies()
                    .Where(asssembly => asssembly.GetName().FullName.StartsWith("TapTap.AntiAddiction.Mobile.Runtime"))
                    .SelectMany(assembly => assembly.GetTypes())
                    .SingleOrDefault((clazz) => typeof(IAntiAddictionJob).IsAssignableFrom(clazz) && clazz.IsClass 
                        && clazz.Name.Contains("AntiAddictionMobileOldJob")));
                return result as IAntiAddictionJob;
            }
        }

        private static void InitJob()
        {
            // 国内-移动端防沉迷用桥接的方式
            if (Region.China == _region && (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer))
            {
                _job = CreateJob(false);
            }
            // 其他均使用 Unity Native 的方式
            else
            {
                _job = CreateJob(true);
            }
            
            TapLogger.Debug(string.Format("Anti Addiction Job Type: {0} ! Region: {1} Platform: {2}", _job.GetType(), _region.ToString(), Application.platform.ToString()));
        }

        public static void SetRegion(Region region)
        {
            if (region != _region || _job == null)
            {
                _region = region;
                InitJob();

            }

            var jobType = _job.GetType();
            var setRegionMI = jobType.GetMethod("SetRegion", BindingFlags.Public);
            setRegionMI?.Invoke(_job, new object[] {region});
        }
        
        public static void Init(AntiAddictionConfig config)
        {
            SetRegion(config.region);
            Job.Init(config);
            _isInit = true;
        }


        public static void Init(AntiAddictionConfig config, Action<int, string> callback) {
            Init(config);
            _isInit = true;
            SetAntiAddictionCallback(callback);
        }

        public static void SetAntiAddictionCallback(Action<int, string> callback)
        {
            if (_isInit == false) {
                TapLogger.Warn("TapSDK::AntiAddictionUIKit is not init, please call Init first!");
            }
            Job.SetAntiAddictionCallback(callback);
        }
        
        public static void StartupWithTapTap(string userId)
        {
            Job.StartupWithTapTap(userId);
        }

        public static void Startup(string userId, bool isTapUser = false)
        {
            Job.Startup(userId, isTapUser);
        }

        public static void Exit()
        {
            Job.Exit();
        }

        [Obsolete]
        public static void EnterGame()
        {
            Job.EnterGame();
        }

        [Obsolete]
        public static void LeaveGame()
        {
            Job.LeaveGame();
        }
        
        /// <summary>
        /// 年龄类型:UNREALNAME = -1;CHILD = 0;TEEN = 8;YOUNG = 16; ADULT = 18;
        /// 当游戏旧版本不使用年龄段，新版本使用年龄段，对于老用户仍返回 -1
        /// </summary>
        public static int AgeRange
        {
            get
            {
                if (Job != null)
                    return Job.AgeRange;
                return -1;
            }
        }

        public static int RemainingTimeInMinutes
        {
            get
            {
                if (Job != null)
                    return Job.RemainingTimeInMinutes;
                return 0;
            }
        }

        /// <summary>
        /// 剩余时间(单位:秒)
        /// </summary>
        public static int RemainingTime
        {
            get
            {
                if (Job != null)
                    return Job.RemainingTime;
                return 0;
            }
        }

        public static string CurrentToken
        {
            get
            {
                if (Job != null)
                    return Job.CurrentToken;
                return "";
            }
        }
        
        /// <summary>
        /// 在支付前,检查支付结果
        /// </summary>
        /// <param name="amount">支付金额,单位:分</param>
        /// <param name="handleCheckPayLimit">检查支付结果的回调</param>
        /// <param name="handleCheckPayLimitException">检查支付碰到问题时的回调</param>
        public static void CheckPayLimit(long amount
            , Action<CheckPayResult> handleCheckPayLimit
            , Action<string> handleCheckPayLimitException)
        {
            Job.CheckPayLimit(amount, handleCheckPayLimit, handleCheckPayLimitException);
        }
        
        /// <summary>
        /// 提交支付结果
        /// </summary>
        /// <param name="amount">支付金额,单位:分</param>
        /// <param name="handleSubmitPayResult">提交成功后的回调</param>
        /// <param name="handleSubmitPayResultException">提交失败后的回调</param>
        public static void SubmitPayResult(long amount
            , Action handleSubmitPayResult
            , Action<string> handleSubmitPayResultException
        )
        {
            Job.SubmitPayResult(amount, handleSubmitPayResult, handleSubmitPayResultException);
        }
        

        /// <summary>
        /// 设置测试环境，需要在 startup 接口调用前设置
        /// </summary>
        /// <param name="enable">测试环境是否可用</param>
        public static void SetTestEnvironment(bool enable) {
            if (Job != null)
                Job.SetTestEnvironment(enable);
        }

        public static void OnInvokeExternalCallback(int code, string msg){
            if (Job != null){
                Job.OnInvokeExternalCallback(code,msg);
            }
        }
        
    }
}