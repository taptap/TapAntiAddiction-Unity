using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TapSDK.UI;
using TapTap.Common;
using TapTap.Login;
using TapTap.Bootstrap;
using TapTap.AntiAddiction;
using TapTap.AntiAddiction.Model;
using TapTap.AntiAddiction.Internal;
using TapTap.AntiAddiction.Internal.Http;
using Network = TapTap.AntiAddiction.Internal.Network;

public class TaptapAntiAddictionMainController : BasePanelController
{
    private const string GameIdChina = "6Rap5XF2ncLQB2oIiW";
    private const string GameIdVietnam = "uZ8Yy6cSXVOR6AMRPj";
    
    public InputField userIdInput;
    public Button startButton;
    public Button tapLoginButton;
    public Button logoutButton;

    public Button remainMinsBtn;
    public Button remainSecsBtn;
    public Button currentTokenBtn;

    public InputField amountInput;
    public InputField ipInput;
    public Button checkAmountButton;
    public Button submitAmountButton;
    
    public Button openTestVietnamKycPanelButton;

    public Toggle charlesToggle;
    public Toggle useStandaloneUIToggle;
    [FormerlySerializedAs("devToggle")] public Toggle rndToggle;

    public Dropdown regionSelectDropdown;

    private Region _region;
    
    private string ServerUrlCn => "https://tds-tapsdk.cn.tapapis.com";
    private string ServerUrlVietnam => "https://tds-account.intl.tapapis.com";
    private string ServerRndUrl => "https://tds-api.xdrnd.com";
    
        
    private string VerificationHostUrl => "https://tds-real-name.tapapis.cn";
    private string VerificationHostRndUrl => "https://tds-real-name.xdrnd.com";
        
    private int PollInterval => 60 * 2;
    private int PollIntervalRnd => 10;


