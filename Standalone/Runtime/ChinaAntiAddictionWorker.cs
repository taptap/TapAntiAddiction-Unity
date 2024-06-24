using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Plugins.AntiAddictionUIKit;
using TapTap.UI;
using TapTap.AntiAddiction.Internal;
using TapTap.AntiAddiction.Model;
using TapTap.Common;
using TapTap.Login;
using TapTap.Login.Internal;
using TapTap.Login.Standalone;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;
using System.Linq;
using ZXing;

namespace TapTap.AntiAddiction
{
    public sealed class ChinaAntiAddictionWorker : BaseAntiAddictionWorker
    {
        #region Abstract Override

        public override Region Region => Region.China;

        private readonly string rsaPublicKey =
            "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA1pM6yfulomBTXWKiQT5gK9fY4hq11Kv8D+ewum25oPGReuEn6dez7ogA8bEyQlnYYUoEp5cxYPBbIxJFy7q1qzQhTFphuFzoC1x7DieTvfZbh+b60psEottrCD8M0Pa3h44pzyIp5U5WRpxRcQ9iULolGLHZXJr9nW6bpOsyEIFG5tQ7qCBj8HSFoNBKZH+5Cwh3j5cjmyg55WdJTimg9ysbbwZHYmI+TFPuGo/ckHT6j4TQLCmmxI8Qf5pycn3/qJWFhjx/y8zaxgn2hgxbma8hyyGRCMnhM5tISYQv4zlQF+5RashvKa2zv+FHA5DALzIsGXONeTxk6TSBalX5gQIDAQAB";

        public override string RsaPublicKey => rsaPublicKey;

        private TaptapAntiAddictionIDInputController idInputPanel;

        private static bool showPopupInVerification;

        /// <summary>
        /// 激活手动认证
        /// </summary>
        /// <param name="activelyVerifyManually">是否为主动选择激活手动认证</param>
        protected override async Task VerifyManuallyAsync(bool activelyVerifyManually)
        {
            var userId = TapTapAntiAddictionManager.UserId;
            do 
            {
                try 
                {
                    // 这里处理了拿 Verification 信息时,5xx服务器错误或者网络错误的情况,策略为不停的弹出重试弹窗
                    var result = await OpenVerificationPanelCn();
                    // await Verification.Save(userId, Region.China, result);
                    if (Verification.IsVerifyFailed) {
                        // TODO@luran:本地化
                        UIManager.Instance.OpenToast("认证未通过，请重新提交实名信息", UIManager.GeneralToastLevel.Error);
                        continue;
                    }

                    break;
                } 
                catch (TaskCanceledException) 
                {
                    TapLogger.Debug(string.Format("[TapTap: ChinaAntiAddiction] Close manual verification panel."));
                    throw;
                } 
                catch (Exception e) 
                {
                    TapLogger.Error(string.Format("[TapTap: ChinaAntiAddiction] manual verification exception! {0}", e.ToString()));
                   
                    if (e is HttpRequestException || e is WebException)
                    {
                        UIManager.Instance.OpenToast(TapTapAntiAddictionManager.LocalizationItems.Current.NetError, UIManager.GeneralToastLevel.Error);
                        continue;
                    }

                    if (e is AntiAddictionException aae)
                    {
                        if (aae.Description != null && aae.Description.Length > 0)
                        {
                            UIManager.Instance.OpenToast(aae.message, UIManager.GeneralToastLevel.Error);
                            continue;
                        }
                        if (aae.Code >= (int)HttpStatusCode.InternalServerError)
                        {
                            // TODO@luran:本地化
                            UIManager.Instance.OpenToast("请求出错", UIManager.GeneralToastLevel.Error);
                            continue;
                        }
                    }
                    if (e.Message.Contains("Interval server error."))
                    {
                        // TODO@luran:本地化
                        UIManager.Instance.OpenToast("请求出错", UIManager.GeneralToastLevel.Error);
                        continue;
                    }
                    UIManager.Instance.OpenToast(TapTapAntiAddictionManager.LocalizationItems.Current.NoVerification, UIManager.GeneralToastLevel.Error);
                    await Task.Yield();
                    
                }
            } 
            while (true);
        }
        
