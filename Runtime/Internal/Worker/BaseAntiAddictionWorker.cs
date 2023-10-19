﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TapTap.UI;
using TapTap.AntiAddiction.Internal;
using TapTap.AntiAddiction.Model;
using TapTap.Common;
using TapTap.Common.Internal.Utils;
using TapTap.Login;
using UnityEngine;
using Network = TapTap.AntiAddiction.Internal.Network;

namespace TapTap.AntiAddiction
{
    public abstract class BaseAntiAddictionWorker {
        protected AntiAddictionConfig config => TapTapAntiAddictionManager.AntiAddictionConfig;
        
        #region Abstract
        
        public abstract Region Region { get; }

        protected static Profile profile;
        
        /// <summary>
        /// 传送身份验证等敏感信息时的key
        /// </summary>
        /// <param name="isRnd"></param>
        /// <returns></returns>
        public abstract string RsaPublicKey { get; }
        
        /// <summary>
        /// 激活手动认证
        /// </summary>
        /// <param name="activelyVerifyManually">是否为主动选择激活手动认证</param>
        protected abstract Task VerifyManuallyAsync(bool activelyVerifyManually);
        
        /// <summary>
        /// 检查未成年人可玩性
        /// </summary>
        /// <returns></returns>
        protected abstract PlayableResult CheckOfflineMinorPlayable();
        
        /// <summary>
        /// 是否激活单机模式
        /// </summary>
        /// <returns></returns>
        public abstract Task<bool> IsStandaloneEnabled();

        /// <summary>
        /// 直接通过 Tap 账号获取实名制信息
        /// </summary>
        /// <returns></returns>
        protected abstract Task<int> InternalStartup(string userId, bool useTapLogin);

        #endregion

        #region Virutal
        
        /// <summary>
        /// 检查可玩性后,已知成年人时的处理
        /// </summary>
        /// </summary>
        /// <param name="playable"></param>
        protected virtual void OnCheckedPlayableWithAdult(PlayableResult playable)
        {
            TryStartPoll();
        }
        
        /// <summary>
        /// 检查可玩性后,已知未成年人时的处理
        /// </summary>
        /// <param name="playable"></param>
        /// <returns></returns>
        protected virtual async Task<int> OnCheckedPlayableWithMinorAsync(PlayableResult playable)
        {
            var tcs = new TaskCompletionSource<int>();
            Action onOk;
            if (playable.CanPlay) {
                onOk = () => {
                    tcs.TrySetResult(StartUpResult.LOGIN_SUCCESS);
                    TryStartPoll();
                };
            } 
            else {
                tcs.TrySetResult(StartUpResult.PERIOD_RESTRICT);
                onOk = () => {
                    Application.Quit();
                };
            }
            Action onSwitchAccount = null;
            if (config.showSwitchAccount) {
                onSwitchAccount = () => {
                    Logout();
                    AntiAddictionUIKit.ExternalCallback?.Invoke(StartUpResult.SWITCH_ACCOUNT, null);
                };
            }
            
            TapTapAntiAddictionUIKit.OpenHealthReminderPanel(playable, onOk, onSwitchAccount);
            return await tcs.Task;
        }
        
        /// <summary>
        /// 检查可玩性,如果出现异常就会用本地的计算方式
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<PlayableResult> CheckPlayableAsyncWithFallback()
        {
            try 
            {
                var playableResult = await CheckPlayableAsync();
                return playableResult;
            } 
            catch (AntiAddictionException e) 
            {
                if (e.Code == 401)
                {
                    throw;
                }
                return CheckOfflinePlayable();
            }
            catch (Exception e) 
            {
                TapLogger.Error(e);
                // 单机判断是否可玩
                return CheckOfflinePlayable();
            }
        }
        
        /// <summary>
        /// 检查可玩性
        /// </summary>
        /// <param name="shouldThrowException">当内部发生错误的时候,是否抛出异常.默认不抛出异常,就会按照本地规则计算playable</param>
        /// <returns></returns>
        protected virtual async Task<PlayableResult> CheckPlayableAsync()
        {
            try 
            {
                if (!Config.NeedUploadUserAction) return CheckOfflinePlayable();
                var playableResult = await Network.CheckPlayable();
                return playableResult;
            }
            catch (Exception e) 
            {
                TapLogger.Error(e);
                throw;
            }
        }
        
