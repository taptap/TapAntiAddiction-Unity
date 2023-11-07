using System;
using UnityEngine.UI;
using TapTap.UI;

namespace TapTap.AntiAddiction.Internal {
    public class TapTapAntiAddictionQuickVerifyTipController : BasePanelController {
        public class OpenParams : AbstractOpenPanelParameter {
            public Action<bool> onClicked;
            public bool justShowConfirmBtn;
            public Action onClose;
            public string gameName;
        }
        
        public Text titleText;
        public Text mainIntroText;
        public Text subIntroText;
        public Text confirmBtn1Text;
        public Text confirmBtn2Text;
        public Text denyBtnText;
        
        public Button confirmBtn1;
        public Button confirmBtn2;
        public Button denyBtn;
        public Button closeBtn;
        
        private OpenParams param;

        
        /// <summary>
        /// bind ugui components for every panel
        /// </summary>
        protected override void BindComponents()
        {
            titleText = transform.Find("Root/TitleText").GetComponent<Text>();
            mainIntroText = transform.Find("Root/GameIntro").GetComponent<Text>();
            subIntroText = transform.Find("Root/GameIntro/GameIntro2").GetComponent<Text>();
            confirmBtn1Text = transform.Find("Root/Button1/ConfirmBtn/Text").GetComponent<Text>();
            confirmBtn2Text = transform.Find("Root/Button2/ConfirmBtn/Text").GetComponent<Text>();
            denyBtnText = transform.Find("Root/Button2/DenyBtn/Text").GetComponent<Text>();
            
            confirmBtn1 = transform.Find("Root/Button1/ConfirmBtn").GetComponent<Button>();
            confirmBtn2 = transform.Find("Root/Button2/ConfirmBtn").GetComponent<Button>();
            denyBtn = transform.Find("Root/Button2/DenyBtn").GetComponent<Button>();
            closeBtn = transform.Find("Root/CloseButton").GetComponent<Button>();
        }

        protected override void OnLoadSuccess()
        {
            base.OnLoadSuccess();
            param = openParam as OpenParams;

            closeBtn.onClick.AddListener(() => {
                Close();
                param.onClose?.Invoke();
            });
            
            confirmBtn1.onClick.RemoveAllListeners();
            confirmBtn1.gameObject.SetActive(false);
            confirmBtn2.onClick.RemoveAllListeners();
            confirmBtn2.gameObject.SetActive(false);
            denyBtn.onClick.RemoveAllListeners();
            denyBtn.gameObject.SetActive(false);
            if (param.justShowConfirmBtn) {
                confirmBtn1.onClick.AddListener(() => {
                    this.Close();
                    param.onClicked?.Invoke(true);
                });
                confirmBtn1.gameObject.SetActive(true);
            }
            else {
                confirmBtn2.onClick.AddListener(() => {
                    this.Close();
                    param.onClicked?.Invoke(true);
                });
                confirmBtn2.gameObject.SetActive(true);
                denyBtn.onClick.AddListener(() => {
                    this.Close();
                    param.onClicked?.Invoke(false);
                });
                denyBtn.gameObject.SetActive(true);
            }
            
            var config = Config.GetQuickVerifyTipPanelTip();
            if (config != null) {
                titleText.text = config.Title;
                var splitter = "</color>";
                var index = config.Content.IndexOf(splitter);
                mainIntroText.text = string.Format(config.Content.Substring(0, index + + splitter.Length), param.gameName);
                subIntroText.text = config.Content.Substring(index + splitter.Length);
                confirmBtn1Text.text = config.PositiveButtonText;
                confirmBtn2Text.text = config.PositiveButtonText;
                denyBtnText.text = config.NegativeButtonText;
            }
        }
        
    }
}