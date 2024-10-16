using Newtonsoft.Json;
using TapTap.AntiAddiction;
using TapTap.AntiAddiction.Internal;
using TapTap.Common;

namespace TapTap.AntiAddiction.Model 
{
    public class VerificationResult 
    {

        [JsonProperty("status")]
        public string Status { get; private set; }

        [JsonProperty("anti_addiction_token")] 
        internal string AntiAddictionToken { get; private set; }
        
        [JsonProperty("code")] 
        public int errorCode { get; private set; }
        
        [JsonProperty("error")] 
        public string error { get; private set; }
        
        [JsonProperty("error_description")] 
        public string errorDescription { get; private set; }
        
        [JsonProperty("msg")] 
        public string msg { get; private set; }
        
        internal VerificationResult() { }

        internal VerificationResult(VerificationResult other) 
        {
            TapLogger.Debug("current state = " + other.Status);
            Status = other.Status;
            AntiAddictionToken = other.AntiAddictionToken;
            errorCode = other.errorCode;
            error = other.error;
            errorDescription = other.errorDescription;
            msg = other.msg;
        }
    }


    internal class ServerVerificationResponse : BaseResponse 
    {
        [JsonProperty("data")]
        internal VerificationResult Result;
        [JsonProperty("code")]
        internal string Code { get; private set; }        
        [JsonProperty("msg")]
        internal string Message { get; private set; }
    }

    public class LocalVerification : VerificationResult
    {
        [JsonProperty("user_id")]
        internal string UserId { get; set; }

        [JsonProperty("region")] 
        internal Region Region = Region.China;

        [JsonProperty("age_limit")] 
        public int AgeLimit;

        [JsonProperty("is_adult")] 
        public bool IsAdult;

        internal LocalVerification() { }

        internal LocalVerification(VerificationResult obj) : base(obj) { }

        /// <summary>
        /// 是否已认证
        /// </summary>
        internal bool IsVerified => Status.Equals(AntiAddictionConst.VERIFICATION_STATUS_SUCCESS);        
        
        /// <summary>
        /// 是否在认证中
        /// /// </summary>
        internal bool IsVerifing => Status.Equals(AntiAddictionConst.VERIFICATION_STATUS_WAITING);
        
        /// <summary>
        /// 是否认证失败
        /// /// </summary>
        internal bool IsVerifyFailed => Status.Equals(AntiAddictionConst.VERIFICATION_STATUS_FAILED);

        internal bool CheckIsAdult => AgeLimit == Verification.AGE_LIMIT_ADULT || AgeLimit == Verification.UNKNOWN_AGE_ADULT;


    }
}