    private Dictionary<Region, string> regionRndRsaKey = new Dictionary<Region, string>()
    {
        { Region.China,   "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAks5GmqBvtcVihXvUorEh3KTHBteK36/4G5e3UOnYKbahspU9+FaJx/GaQxdtnFzkVXoGHVlYkhYokY12YO+OVB9INSgzwfxDGd2ttAsqCsUBl0GCVDzBHnS0agf7hk6YVljG8dRN01yW6q50XQCqyS2L3bfXDuWmUT8upgZ6fJSJRRRGh+vt9AJRBwZb3vQ/d/iejWH/64mGnM154CZGr+28SZ8AAXiCJ0BrfyGZqbhoqeWbFUbI7zv3FXiawuqS5EatH5ZU0ll14MlXdcIK7NzcDKCb/tekkr5zPDdTOPkQ5KDrwOx6oMEYs1sLC37nB0Me6mQWQPZY0lYPQ/GmwwIDAQAB" },
        { Region.Vietnam, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAks5GmqBvtcVihXvUorEh3KTHBteK36/4G5e3UOnYKbahspU9+FaJx/GaQxdtnFzkVXoGHVlYkhYokY12YO+OVB9INSgzwfxDGd2ttAsqCsUBl0GCVDzBHnS0agf7hk6YVljG8dRN01yW6q50XQCqyS2L3bfXDuWmUT8upgZ6fJSJRRRGh+vt9AJRBwZb3vQ/d/iejWH/64mGnM154CZGr+28SZ8AAXiCJ0BrfyGZqbhoqeWbFUbI7zv3FXiawuqS5EatH5ZU0ll14MlXdcIK7NzcDKCb/tekkr5zPDdTOPkQ5KDrwOx6oMEYs1sLC37nB0Me6mQWQPZY0lYPQ/GmwwIDAQAB" },
    };    
    
    private Dictionary<Region, string> regionFormalRsaKey = new Dictionary<Region, string>()
    {
        { Region.China, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA1pM6yfulomBTXWKiQT5gK9fY4hq11Kv8D+ewum25oPGReuEn6dez7ogA8bEyQlnYYUoEp5cxYPBbIxJFy7q1qzQhTFphuFzoC1x7DieTvfZbh+b60psEottrCD8M0Pa3h44pzyIp5U5WRpxRcQ9iULolGLHZXJr9nW6bpOsyEIFG5tQ7qCBj8HSFoNBKZH+5Cwh3j5cjmyg55WdJTimg9ysbbwZHYmI+TFPuGo/ckHT6j4TQLCmmxI8Qf5pycn3/qJWFhjx/y8zaxgn2hgxbma8hyyGRCMnhM5tISYQv4zlQF+5RashvKa2zv+FHA5DALzIsGXONeTxk6TSBalX5gQIDAQAB" },
        { Region.Vietnam, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA708T4I+a6wkvT7kn16HY9VrpBE3ay8/nNtaLVFNj/LVBVB6LyIHdU+XIIwi9nj9+I1+a0R2lBds6yKDy6uiwctAwhEHFcKKMmjbdfL0db8bACflASNdrAodw38i7SjzdDrlFiFvJiktkUWnSswaLLPpan/1K3fKo5GgzBtQd8Fe4GQYJ5ghePjA4vVHrpI5rBa9Ca0Ji2YnSOwYv9lFljMCKDYoTzn/Ctsq5vzN+ZGomjz+cATIbA8+zSek+XoGltZvQEWyBtjHDK/pkzb58Kc0zAnEmMQPPtHa0pCU1moMXKIiPvw+YXEVxyvOCUKLAHnzhJNTPlzZzKWtz9VGktQIDAQAB" },
    };


    /// <summary>
    /// bind ugui components for every panel
    /// </summary>
    protected override void BindComponents() 
    {
        userIdInput = transform.Find("Container/Start/UserIdInputField").GetComponent<InputField>();
        startButton = transform.Find("Container/Start/StartButton").GetComponent<Button>();
        tapLoginButton = transform.Find("Container/Start/TapLogin").GetComponent<Button>();
        logoutButton = transform.Find("Container/Start/LogoutButton").GetComponent<Button>();

        amountInput = transform.Find("Container/Amount/AmountInputField").GetComponent<InputField>();
        checkAmountButton = transform.Find("Container/Amount/CheckAmountButton").GetComponent<Button>();
        submitAmountButton = transform.Find("Container/Amount/SubmitAmountButton").GetComponent<Button>();

        ipInput = transform.Find("Container/Charles/IpInputField").GetComponent<InputField>();
        charlesToggle = transform.Find("Container/Charles/CharlesToggle").GetComponent<Toggle>();
        useStandaloneUIToggle = transform.Find("Container/useStandaloneUIToggle").GetComponent<Toggle>();
        rndToggle = transform.Find("Container/DevToggle").GetComponent<Toggle>();
        
        regionSelectDropdown = transform.Find("Container/Region/RegionSelect/Dropdown").GetComponent<Dropdown>();
        regionSelectDropdown.ClearOptions();
        regionSelectDropdown.AddOptions(new List<string>(Enum.GetNames(typeof(Region))));
        
        openTestVietnamKycPanelButton = transform.Find("Container/Region/Button").GetComponent<Button>();
        
        remainMinsBtn = transform.Find("Container/Misc/RemainMinutes").GetComponent<Button>();
        remainSecsBtn = transform.Find("Container/Misc/RemainSeconds").GetComponent<Button>();
        currentTokenBtn = transform.Find("Container/Misc/CurrentToken").GetComponent<Button>();
    }

    protected override void OnLoadSuccess()
    {
        base.OnLoadSuccess();
        TapLogger.LogDelegate = Sample.TapLog;

        startButton.onClick.AddListener(() => OnStartButtonClicked(false));
        tapLoginButton.onClick.AddListener(() => OnStartButtonClicked(true));
        logoutButton.onClick.AddListener(OnLogoutButtonClicked);
        checkAmountButton.onClick.AddListener(OnCheckAmountButtonClicked);
        submitAmountButton.onClick.AddListener(OnSubmitAmountButtonClicked);

        charlesToggle.onValueChanged.AddListener(OnCharlesValueChanged);
        useStandaloneUIToggle.onValueChanged.AddListener(OnUseStandaloneUIValueChanged);
        rndToggle.onValueChanged.AddListener(OnRndValueChanged);
        
        regionSelectDropdown.onValueChanged.AddListener(OnRegionValueChang);
        
        openTestVietnamKycPanelButton.onClick.AddListener(OpenVietnamKycPanel);
        // 0-China;1-Vietnam
        int regionDropdownVal = 1;
        regionSelectDropdown.value = regionDropdownVal;
        // regionSelectDropdown.onValueChanged.Invoke(regionDropdownVal);
        // OnRegionValueChang(regionDropdownVal);
        OnRndValueChanged(rndToggle.isOn);
        OnCharlesValueChanged(charlesToggle.isOn);
        
        remainMinsBtn.onClick.AddListener(() =>
        {
            Debug.LogFormat($"剩余分钟: {AntiAddictionUIKit.RemainingTimeInMinutes}");
        });
        
        remainSecsBtn.onClick.AddListener(() =>
        {
            Debug.LogFormat($"剩余秒数: {AntiAddictionUIKit.RemainingTime}");
        });
        
        currentTokenBtn.onClick.AddListener(() =>
        {
            Debug.LogFormat($"Current Token: {AntiAddictionUIKit.CurrentToken}");
        });
    }

    private void OpenVietnamKycPanel()
    {
        var path = AntiAddictionSceneConst.GetPrefabPath(AntiAddictionSceneConst.TIME_SELECTOR_PANEL_NAME);
        UIManager.Instance.OpenUI<TaptapAntiAddictionTimeSelectorController>(path);
    }

    void OnStartButtonClicked(bool useTapLogin)
    {
        string userId = userIdInput.text;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }
        var gameId = _region == Region.China ? GameIdChina : GameIdVietnam;

        if (IsChinaMobile())
        {
            InitForChinaMobile();
        }
        AntiAddictionUIKit.Init(new AntiAddictionConfig()
        {
            gameId = gameId,
            useTapLogin = false,
            showSwitchAccount = true,
            region = _region,
        }, ((code, s) => Debug.LogFormat("放防沉迷回调 code: {0} 错误Msg: {1}", code, s)));
        
        OnRndValueChanged(rndToggle.isOn);
        
        AntiAddictionUIKit.Startup(userId);
    }

    void OnLogoutButtonClicked()
    {
        AntiAddictionUIKit.Exit();
        if (IsChinaMobile())
        {
            ExitForChinaMobile();
        }
        Debug.Log("登出");
    }

    void OnCheckAmountButtonClicked()
    {
        string amountStr = amountInput.text;
        if (string.IsNullOrEmpty(amountStr))
        {
            return;
        }
        var amount = long.Parse(amountStr);
        AntiAddictionUIKit.CheckPayLimit(amount, checkResult => Debug.LogFormat($"充值 status 结果: {checkResult.status}"), s => Debug.LogFormat($"检查充值异常! Error: {s}"));
    }

    void OnSubmitAmountButtonClicked()
    {
        string amountStr = amountInput.text;
        if (string.IsNullOrEmpty(amountStr))
        {
            return;
        }

        var amount = long.Parse(amountStr);
        AntiAddictionUIKit.SubmitPayResult(amount, () => Debug.Log("充值上报完成"),str => Debug.LogFormat($"上传充值异常! Error: {str}"));
    }

    void OnCharlesValueChanged(bool isOn)
    {
        ToggleCharles(isOn);
    }   
    
    void OnUseStandaloneUIValueChanged(bool isStandaloneUI)
    {
        TapTapAntiAddictionManager.useMobileUI = !isStandaloneUI;
    }

    void OnRndValueChanged(bool isOn)
    {
        OnRndEnvironmentToggle(isOn);
        SetRsaKey(isOn);
    }
    
    void OnRegionValueChang(int val)
    {
        string regionStr = regionSelectDropdown.options[val].text;
        var region = (Region)Enum.Parse(typeof(Region), regionStr);

        if (region == _region)
        {
            SetRsaKey(rndToggle.isOn);
            return;
        }
        else
        {
            TapTapAntiAddictionManager.AntiAddictionConfig.region = _region = region;
            Debug.LogFormat($"change Region String: {regionStr} _region: {_region}");
            TapTapAntiAddictionManager.AntiAddictionConfig.gameId = _region == Region.China ? GameIdChina : GameIdVietnam;
            AntiAddictionUIKit.SetRegion(_region);
            
            var networkType = typeof(Network);
            var antiAddictionHttpClientFieldInfo = networkType.GetField("HttpClient",
                BindingFlags.Static | BindingFlags.NonPublic);
            var antiAddictionHttpClient = antiAddictionHttpClientFieldInfo?.GetValue(null);
            
            var httpClientType = typeof(AntiAddictionHttpClient);
            var hostFieldInfo = httpClientType.GetField("serverUrl", BindingFlags.NonPublic | BindingFlags.Instance);
            var isRndOn = rndToggle.isOn;
            string host = isRndOn ? ServerRndUrl : (_region == Region.China ? ServerUrlCn : ServerUrlVietnam);
            hostFieldInfo?.SetValue(antiAddictionHttpClient, host);
            SetRsaKey(rndToggle.isOn);
        }
    }
    
    /// <summary>
    /// 是否开启Charles
    /// </summary>
    /// <param name="isOn"></param>
    private void ToggleCharles(bool isOn)
    {
        HttpClient client = new HttpClient();
        if (isOn)
        {
            var ip = $"http://{ipInput.text}:8888";
            charlesToggle.transform.Find("Label").GetComponent<Text>().text = ip;
            HttpClientHandler clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy(ip)
            };
            client = new HttpClient(clientHandler);
        }
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        Type networkType = typeof(Network);
        var antiAddictionHttpClientFieldInfo = networkType.GetField("HttpClient",
            BindingFlags.Static | BindingFlags.NonPublic);
        object antiAddictionHttpClient = antiAddictionHttpClientFieldInfo?.GetValue(null);

        Type httpClientType = typeof(AntiAddictionHttpClient);
        FieldInfo antiHttpClientFieldInfo = httpClientType.GetField("client", BindingFlags.NonPublic | BindingFlags.Instance);
        antiHttpClientFieldInfo?.SetValue(antiAddictionHttpClient, client);

        Type standaloneType = typeof(StandaloneChina);
        FieldInfo httpClientFieldInfo = standaloneType.GetField("client", BindingFlags.Static | BindingFlags.NonPublic);
        httpClientFieldInfo?.SetValue(null, client);
    }
    
    /// <summary>
    /// 切换测试环境
    /// </summary>
    /// <param name="isRndOn"></param>
    private void OnRndEnvironmentToggle(bool isRndOn)
    {
        #if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
        try
        {
            Plugins.AntiAddictionUIKit.AntiAddictionUIKit.SetTestMode(isRndOn);
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat($"Msg: {e.Message} \n Stack: {e.StackTrace}");
        }
        #else 
        var networkType = typeof(Network);
        var antiAddictionHttpClientFieldInfo = networkType.GetField("HttpClient",
            BindingFlags.Static | BindingFlags.NonPublic);
        var antiAddictionHttpClient = antiAddictionHttpClientFieldInfo?.GetValue(null);

        var httpClientType = typeof(AntiAddictionHttpClient);
        var hostFieldInfo = httpClientType.GetField("serverUrl", BindingFlags.NonPublic | BindingFlags.Instance);
        string host = isRndOn ? ServerRndUrl : (_region == Region.China ? ServerUrlCn : ServerUrlVietnam);
        hostFieldInfo?.SetValue(antiAddictionHttpClient, host);

        var verificationType = typeof(Verification);
        var verificationHostFieldInfo = verificationType.GetField("HOST", BindingFlags.Static | BindingFlags.NonPublic);
        string verificationHost = isRndOn ? VerificationHostRndUrl : VerificationHostUrl;
        verificationHostFieldInfo?.SetValue(null, verificationHost);

        var antiAddictionPollType = typeof(AntiAddictionPoll);
        var pollIntervalFieldInfo = antiAddictionPollType.GetField("pollInterval",
            BindingFlags.Static | BindingFlags.NonPublic);
        int interval = isRndOn ? PollIntervalRnd : PollInterval;
        pollIntervalFieldInfo?.SetValue(null, interval);
        #endif
    }

    private void SetRsaKey(bool isRndOn)
    {
        Dictionary<Region, string> dic = isRndOn ? regionRndRsaKey : regionFormalRsaKey;
        if (dic != null && dic.TryGetValue(_region, out string rsaKey) && !string.IsNullOrEmpty(rsaKey))
        {
            var networkType = typeof(TapTapAntiAddictionManager);
            var worker = networkType.GetProperty("Worker",
                BindingFlags.Static | BindingFlags.NonPublic);

            var workerInstance = worker?.GetValue(null);
            var workerType = workerInstance.GetType();
            var rsaKeyFiledInfo = workerType.GetField("rsaPublicKey", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rsaKeyFiledInfo == null)
            {
                throw new Exception("Can't find rsaKeyFiledInfo!");
            }
            rsaKeyFiledInfo.SetValue(workerInstance,rsaKey);
            
            TapLogger.Debug(string.Format($"Set region: {TapTapAntiAddictionManager.AntiAddictionConfig.region} rsaKey: {rsaKey}"));
        }
        else
        {
            TapLogger.Error(string.Format($"无法找到RSA key! region: {_region} isRnd: {isRndOn}"));
        }
    }

    #region 国内-移动版代码迁移
    private bool _isInit;
    
    private void InitForChinaMobile()
    {
        if (_isInit)
        {
            return;
        }
        try
        {
            _isInit = true;
            Debug.Log("China-Mobile - started");
            TapLogin.Init(GameIdChina);

            var config = new TapConfig.Builder()
                .ClientID(GameIdChina) // 必须，开发者中心对应 Client ID
                .ClientToken("ETndJL7LhNdbjFW7jyMWuNO9a73iF9aQhzHfFd3c") // 必须，开发者中心对应 Client Token
                .ServerURL("https://api.leancloud.cn") // 必须，开发者中心 > 你的游戏 > 游戏服务 > 基本信息 > 域名配置 > API
                .RegionType(RegionType.CN) // 非必须，CN 表示中国大陆，IO 表示其他国家或地区
                .ConfigBuilder();
            TapBootstrap.Init(config);
            
        }
        catch (Exception exception)
        {
            _isInit = false;
            Debug.LogError(exception);
        }
    }

    private void ExitForChinaMobile()
    {
        _isInit = false;
    }

    private bool IsChinaMobile()
    {
        return _region == Region.China && (Application.platform == RuntimePlatform.Android ||
                                           Application.platform == RuntimePlatform.IPhonePlayer);
    }
    #endregion
}