        /// <summary>
        /// 本地判断可玩性
        /// </summary>
        /// <returns></returns>
        protected virtual PlayableResult CheckOfflinePlayable()
        {
            // 成年人
            if (Verification.IsAdult) 
            {
                // 可玩
                return new PlayableResult 
                {
                    RestrictType = PlayableResult.ADULT,
                    CanPlay = true,
                };
            }
            
            return CheckOfflineMinorPlayable();
        }
        
        /// <summary>
        /// 是否需要开启轮询检查可玩性
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsNeedStartPoll()
        {
            return !Verification.IsAdult || Config.NeedUploadUserAction;
        }
        
        /// <summary>
        /// 轮询时,检查可玩性判断
        /// </summary>
        /// <returns></returns>
        public virtual async Task<PlayableResult> CheckPlayableOnPollingAsync()
        {
            try 
            {
                var playable = await CheckPlayableAsyncWithFallback();

                if (!playable.CanPlay)
                {
                    OnUnplayablePostPoll(playable);
                }

                return playable;
            } 
            catch (AntiAddictionException e) 
            {
                if (e.Code == 401) {
                    // TapTapAntiAddictionManager.Logout();
                }
                return CheckOfflinePlayable();
            }
        }
        
        /// <summary>
        /// 轮询时,发现不可玩处理
        /// </summary>
        /// <param name="playable"></param>
        protected virtual void OnUnplayablePostPoll(PlayableResult playable)
        {
            
            Action onExitGame = Application.Quit;
            Action onSwitch = null;
            if (config.showSwitchAccount) 
            {
                onSwitch = () => {
                    Logout();
                    AntiAddictionUIKit.ExternalCallback?.Invoke(StartUpResult.SWITCH_ACCOUNT, null);
                };
            }
            TapTapAntiAddictionUIKit.OpenHealthReminderPanel(playable, onExitGame, onSwitch);
            AntiAddictionUIKit.ExternalCallback?.Invoke(StartUpResult.PERIOD_RESTRICT, null);
        }
        
        /// <summary>
        /// 通过检查,发现不可支付时的处理
        /// </summary>
        /// <param name="payable"></param>
        protected virtual void OnCheckedUnpayable(PayableResult payable)
        {
            TapTapAntiAddictionUIKit.OpenHealthPaymentPanel(payable);
        }
        
        /// <summary>
        /// 获得配置后的处理
        /// </summary>
        public virtual void OnConfigFetched()
        {
            Config.OnFetched();
        }
        
        /// <summary>
        /// 登出处理
        /// </summary>
        public virtual void Logout()
        {
            Verification.Logout();
            AntiAddictionPoll.Logout();
            TapTapAntiAddictionManager.UserId = null;
        }
        
        #endregion
        
        #region Internal

        public async Task<int> StartUp(string userId, bool isTapUser) {
            var lastVerificationValid = await CheckLastVerificationValid(userId);
            if (!lastVerificationValid) {
                var result = await InternalStartup(userId, isTapUser);
                // 目前只会返回 9002 和 0 , -1, -2, -3
                // 9002 是 StartUpResult.REAL_NAME_STOP
                // -1 认证中
                // 0 认证成功
                // -2 认证失败
                // -3 认证取消
                if (result != 0) {
                    return result;
                }
            }
            else {
                UIManager.Instance.CloseLoading();
            }
            return await OnVerificationFetched();
        }

