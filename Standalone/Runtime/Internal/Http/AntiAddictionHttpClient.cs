﻿using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LC.Newtonsoft.Json;
using TapTap.Common;
using TapTap.Common.Internal.Json;
using TapTap.Common.Internal.Http;
using TapTap.AntiAddiction.Model;

namespace TapTap.AntiAddiction.Internal.Http {
    public class AntiAddictionHttpClient {
        static readonly int INTERNAL_SERVER_ERROR_LIMIT = 3;

        internal readonly string serverUrl;

        private readonly HttpClient client;

        private readonly Dictionary<string, Func<Task<string>>> runtimeHeaderTasks = new Dictionary<string, Func<Task<string>>>();

        private readonly Dictionary<string, string> additionalHeaders = new Dictionary<string, string>();



        public AntiAddictionHttpClient(string serverUrl) {
            this.serverUrl = serverUrl;
            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void AddRuntimeHeaderTask(string key, Func<Task<string>> task) {
            if (string.IsNullOrEmpty(key)) {
                return;
            }
            if (task == null) {
                return;
            }
            runtimeHeaderTasks[key] = task;
        }

        public void ChangeAddtionalHeader(string key, string value) {
            if (string.IsNullOrEmpty(key)) {
                return;
            }

            if (string.IsNullOrEmpty(value))
            {
                if (additionalHeaders.ContainsKey(key))
                    additionalHeaders.Remove(key);
            }
            else
            {
                additionalHeaders[key] = value;
            }
        }

        public Task<T> Get<T>(string path,
            Dictionary<string, object> headers = null,
            Dictionary<string, object> queryParams = null) {
            return Request<T>(path, HttpMethod.Get, headers, null, queryParams);
        }

        public Task<T> Post<T>(string path,
            Dictionary<string, object> headers = null,
            object data = null,
            Dictionary<string, object> queryParams = null) {
            return Request<T>(path, HttpMethod.Post, headers, data, queryParams);
        }

        public Task<T> Put<T>(string path,
            Dictionary<string, object> headers = null,
            object data = null,
            Dictionary<string, object> queryParams = null) {
            return Request<T>(path, HttpMethod.Put, headers, data, queryParams);
        }

        public Task Delete(string path,
            Dictionary<string, object> headers = null,
            object data = null,
            Dictionary<string, object> queryParams = null) {
            return Request<Dictionary<string, object>>(path, HttpMethod.Delete, headers, data, queryParams);
        }

        async Task<T> Request<T>(string path,
            HttpMethod method,
            Dictionary<string, object> headers = null,
            object data = null,
            Dictionary<string, object> queryParams = null) {

            int retryTimes = 3;
            HttpStatusCode serverErrorStatusCode = HttpStatusCode.InternalServerError;
            string serverErrorString = "";
            while(retryTimes > 0){
                string url = BuildUrl(path, queryParams);
                HttpRequestMessage request = new HttpRequestMessage {
                    RequestUri = new Uri(url),
                    Method = method,
                };
                await FillHeaders(request.Headers, headers);

                string content = null;
                if (data != null) {
                    content = JsonConvert.SerializeObject(data);
                    StringContent requestContent = new StringContent(content);
                    requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    request.Content = requestContent;
                }
                TapHttpUtils.PrintRequest(client, request, content);
           
                HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                request.Dispose();

                string resultString = await response.Content.ReadAsStringAsync();
                response.Dispose();
                TapHttpUtils.PrintResponse(response, resultString);

                if (response.IsSuccessStatusCode)
                {
                    T ret = JsonConvert.DeserializeObject<T>(resultString,
                        TapJsonConverter.Default);
                    if (ret is BaseResponse baseResponse && !baseResponse.Success)
                    {
                        throw HandleErrorResponse(response.StatusCode, resultString);
                    }
                    return ret;
                }
                TapLogger.Debug("http log response code = " + response.StatusCode + " result = " + resultString);
                if(response.StatusCode >= HttpStatusCode.InternalServerError){
                    retryTimes--;
                    serverErrorStatusCode = response.StatusCode;
                    serverErrorString = resultString;
                }else{
                    throw HandleErrorResponse(response.StatusCode, resultString);
                }
            }
            throw HandleErrorResponse(serverErrorStatusCode, serverErrorString);
        }

        Exception HandleErrorResponse(HttpStatusCode statusCode, string responseContent) 
        {
            
            if (statusCode >= HttpStatusCode.InternalServerError) {
                return new AntiAddictionException((int)statusCode, responseContent);
            }

            int code = (int)statusCode;
            string message = responseContent;
            string err = null;
            string desc = null;
            long timestamp = 0;
            long errorCode = -1;
            try {
                // 尝试获取 LeanCloud 返回错误信息
                ErrorResponse error = JsonConvert.DeserializeObject<ErrorResponse>(responseContent,
                    TapJsonConverter.Default);
                message = error.Result.Message;
                err = error.Result.Error;
                desc = error.Result.Description;
                timestamp = error.Now;
                errorCode = error.Result.ErrorCode;
            } catch (Exception e) {
                TapLogger.Error(e);
            }
            return new AntiAddictionException(code, message) {
                Error = err,
                Description = desc,
                Now = timestamp,
                ErrorCode = errorCode
            };
        }

        string BuildUrl(string path, Dictionary<string, object> queryParams) {
            string apiServer = serverUrl;
            StringBuilder urlSB = new StringBuilder(apiServer.TrimEnd('/'));
            urlSB.Append($"/{path}");
            string url = urlSB.ToString();
            if (queryParams != null) {
                IEnumerable<string> queryPairs = queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value.ToString())}");
                string queries = string.Join("&", queryPairs);
                url = $"{url}?{queries}";
            }
            return url;
        }

        async Task FillHeaders(HttpRequestHeaders headers, Dictionary<string, object> reqHeaders = null) {
            // 额外 headers
            if (reqHeaders != null) {
                foreach (KeyValuePair<string, object> kv in reqHeaders) {
                    headers.Add(kv.Key, kv.Value.ToString());
                }
            }
            if (additionalHeaders.Count > 0) {
                foreach (KeyValuePair<string, string> kv in additionalHeaders) {
                    headers.Add(kv.Key, kv.Value);
                }
            }
            // 服务额外 headers
            foreach (KeyValuePair<string, Func<Task<string>>> kv in runtimeHeaderTasks) {
                if (headers.Contains(kv.Key)) {
                    continue;
                }
                string value = await kv.Value.Invoke();
                if (string.IsNullOrEmpty(value)) {
                    continue;
                }
                headers.Add(kv.Key, value);
            }
        }

    }
}
