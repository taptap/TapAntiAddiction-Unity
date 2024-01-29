﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using LC.Newtonsoft.Json;
using TapTap.AntiAddiction.Internal.Http;
using TapTap.AntiAddiction.Model;
using TapTap.Common;
using TapTap.Login;
using UnityEngine;

namespace TapTap.AntiAddiction.Internal
{
    internal static class Network {
        static readonly string ChinaHost = "https://tds-tapsdk.cn.tapapis.com";
        static readonly string VietnamHost = "https://tds-account.intl.tapapis.com";

        private static AntiAddictionHttpClient
            HttpClient = new AntiAddictionHttpClient(ChinaHost);

        private static string gameId;
        
        private static bool enableTestMode;

        internal static void SetGameId(string gameId)
        {
            Network.gameId = gameId;
            HttpClient.ChangeAddtionalHeader("X-LC-Id", gameId);
        }

        internal static void ChangeRegion(Region region)
        {
            HttpClient.ChangeAddtionalHeader("Accept-Language",
                (region == Region.Vietnam ? "vi-VN" : "zh-CN"));
        }
        
        internal static void ChangeHost()
        {
            string host;
            switch (TapTapAntiAddictionManager.AntiAddictionConfig.region)
            {
                case Region.Vietnam:
                    host = VietnamHost;
                    break;
                case Region.China:
                default:
                    host = ChinaHost;
                    break;
            }
            
            if (HttpClient != null)
            {
                Type httpClientType = typeof(AntiAddictionHttpClient);
                var hostFieldInfo = httpClientType.GetField("serverUrl", BindingFlags.NonPublic | BindingFlags.Instance);
                hostFieldInfo?.SetValue(HttpClient, host);
            }
        }
        
        /// <summary>
        /// 拉取配置并缓存在内存
        /// 没有持久化的原因是无法判断 SDK 自带与本地持久化版本的高低
        /// </summary>
        /// <returns></returns>
        internal static async Task<AntiAddictionConfigResult> FetchConfig() {
            string path = $"anti-addiction/v1/{TapTapAntiAddictionManager.AntiAddictionConfig.regionStr}/clients/{gameId}/configuration";
            AntiAddictionConfigResponse response = await HttpClient.Get<AntiAddictionConfigResponse>(path);
            return response.Result;
        }

        private static string FixForTestMode(string path) {
            var splitCharIndex = path.LastIndexOf('/');
            if (splitCharIndex >= 0)
                path = path.Insert(splitCharIndex, "/fake");
            return path;
        }

        /// <summary>
        /// 拉取实名认证数据
        /// </summary>
        /// <returns></returns>
        internal static async Task<VerificationResult> FetchVerification(string userId) 
        {
            string path = $"real-name/v1/{TapTapAntiAddictionManager.AntiAddictionConfig.regionStr}/clients/{gameId}/users/{userId}";
            ServerVerificationResponse response = await HttpClient.Get<ServerVerificationResponse>(path);
            return response.Result;
        }
        
        /// <summary>
        /// 通过 code 换取实名信息
        /// </summary>
        /// <returns></returns>
        internal static async Task<VerificationResult> FetchVerificationByCode(string userId, string code, bool isTapLogin) {
            var tcs = new TaskCompletionSource<VerificationResult>();
            var path = $"real-name/v1/{TapTapAntiAddictionManager.AntiAddictionConfig.regionStr}/clients/{gameId}/users/{userId}/taptap";
            var param = new Dictionary<string, object> {
                ["code"] = code,
                ["from_taptap"] = isTapLogin ? "1" : "0"
            };
            
            var response = await HttpClient.Post<ServerVerificationResponse>(path, queryParams: param);
            tcs.TrySetResult(response.Result);
            return await tcs.Task;
        }
        
        /// <summary>
        /// 拉取实名认证数据
        /// </summary>
        /// <returns></returns>
        internal static async Task<VerificationResult> QuickFetchVerification(string userId, AccessToken token, string key) {
            var tcs = new TaskCompletionSource<VerificationResult>();
            var json = JsonConvert.SerializeObject(token, Formatting.Indented, new AccessTokenJsonConverter(typeof(AccessToken)));
            string encryptStr = Tool.RsaEncrypt(json, key);
            if (string.IsNullOrEmpty(encryptStr))
            {
                tcs.TrySetException(new Exception("RSA Encrypt Failed"));
            }
            else
            {
                string path = $"real-name/v1/{TapTapAntiAddictionManager.AntiAddictionConfig.regionStr}/clients/{gameId}/users/{userId}/taptap-smooth";
                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    ["data"] = encryptStr
                };
                ServerVerificationResponse response = await HttpClient.Post<ServerVerificationResponse>(path, data: data);
                tcs.TrySetResult(response.Result);
            }
            return await tcs.Task;
        }
        