        private async Task<bool> CheckLastVerificationValid(string userId) {
            await FetchVerificationAsync(userId);
            return Verification.IsVerified;
        }
        /// <summary>
        /// 获得实名信息
        /// </summary>
        public async Task FetchVerificationAsync(string userId) {
            // 拉取服务端实名信息
            do {
                try {
                    await Verification.Fetch(userId);
                    UIManager.Instance.CloseLoading();
                    break;
                }
                catch (HttpRequestException e) {
                    TapLogger.Error(e);
                    UIManager.Instance.CloseLoading();
                    // 网络异常，则提示重新查询
                    await TapTapAntiAddictionUIKit.ShowRetryDialog(TapTapAntiAddictionManager.LocalizationItems.Current.NetError);
                    UIManager.Instance.OpenLoading();
                } 
                catch (Exception e) {
                    TapLogger.Error(e);
                    UIManager.Instance.CloseLoading();
                    var aae = e as AntiAddictionException;
                    if (aae != null && aae.code == (int)HttpStatusCode.Unauthorized) {
                        await TapTapAntiAddictionUIKit.ShowRetryDialog(TapTapAntiAddictionManager.LocalizationItems.Current.NoVerification);
                    }
                    else if (aae != null && aae.code >= (int)HttpStatusCode.InternalServerError) {
                        await TapTapAntiAddictionUIKit.ShowRetryDialog(TapTapAntiAddictionManager.LocalizationItems.Current.NoVerification);
                    }
                    else if (e.Message.Contains("Interval server error.")) {
                        await TapTapAntiAddictionUIKit.ShowRetryDialog(TapTapAntiAddictionManager.LocalizationItems.Current.NoVerification);
                    }
                    else {
                        await TapTapAntiAddictionUIKit.ShowRetryDialog(TapTapAntiAddictionManager.LocalizationItems.Current.NetError);
                    }
                    UIManager.Instance.OpenLoading();
                }
            } 
            while (true);
        }
        
        /// <summary>
        /// 获得实名信息后的处理
        /// </summary>
        /// <returns></returns>
        public async Task<int> OnVerificationFetched() {
            return await ValidatePlayableAsync();
        }
        
        /// <summary>
        /// 检查可玩性
        /// </summary>
        /// <returns></returns>
        private async Task<int> ValidatePlayableAsync()
        {
            int tryCount = 0;
            do {
                try{
                    tryCount++;
                    PlayableResult playable = await CheckPlayableAsyncWithFallback();
                    TapTapAntiAddictionManager.CurrentPlayableResult = playable;
                    // 2.1  成年人-后处理
                    if (playable.IsAdult) {
                        OnCheckedPlayableWithAdult(playable);
                        return StartUpResult.LOGIN_SUCCESS;
                    }
                    // 2.2  未成年人-后处理
                    else{
                        return await OnCheckedPlayableWithMinorAsync(playable);
                    }
                }
                catch (Exception e){
                    var aae = e as AntiAddictionException;
                    if (aae.Code == 401){
                        await TapTapAntiAddictionUIKit.ShowRetryDialog(TapTapAntiAddictionManager.LocalizationItems.Current.NetError);
                    }
                    if (Verification.IsVerified){
                        if (!Verification.IsAdult)
                            await TapTapAntiAddictionUIKit.ShowRetryDialog(TapTapAntiAddictionManager.LocalizationItems.Current.NetError);
                        else if (tryCount >= 3){
                            var playableResult = new PlayableResult
                            {
                                RestrictType = PlayableResult.ADULT,
                                CanPlay = true,
                            };
                            TapTapAntiAddictionManager.CurrentPlayableResult = playableResult;
                            OnCheckedPlayableWithAdult(playableResult);
                            return StartUpResult.LOGIN_SUCCESS;
                        }
                    }
                }
            }while (true);
        }
        
        /// <summary>
        /// 尝试开启轮询检查
        /// </summary>
        protected void TryStartPoll()
        {
            if (IsNeedStartPoll())
            {
                AntiAddictionPoll.StartUp();
            }
        }
        
        /// <summary>
        /// 检查是否可以支付
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async virtual Task<PayableResult> CheckPayableAsync(long amount)
        {
            PayableResult payable = await Network.CheckPayable(amount);
            if (!payable.Status)
            {
                OnCheckedUnpayable(payable);
            }
            return payable;
        }
        
        /// <summary>
        /// 提交充值结果
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public virtual Task SubmitPayResult(long amount)
        {
            return Network.SubmitPayment(amount);
        }
        
        /// <summary>
        /// 获取配置
        /// </summary>
        public async Task FetchConfigAsync()
        {
            await Config.Fetch();
        }

        protected async Task ShowVerifiedToast(bool useAvatar) {
            Texture avatar = UI.UIManager.TapTapToastIcon;
            string str = "您已在 TapTap 实名！";
            if (useAvatar) {
                Texture userAvatar = null;
                try {
                    userAvatar = await ImageUtils.LoadImage(profile?.avatar);
                }
                finally {
                    if (userAvatar != null){
                        avatar = userAvatar;
                    }
                }
            }
            UI.UIManager.Instance.OpenToast(false, str, 3, icon: avatar);
            
        }
        
        #endregion
    }
}