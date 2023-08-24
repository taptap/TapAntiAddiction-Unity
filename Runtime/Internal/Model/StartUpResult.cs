﻿
namespace TapTap.AntiAddiction.Model 
{
    public static class StartUpResult
    {
        public const int LOGIN_SUCCESS      = 500;    // 登录成功
        public const int EXITED             = 1000;   // 用户登出
        public const int SWITCH_ACCOUNT     = 1001;   // 切换账号
        public const int PERIOD_RESTRICT    = 1030;   // 当前用户达到宵禁时长
        public const int DURATION_LIMIT     = 1050;   // 时长限制
        public const int REAL_NAME_STOP     = 9002;   // 实名过程中点击了关闭实名窗
    }
}
