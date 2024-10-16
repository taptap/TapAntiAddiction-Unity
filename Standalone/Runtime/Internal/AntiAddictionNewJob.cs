using System;
using System.Threading.Tasks;
using TapTap.AntiAddiction.Internal;
using TapTap.AntiAddiction.Model;
using TapTap.Login;
using CheckPayResult = Plugins.AntiAddictionUIKit.CheckPayResult;

namespace TapTap.AntiAddiction
{
    public sealed class AntiAddictionNewJob : IAntiAddictionJob
    {
        private bool UseAgeRange = true;
        private Action<int, string> _externalCallback;

        public Action<int, string> ExternalCallback
        {
            get => _externalCallback;
        }
        
        public int AgeRange
        {
            get
            {
                if (!Verification.IsVerified || !UseAgeRange){
                    return -1;
                } 
                if(Verification.AgeLimit < Verification.UNKNOWN_AGE){
                    return Verification.AgeLimit;
                }else{
                    return -1;
                }
            }
        }

        /// <summary>
        /// 剩余时间(单位:秒)
        /// </summary>
        public int RemainingTime
        {
            get
            {
                if (TapTapAntiAddictionManager.CurrentRemainSeconds == null) return 0;
                if (Verification.IsAdult) return 9999;
                return TapTapAntiAddictionManager.CurrentRemainSeconds.Value;
            }
        }

        public int RemainingTimeInMinutes
        {
            get
            {
                int seconds = RemainingTime;
                if (seconds <= 0) 
                    return 0;
                return UnityEngine.Mathf.CeilToInt(seconds / 60.0f);
            }
        }
        
        public string CurrentToken
        {
            get
            {
                if (!Verification.IsVerified) return "";
                return Verification.GetCurrentToken();
            }
        }
        
        public void Init(AntiAddictionConfig config) {
            UseAgeRange = config.useAgeRange;
            AppendLoginPermission(config.useAgeRange);
            TapTapAntiAddictionManager.Init(config);
        }

        public void SetAntiAddictionCallback(Action<int, string> callback){
            _externalCallback = callback;
        }
        
        public async void Startup(string userId, bool isTapUser) {
            await StartupAsync(userId, isTapUser);
        }
        
        public async Task StartupAsync(string userId, bool isTapUser)
        {
            if(TapTapAntiAddictionManager.UserId != null){
                TapTapAntiAddictionManager.ClearUserCache();
            }
            var code = await TapTapAntiAddictionManager.StartUp(userId, isTapUser);
            OnInvokeExternalCallback(code,null);
        }
        
        /// <summary>
        /// Async Method
        /// </summary>
        /// <param name="userId"></param>
        public async void StartupWithTapTap(string userId)
        {
            await StartupAsync(userId, true);
        }

        // ReSharper disable Unity.PerformanceAnalysis

        public void Exit()
        {
            TapTapAntiAddictionManager.Logout();
            _externalCallback?.Invoke(StartUpResult.EXITED, null);
        }

        public void EnterGame()
        {
            TapTapAntiAddictionManager.EnterGame();
        }

        public void LeaveGame()
        {
            TapTapAntiAddictionManager.LeaveGame();
        }

        public async void CheckPayLimit(long amount, Action<CheckPayResult> handleCheckPayLimit, Action<string> handleCheckPayLimitException)
        {
            try
            {
                var payResult = await TapTapAntiAddictionManager.CheckPayLimit(amount);
                handleCheckPayLimit?.Invoke(new CheckPayResult()
                {
                    // status 为 1 时可以支付
                    status = payResult.Status ? 1 : 0,
                    title = payResult.Title,
                    description = payResult.Content
                });
            }
            catch (Exception e)
            {
                handleCheckPayLimitException?.Invoke(e.Message);
                if(e is AntiAddictionException aee && aee.IsTokenExpired()){
                    Exit();
                }
            }
        }

        public async void SubmitPayResult(long amount, Action handleSubmitPayResult, Action<string> handleSubmitPayResultException)
        {
            try
            {
                await TapTapAntiAddictionManager.SubmitPayResult(amount);
                handleSubmitPayResult?.Invoke();
            }
            catch (Exception e)
            {
                handleSubmitPayResultException?.Invoke(e.Message);
                if(e is AntiAddictionException aee && aee.IsTokenExpired()){
                    Exit();
                }
            }
        }

        public void InvokeExternalCallback(int code, string msg){
            if(ExternalCallback != null){

            }
        }
        
        public void SetTestEnvironment(bool enable) {
            TapTapAntiAddictionManager.SetTestEnvironment(enable);
        }

        public void AppendLoginPermission(bool useAgeRange)
        {
            TapTapAntiAddictionManager.SetRegion(Region.China);
            if(useAgeRange){
                TapLogin.AppendPermission(TapLogin.TAP_LOGIN_SCOPE_COMPLIANCE);
            }else{
                TapLogin.AppendPermission(TapLogin.TAP_LOGIN_SCOPE_COMPLIANCE_BASIC);
            }
        }

        public void OnInvokeExternalCallback(int code, string msg){
            switch(code){
                case StartUpResult.LOGIN_SUCCESS:
                    TapTapAntiAddictionManager.CanPlay = true;
                    break;
                case StartUpResult.AGE_LIMIT:
                case StartUpResult.PERIOD_RESTRICT:
                case StartUpResult.DURATION_LIMIT:
                case StartUpResult.EXITED:
                case StartUpResult.INVALID_CLIENT_OR_NETWORK_ERROR:
                case StartUpResult.SWITCH_ACCOUNT:
                    TapTapAntiAddictionManager.CanPlay = false;
                    break;
            }
            if (StartUpResult.Contains(code)){
                ExternalCallback?.Invoke(code, msg);
            }
        }

    }
}
