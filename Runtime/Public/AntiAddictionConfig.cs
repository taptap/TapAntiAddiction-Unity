using System;
using System.Collections.Generic;
using TapTap.AntiAddiction.Model;

namespace TapTap.AntiAddiction.Model
{
    public class AntiAddictionConfig
    {
        public static AntiAddictionConfig Config { get; set; }
        
        public string gameId;

        [Obsolete]
        public bool useTapLogin;
        
        public bool showSwitchAccount = true;

        public Region region = Region.China;

        public bool useAgeRange = true;
        
        //"g" means Displays the enumeration entry as a string value, if possible, and otherwise displays the integer value of the current instance.
        public string regionStr => region.ToString("g").ToLower();
        
        public Dictionary<string, object> ToDict() {
            return new Dictionary<string, object> {
                ["gameId"] = gameId,
                ["useTapLogin"] = useTapLogin,
                ["showSwitchAccount"] = showSwitchAccount,
                ["region"] = region,
            };
        }
    }
}

namespace TapTap.Common {
    public static class TapConfigBuilderForAntiAddiction {
        /// <summary>
        /// 扩展 TapConfig.Builder 添加防沉迷初始化配置
        /// </summary>
        /// <param name="builder">TapConfig.Builder</param>
        /// <param name="showSwitchAccount">是否显示切换账号</param>
        /// <param name="useAgeRange">是否使用年龄段</param>
        /// <param name="useTapLogin">是否使用 Tap 登录，已被废弃</param>
        /// <returns> TapConfig.Builder</returns>
        public static TapConfig.Builder AntiAddictionConfig(this TapConfig.Builder builder,
            bool showSwitchAccount,
            bool useAgeRange = true, bool useTapLogin = true) {

            AntiAddiction.Model.AntiAddictionConfig.Config = new AntiAddictionConfig(){
                showSwitchAccount = showSwitchAccount,
                useTapLogin = useTapLogin,
                useAgeRange = useAgeRange
            };
            
            return builder;
        }
    }
}
