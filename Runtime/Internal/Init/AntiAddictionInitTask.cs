using TapTap.AntiAddiction.Model;
using TapTap.Common;
using TapTap.Common.Internal.Init;

namespace TapTap.AntiAddiction.Internal.Init {
    public class AntiAddictionInitTask : IInitTask {
        public int Order => 12;

        public void Init(TapConfig config) {
            AntiAddictionConfig antiAddictionConfig = AntiAddictionConfig.Config;
            if (antiAddictionConfig == null) {
                antiAddictionConfig = new AntiAddictionConfig() {
                    gameId = config.ClientID,
                    useTapLogin = true,
                    showSwitchAccount = true,
                    region =  Region.China 
                };
            }
            else {
                antiAddictionConfig.gameId = config.ClientID;
                antiAddictionConfig.region = Region.China;
            }
            AntiAddictionUIKit.Init(antiAddictionConfig);
        }
    }
}