        /// <summary>
        /// 获取服务器时间
        /// </summary>
        /// <returns></returns>
        internal static async Task<long> FetchServerTime() 
        {
            string path = $"anti-addiction/v1/server-time";
            ServerTimeResponse response = await HttpClient.Get<ServerTimeResponse>(path);
            return response.Result.Timestamp;
        }
        
        /// <summary>
        /// 检测身份信息是否通过
        /// </summary>
        /// <param name="json">包括了身份信息,每个国家的身份信息会不同</param>
        /// <param name="key"></param>
        /// <param name="activelyManualVerify">主动触发手动认证</param>
        /// <returns></returns>
        internal static async Task<VerificationResult> FetchVerificationManual(string json, string key, bool activelyManualVerify)
        {
            var tcs = new TaskCompletionSource<VerificationResult>();
            string encryptStr = Tool.RsaEncrypt(json, key);
            if (string.IsNullOrEmpty(encryptStr))
            {
                tcs.TrySetException(new Exception("RSA Encrypt Failed"));
            }
            else
            {
                string path;
                if (activelyManualVerify)
                    path = $"real-name/v1/{TapTapAntiAddictionManager.AntiAddictionConfig.regionStr}/clients/{gameId}/users/{TapTapAntiAddictionManager.UserId}/manual";
                else
                    path = $"real-name/v1/{TapTapAntiAddictionManager.AntiAddictionConfig.regionStr}/clients/{gameId}/users/{TapTapAntiAddictionManager.UserId}/fallback";
                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    ["data"] = encryptStr
                };
                ServerVerificationResponse response = await HttpClient.Post<ServerVerificationResponse>(path, data: data);
                tcs.TrySetResult(response.Result);
            }
            return await tcs.Task;
        }

        /// <summary>
        /// 检测是否可玩
        /// </summary>
        /// <returns></returns>
        internal static async Task<PlayableResult> CheckPlayable() 
        {
            string path = "";
            if (!enableTestMode) {
                path = $"anti-addiction/v1/{TapTapAntiAddictionManager.AntiAddictionConfig.regionStr}/clients/{gameId}/users/{TapTapAntiAddictionManager.UserId}/playable";
            }
            else {
                path = $"anti-addiction/v1/clients/{gameId}/users/{TapTapAntiAddictionManager.UserId}/playable";
                path = FixForTestMode(path);
            }
            Dictionary<string, object> headers = GetAuthHeaders();
            PlayableResponse response = await HttpClient.Post<PlayableResponse>(path, headers: headers);
            #if UNITY_EDITOR
            TapLogger.Debug($"检查是否可玩结果: canPlay: {response.Result.CanPlay} remainTime: {response.Result.RemainTime} CostTime: {response.Result.CostTime} IsAdult: {response.Result.IsAdult} Content: {response.Result.Content}");
            #endif
            return response.Result;
        }

        /// <summary>
        /// 检测是否可充值
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        internal static async Task<PayableResult> CheckPayable(long amount) 
        {
            string path = "";
            if (!enableTestMode) {
                path = $"anti-addiction/v1/{TapTapAntiAddictionManager.AntiAddictionConfig.regionStr}/clients/{gameId}/users/{TapTapAntiAddictionManager.UserId}/payable";
            }
            else {
                path = $"anti-addiction/v1/clients/{gameId}/users/{TapTapAntiAddictionManager.UserId}/payable";
                path = FixForTestMode(path);
            }
            Dictionary<string, object> headers = GetAuthHeaders();
            Dictionary<string, object> data = new Dictionary<string, object> 
            {
                { "amount", amount }
            };
            PayableResponse response = await HttpClient.Post<PayableResponse>(path, headers: headers, data: data);
            return response.Result;
        }

        /// <summary>
        /// 上传充值操作
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        internal static async Task SubmitPayment(long amount) 
        {
            string path = "";
            if (!enableTestMode) {
                path = $"anti-addiction/v1/{TapTapAntiAddictionManager.AntiAddictionConfig.regionStr}/clients/{gameId}/users/{TapTapAntiAddictionManager.UserId}/payments";
            }
            else {
                path = $"anti-addiction/v1/clients/{gameId}/users/{TapTapAntiAddictionManager.UserId}/payments";
                path = FixForTestMode(path);
            }
            Dictionary<string, object> headers = GetAuthHeaders();
            Dictionary<string, object> data = new Dictionary<string, object> 
            {
                { "amount", amount }
            };
            await HttpClient.Post<SubmitPaymentResponse>(path, headers:headers, data: data);
        }

        internal static Dictionary<string, object> GetAuthHeaders() 
        {
            string token = Verification.GetCurrentToken();
            if (string.IsNullOrEmpty(token)) 
            {
                return null;
            }

            return new Dictionary<string, object> 
            {
                { "Authorization", token }
            };
        }

        internal static void SetTestEnvironment(bool enable) {
            enableTestMode = enable;
        }
    }
}