        protected override PlayableResult CheckOfflineMinorPlayable()
        {
            // 未成年人
            if (IsGameTime()) 
            {
                // 可玩：节假日并且是游戏时间
                HealthReminderDesc playableTip = Config.GetMinorPlayableHealthReminderTip();

                // 计算时间
                DateTimeOffset gameEndTime = Config.StrictEndTime;
                TimeSpan remain = gameEndTime - DateTimeOffset.Now;

                return new PlayableResult 
                {
                    Title = playableTip?.tipTitle,
                    Content = playableTip?.tipDescription,
                    RemainTime = Convert.ToInt32(remain.TotalSeconds)
                };
            }

            // 不可玩：未成年人不在可玩时间
            HealthReminderDesc unplayableTip = Config.GetMinorUnplayableHealthReminderTip();
            return new PlayableResult 
            {
                Title = unplayableTip?.tipTitle,
                Content = unplayableTip?.tipDescription,
                RemainTime = 0
            };
        }
        
        protected override async Task<int> InternalStartup(string userId, bool isTapUser) {
            try {
                UIManager.Instance.CloseLoading();
                return await GetVerificationResult(userId, isTapUser);
            }
            catch (Exception e) {
                TapLogger.Error("[TapTap: ChinaAntiAddiction] " + e.ToString());
                return StartUpResult.INVALID_CLIENT_OR_NETWORK_ERROR;
            }
        
        }
        
        #endregion
        
        private static bool HaveComplianceScope(AccessToken token) {
            if (token == null ) return false;
            bool useAgeRange = TapTapAntiAddictionManager.config.useAgeRange;
            if(useAgeRange){
               return token.scopeSet != null && token.scopeSet.Contains(TapLogin.TAP_LOGIN_SCOPE_COMPLIANCE);
            }else{
               return token.scopeSet != null && 
               (token.scopeSet.Contains(TapLogin.TAP_LOGIN_SCOPE_COMPLIANCE) || token.scopeSet.Contains(TapLogin.TAP_LOGIN_SCOPE_COMPLIANCE_BASIC));
            }
        }
        
        private static async Task<ComplianceCodeData> GetComplianceCode(string clientId, AccessToken token) {
            string url = TapTapSdk.CurrentRegion.ApiHost() + "/account/compliance/v1?client_id=" + clientId;
            var uri = new Uri(url);
            var sign = GetMacToken(token, uri);
            var headers = new Dictionary<string, object> {
                { "Authorization", sign }
            };
            ComplianceCodeResponse response = await LoginService.HttpClient.Get<ComplianceCodeResponse>(url, headers: headers);
            return response.Data;
        }
        
