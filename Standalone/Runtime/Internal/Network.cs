using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TapTap.AntiAddiction.Internal.Http;
using TapTap.AntiAddiction.Model;
using TapTap.Common;
using TapTap.Login;
using TapTap.Login.Internal;
using TapTap.Login.Standalone;
using UnityEngine;

namespace TapTap.AntiAddiction.Internal
{
    public static class Network {
        static readonly string ChinaHost = "https://tds-tapsdk.cn.tapapis.com";

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
            HttpClient.ChangeAddtionalHeader("Accept-Language","zh-CN");
        }
        
        internal static void ChangeHost()
        {
            string host = ChinaHost;
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
        internal static async Task<RealNameConfigResult> FetchConfig(string userId) {
            string path = $"real-name/v2/get-global-config?client_id={gameId}&user_identifier={userId}";
            RealNameConfigResponse response = await HttpClient.Get<RealNameConfigResponse>(path);
            return response.Result;
        }

        /// <summary>
        /// 拉取实名认证数据
        /// </summary>
        /// <returns></returns>
        internal static async Task<VerificationResult> FetchVerification(string userId) 
        {
            string path = $"real-name/v2/anti-addiction-token?client_id={gameId}&user_identifier={userId}";
            ServerVerificationResponse response = await HttpClient.Get<ServerVerificationResponse>(path);
            return response.Result;
        }

         /// <summary>
        /// V1 升级 v2 token
        internal static async Task<VerificationResult> UpgradeToken(string userId, string oldToken) 
        {
            string path = $"real-name/v2/anti-addiction-token-upgrade?client_id={gameId}&user_identifier={userId}";
            var param = new Dictionary<string, object> {
                ["anti_addiction_token_v1"] = oldToken
            };
            ServerVerificationResponse response = await HttpClient.Post<ServerVerificationResponse>(path,data:param);
            return response.Result;
        }

        /// </summary>
        /// 使用 TapToken 获取实名 token
        /// <returns></returns>
        public static async Task<VerificationResult> FetchVerificationByTapToken(string userId, AccessToken token, long timestamp = 0) {
            string path = $"real-name/v2/anti-addiction-token-taptap?client_id={gameId}&user_identifier={userId}";            
            var httpClientType = typeof(AntiAddictionHttpClient);
            var hostFieldInfo = httpClientType.GetField("serverUrl", BindingFlags.NonPublic | BindingFlags.Instance);
            string host = hostFieldInfo?.GetValue(HttpClient) as string;
            var uri = new Uri(host  + "/" +  path);

            var sign = GetMacToken(token, uri, timestamp);
            var headers = new Dictionary<string, object> {
                { "Authorization", sign }
            };
            ServerVerificationResponse response = await HttpClient.Get<ServerVerificationResponse>(path, headers:headers);
            return response.Result;
        }
        
        private static string GetMacToken(AccessToken token, Uri uri, long timestamp = 0) {
            TapLogger.Debug(" uri = " + uri.Host + " path = " + uri.PathAndQuery + " token mac = "
             + token.macKey);
            int ts = (int)timestamp;
            if (ts == 0) {
                var dt = DateTime.UtcNow - new DateTime(1970, 1, 1);
                ts = (int)dt.TotalSeconds;
            }
            TapLogger.Debug(" GetMacToken ts = " + ts);
            var sign = "MAC " + LoginService.GetAuthorizationHeader(token.kid,
                token.macKey,
                token.macAlgorithm,
                "GET",
                uri.PathAndQuery,
                uri.Host,
                "443", ts);
            return sign;
        }
        
        /// <summary>
        /// 检测身份信息是否通过
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="idCard">身份证信息</param>
        /// <returns></returns>
        internal static async Task<VerificationResult> FetchVerificationManual(string userName, string idCard)
        {
            var tcs = new TaskCompletionSource<VerificationResult>();
            string path = $"real-name/v2/anti-addiction-token-manual?client_id={gameId}&user_identifier={TapTapAntiAddictionManager.UserId}";
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["name"] = userName,
                ["id_card"] = idCard
            };
            ServerVerificationResponse response = await HttpClient.Post<ServerVerificationResponse>(path, data: data);
            tcs.TrySetResult(response.Result);
            
            return await tcs.Task;
        }

        /// <summary>
        /// 获取用户配置
        /// </summary>
        /// <returns></returns>
        internal static async Task<UserAntiAddictionConfigResult> CheckUserConfig() 
        {
            string path;
            if (!enableTestMode) {
             path = $"anti-addiction/v2/get-config-by-token?platform=pc&client_id={gameId}&user_identifier={TapTapAntiAddictionManager.UserId}";
            }else{
             path = $"anti-addiction/v2/get-config-by-token?platform=pc&client_id={gameId}&user_identifier={TapTapAntiAddictionManager.UserId}&test_mode=1";
            }
            Dictionary<string, object> headers = GetAuthHeaders();
            UserAntiAddictionConfigResponse response = await HttpClient.Get<UserAntiAddictionConfigResponse>(path, headers: headers);
            #if UNITY_EDITOR
            TapLogger.Debug($"检查用户状态: ageLimit: {response.Result.userState.ageLimit} ageCheck: {response.Result.ageCheckResult.allow}  IsAdult: {response.Result.userState.isAdult} ");
            #endif
            return response.Result;
        }
        /// <summary>
        /// 检测是否可玩
        /// </summary>
        /// <returns></returns>
        internal static async Task<PlayableResult> CheckPlayable() 
        {
            string path = "";
            if (!enableTestMode) {
                path = $"anti-addiction/v2/heartbeat?client_id={gameId}&user_identifier={TapTapAntiAddictionManager.UserId}";
            }
            else {
                path = $"anti-addiction/v2/heartbeat?client_id={gameId}&user_identifier={TapTapAntiAddictionManager.UserId}&test_mode=1";
            }
            Dictionary<string, object> headers = GetAuthHeaders();
            Dictionary<string,object> data = new Dictionary<string,object>{
                ["session_id"] = TapTapAntiAddictionManager.CurrentSession
            };
            PlayableResponse response = await HttpClient.Post<PlayableResponse>(path, headers: headers, data:data);
            #if UNITY_EDITOR
            TapLogger.Debug($"检查是否可玩结果: remainTime: {response.Result.RemainTime}  Content: {response.Result.Content}");
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
                path = $"anti-addiction/v2/payable?client_id={gameId}&user_identifier={TapTapAntiAddictionManager.UserId}&amount={amount}";
            }
            else {
                path = $"anti-addiction/v2/payable?client_id={gameId}&user_identifier={TapTapAntiAddictionManager.UserId}&amount={amount}&test_mode=1";
            }
            Dictionary<string, object> headers = GetAuthHeaders();
            PayableResponse response = await HttpClient.Get<PayableResponse>(path, headers: headers);
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
                path = $"anti-addiction/v2/payment-submit?client_id={gameId}&user_identifier={TapTapAntiAddictionManager.UserId}";
            }
            else {
                path = $"anti-addiction/v2/payment-submit?client_id={gameId}&user_identifier={TapTapAntiAddictionManager.UserId}&test_mode=1";
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
                { "X-TAP-Anti-Addiction-Token", token }
            };
        }

        internal static void SetTestEnvironment(bool enable) {
            enableTestMode = enable;
        }
    }
}