        private static string GetMacToken(AccessToken token, Uri uri, int timestamp = 0) {
            var ts = timestamp;
            if (ts == 0) {
                var dt = DateTime.UtcNow - new DateTime(1970, 1, 1);
                ts = (int)dt.TotalSeconds;
            }
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
        /// 使用 TapToken 获取实名信息
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="accessToken"></param>
        /// <returns>0-正常;1-走手动;-1-打断流程</returns>
        private static async Task<int> FetchByTapToken(string userId, AccessToken accessToken) {
            UIManager.Instance.OpenLoading();
            var tcs = new TaskCompletionSource<int>();
            // get verification
            try {
                VerificationResult result = await Verification.FetchVerificationByTapToken(userId,accessToken);
                UIManager.Instance.CloseLoading();
                if(result.Status != null && result.Status.Equals(AntiAddictionConst.VERIFICATION_STATUS_SUCCESS)){
                    tcs.TrySetResult(0);
                }else{
                    tcs.TrySetResult(-1);
                }
            }
            catch (Exception e) {
                TapLogger.Error(e.ToString());
                UIManager.Instance.CloseLoading();
                var aae = e as TapException;
                if (aae != null && aae.code >= (int)HttpStatusCode.InternalServerError && aae.code < 600) {
                    tcs.TrySetResult(1);
                    TapLogger.Warn($"[AntiAddiction] 通过 code 拿去实名信息网络错误,将打断认证流程");
                }
                else {
                    TapLogger.Warn($"[AntiAddiction] 通过 code 拿去实名信息失败,将启动手动认证");
                    tcs.TrySetResult(1);
                }
            }
            UIManager.Instance.CloseLoading();
            return tcs.Task.Result;
        }
        
        
        /// <summary>
        /// 是否在允许游戏时间段
        /// </summary>
        private bool IsGameTime()
        {
            string currentDate = System.DateTime.Now.ToString( "yyyy-MM-dd" );
            List<string> holidays = TapTapAntiAddictionManager.CurrentUserAntiResult.localConfig.timeRangeConfig.holidays;
            TapLogger.Debug("current date = " + currentDate + " holidays = " + holidays.ToArray());
            if(holidays.Contains(currentDate)){
                DateTimeOffset strictStart = Config.StrictStartTime;
                DateTimeOffset strictEnd = Config.StrictEndTime;

                bool playable;
                DateTimeOffset now = DateTimeOffset.Now;
                TapLogger.Debug(" now = " + now + " ,start = " + strictStart + ", end = " + strictEnd);
                playable = now >= strictStart && now < strictEnd;
                return playable;
            }
            return false;
        }

        /// <summary>
        /// 打开中国实名制窗口
        /// </summary>
        /// <param name="activelyVerifyManually">是否为主动选择激活手动认证</param>
        /// <returns></returns>
        private Task<VerificationResult> OpenVerificationPanelCn()
        {
            var tcs = new TaskCompletionSource<VerificationResult>();
            var path = AntiAddictionConst.GetPrefabPath(AntiAddictionConst.ID_NUMBER_INPUT_PANEL_NAME,
                false);
            idInputPanel =
                UIManager.Instance.OpenUI<TaptapAntiAddictionIDInputController>(path);
            idInputPanel.activeManualVerification = true;;
            if (idInputPanel != null)
            {
                idInputPanel.OnVerified = (verification) => tcs.TrySetResult(verification);
                idInputPanel.OnException = (e) =>
                {
                    if (e is HttpRequestException || e is WebException)
                    {
                        tcs.TrySetException(e);
                    }
                    else
                    {
                        if (e is AntiAddictionException aae)
                        {
                                tcs.TrySetException(e);
                        }
                        else
                        {
                            if (e.Message.Contains("Interval server error."))
                                tcs.TrySetException(e);
                            else {
                                UIManager.Instance.OpenToast("身份证号码错误", UIManager.GeneralToastLevel.Error);
                            }
                        }
                    }
                };
                idInputPanel.OnClosed = () => tcs.TrySetCanceled();
            }

            return tcs.Task;
        }
        
        private async Task<int> GetVerificationResult(string userId, bool justQuickVerify) {
            var tcs = new TaskCompletionSource<int>();
            int mannualVerify = 0;
            AccessToken accessToken = await TapLogin.GetAccessToken();
            bool isTapUser = accessToken != null;
            TapLogger.Debug(" Verification current = " + Verification.Current );            
            //0 不使用手动验证;1 主动使用手动认证; 2 保底触发(被动)使用手动验证
            try {
                accessToken = await GetAccessToken(accessToken, justQuickVerify);
                if (accessToken != null) {
                    TapLogger.Debug("[TapTap: ChinaAntiAddiction] 获得登录Access Token!Token结果: {0}\n是否包括Compliance: {1}",
                        accessToken.ToJson(), HaveComplianceScope(accessToken));
                    if (HaveComplianceScope(accessToken)) {
                        // 0-正常;1-走手动;-1-打断流程
                        var fetchResult = await FetchByTapToken(userId, accessToken);
                        if (fetchResult == 1) {
                            UIManager.Instance.OpenToast("授权异常", UIManager.GeneralToastLevel.Error);
                            mannualVerify = 2;
                        }else if (fetchResult == -1) {
                           mannualVerify = 2;
                        }
                    }
                    else {
                        //TODO@luran:本地化
                        UIManager.Instance.OpenToast("授权错误", UIManager.GeneralToastLevel.Error);
                        mannualVerify = 2;
                    }
                }
                else {
                    TapLogger.Debug("[TapTap: ChinaAntiAddiction] 不能登录Access Token!直接降级为手动认证");
                    mannualVerify = 2;
                }
            }
            catch (AggregateException aggregateException) {
                var cancelException = aggregateException.InnerException as TaskCanceledException;
                if (cancelException != null) {
                    TapLogger.Debug("[TapTap: ChinaAntiAddiction] 获得登录Access Token 中主动退出! 触发 AggregateException.TaskCanceledException");
                    tcs.TrySetResult(StartUpResult.REAL_NAME_STOP);
                    return tcs.Task.Result;
                }
            }
            catch (TaskCanceledException) {
                TapLogger.Debug("[TapTap: ChinaAntiAddiction] 获得登录Access Token 中主动退出! 触发 TaskCanceledException");
                tcs.TrySetResult(StartUpResult.REAL_NAME_STOP);
                return tcs.Task.Result;
            }

            catch (AntiAddictionException aae) {
                TapLogger.Debug(
                    $"[TapTap: ChinaAntiAddiction] 获取快速实名制信息出错! code: {aae.Code} error: {aae.Error} errorMsg: {aae.Description}");
                //TapToken过期->重新授权实名流程
                if (aae.message.ToLower().Contains("refuse quick verify")) {
                    mannualVerify = 1;
                }
                else {
                    mannualVerify = 2;
                }
            }
            catch (Exception e) {
                TapLogger.Debug(string.Format("[TapTap: ChinaAntiAddiction] 获得登录Access Token 碰到错误,降级为手动验证! 错误信息: {0}", e.ToString()));
                mannualVerify = 2;
            }
            
                
            // 手动认证
            if (mannualVerify != 0) {
                try {
                    await VerifyManuallyAsync(mannualVerify == 1);
                }
                catch (TaskCanceledException) {
                    TapLogger.Debug("[TapTap: ChinaAntiAddiction] 手动实名制过程中主动退出! 触发 TaskCanceledException");
                    tcs.TrySetResult(StartUpResult.REAL_NAME_STOP);
                    return tcs.Task.Result;
                }
            }

            try {
                var tempResult = await ValidateVerification(showPopupInVerification);
                tcs.TrySetResult(tempResult);
                bool isVerified = tempResult == 0;
                if (isVerified && mannualVerify == 0) {
                    if (isTapUser && profile == null) {
                        profile = await TapLogin.GetProfile();
                    }
                    await ShowVerifiedToast(isTapUser);
                }
                //认证失败:前置无弹窗的话，回调取消；前置有弹窗，保持弹窗
                else if (tempResult == -3) {
                    if (showPopupInVerification) {
                        tcs.TrySetResult(await GetVerificationResult(userId, justQuickVerify));
                    }
                    else {
                        TapLogger.Debug("[TapTap: ChinaAntiAddiction] 认证失败,并且没有前置弹窗,直接触发取消回调! 触发 TaskCanceledException");
                        tcs.TrySetResult(StartUpResult.REAL_NAME_STOP);
                    }
                }
            }
            catch (TaskCanceledException) {
                TapLogger.Debug("[TapTap: ChinaAntiAddiction] 手动实名制过程中主动退出! 触发 TaskCanceledException");
                tcs.TrySetResult(StartUpResult.REAL_NAME_STOP);
            }
            catch (Exception e) {
                tcs.TrySetException(e);
            }

            return tcs.Task.Result;
        }
        
        /// <summary>
        /// 验证已经获取到的 Verification 是否有效,如果有效,则直接返回;如果无效,则弹出相应的提示
        /// </summary>
        /// <param name="havePopupBefore">前置是否有弹窗</param>
        /// <returns>-1:认证中;-2:实名信息为空;-3:认证未通过;0-认证成功</returns>
        private async Task<int> ValidateVerification(bool havePopupBefore) {
            var tcs = new TaskCompletionSource<int>();
            if (Verification.Current == null) {
                TapLogger.Error("[TapTap: ChinaAntiAddiction] 已经获取过实名信息,但是到了验证阶段实名信息仍未空!");
                tcs.TrySetResult(-2);
            }
            else if (Verification.IsVerifing) {
                var tip = Config.GetInputIdentifyBlockingTip();
                Action onOk = () => {
                    TapLogger.Debug("[TapTap: ChinaAntiAddiction] 认证中,退出游戏!");
                    tcs.TrySetResult(-1);
                    #if !UNITY_EDITOR
                    Application.Quit();
                    #else
                    EditorApplication.isPlaying = false;
                    #endif
                };
                TapTapAntiAddictionUIKit.OpenHealthPaymentPanel(tip.Title, tip.Content, tip.PositiveButtonText, onOk);
                await tcs.Task;
            }
            else if (Verification.IsVerifyFailed) {
                // TODO:多语言
                UIManager.Instance.OpenToast("认证未通过, 请在 Tap 客户端重新提交实名信息", UIManager.GeneralToastLevel.Error);
                if (havePopupBefore) {
                    tcs.TrySetResult(-3);
                }
                else {
                    throw new TaskCanceledException();
                }
            }
            else {
                TapLogger.Debug("[TapTap: ChinaAntiAddiction] 认证成功!接入认证!");
                tcs.TrySetResult(0);
            }

            await tcs.Task;
            return tcs.Task.Result;
        }
        
        /// <summary>
        /// 获取 Token,分为几种情况:1)不是TapUser的话,需要去展示授权界面,授权界面可能选择拒绝,这时转为手动;2)是TapUser,但是没有Compliance权限,这时也需要去展示授权界面;3)是TapUser,并且有Compliance权限,这时直接返回Token
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="justQuickVerify"></param>
        /// <returns></returns>
        private static async Task<AccessToken> GetAccessToken(AccessToken accessToken, bool justQuickVerify) {
            showPopupInVerification = false;
            bool haveCompliance = false;
            var isTapUser = IsTapUser(accessToken);
            if (isTapUser) {
                haveCompliance = HaveComplianceScope(accessToken);
            }
            TapLogger.Debug($"[TapTap: ChinaAntiAddiction] 启动快速实名制. 是否为Tap用户: {isTapUser} 是否包含Complaince: {haveCompliance}");
            return await InternalGetAccessToken(isTapUser, haveCompliance, accessToken);
        }

        private static async Task<AccessToken> InternalGetAccessToken(bool isTapUser, bool haveCompliance, AccessToken accessToken) {
            var tcs = new TaskCompletionSource<AccessToken>();
            Func<Task<AccessToken>> getToken = null;
            // 先弹出快速认证提示,再弹出授权页面
            showPopupInVerification = true;
            bool useAgeRange = TapTapAntiAddictionManager.config.useAgeRange;
            string permission = useAgeRange ? TapLogin.TAP_LOGIN_SCOPE_COMPLIANCE :TapLogin.TAP_LOGIN_SCOPE_COMPLIANCE_BASIC;
            bool justShowConfirmBtn = isTapUser && !haveCompliance|| !Config.Current.useManual;
            var goQuickVerify = await ShowQuickVerifyTipWindow(justShowConfirmBtn);
            TapLogger.Debug($"[TapTap: ChinaAntiAddiction] 因为不是Tap用户启动快速认证提示弹窗!弹窗结果: {goQuickVerify}");
            // 同意授权
            if (goQuickVerify) {
                getToken = () =>
                    //https://xindong.slack.com/archives/C0271SPG77A/p1701161783480859?thread_ts=1700206625.065689&cid=C0271SPG77A
                    TapLogin.Authorize(new string[] { permission});
            }
            //不同意授权
            else {
                TapLogger.Debug("[TapTap: ChinaAntiAddiction] 用户拒绝快速认证!");
            }
            
            // 不同意快速认证: 需要降级为手动
            if (getToken == null) {
                var aae = new AntiAddictionException(-1, "refuse quick verify");
                throw aae;
            }
            else {
                try {
                    var tToken = await getToken();
                    tcs.TrySetResult(tToken);
                }
                catch (TapException te) {
                    if (te.Code == (int)TapErrorCode.ERROR_CODE_LOGIN_CANCEL && te.Message.Contains("Login Cancel")) {
                        UIManager.Instance.OpenToast("授权取消", UIManager.GeneralToastLevel.Warning);
                        return await InternalGetAccessToken(isTapUser, haveCompliance, accessToken);
                    }
                }
                catch (Exception e) {
                    TapLogger.Error(e.ToString());
                    throw;
                }
            }
            
            return tcs.Task.Result;
        }
        
        /// <summary>
        /// 显示快速认证提示弹窗
        /// </summary>
        /// <param name="justQuickVerify">是否只包括确认快速认证按钮,如果否的话,还包含不启动快速认证按钮</param>
        /// <returns>返回 bool 里表示玩家是否选择快速认证</returns>
        private static Task<bool> ShowQuickVerifyTipWindow(bool justShowConfirmBtn) {
            var tcs = new TaskCompletionSource<bool>();
            var openParams = new TapTapAntiAddictionQuickVerifyTipController.OpenParams()
            {
                justShowConfirmBtn = justShowConfirmBtn, 
                onClicked = (isQuickVerify) => tcs.TrySetResult(isQuickVerify),
                onClose = ()=> tcs.TrySetCanceled(),
            };
            var path = AntiAddictionConst.GetPrefabPath(AntiAddictionConst.QUICK_VERIFY_TIP_PANEL_NAME,
                false);
            UIManager.Instance
                .OpenUI<TapTapAntiAddictionQuickVerifyTipController>(path, openParams);
            return tcs.Task;
        }
        
        private static bool IsTapUser(AccessToken accessToken) {
            return accessToken != null && !string.IsNullOrEmpty(accessToken.kid);
        }
    }
}