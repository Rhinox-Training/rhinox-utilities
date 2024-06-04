using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;
using UnityEngine.Networking;
using Utility = Rhinox.Lightspeed.Utility;

namespace Rhinox.Utilities
{
    public class ApiHelper
    {
        private string _baseUrl;

        private Dictionary<string, string> _headers;
        
        public ApiHelper(string url)
        {
            _baseUrl = url;
            _headers = new Dictionary<string, string>();
        }

        public void AddHeader(string key, string value)
        {
            _headers[key] = value;
        }
        
        public void RemoveHeader(string key, string value)
        {
            _headers.Remove(key);
        }
        
        public void ClearHeaders()
        {
            _headers.Clear();
        }

        private UnityWebRequestAsyncOperation InitAndSend(UnityWebRequest request)
        {
            foreach (var (key, value) in _headers)
                request.SetRequestHeader(key, value);

            return request.SendWebRequest();
        }
        
        private static bool CheckRequestValidity(UnityWebRequest request, ErrorCallbackAction errorCallback = null)
        {
            if (!request.IsRequestValid(out string error))
            {
                if (errorCallback == null)
                    PLog.Error<UtilityLogger>(error);

                errorCallback?.Invoke(request);
                return false;
            }
            
            return true;
        }
        
        public delegate void WebRequestAction(UnityWebRequest request);
        public delegate void ErrorCallbackAction(UnityWebRequest request);
        
        // ================================================================================
        // GET

        public async Task Get(string path, WebRequestAction handleRequest)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                await InitAndSend(request);
                handleRequest?.Invoke(request);
            }
        }

        public async Task<T> Get<T>(string path, ErrorCallbackAction errorCallback = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                await InitAndSend(request);

                if (CheckRequestValidity(request, errorCallback))
                    return request.ParseJsonResult<T>(true);
                else
                    return default;
            }
        }

        public T GetSync<T>(string path, ErrorCallbackAction errorCallback = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                var op = InitAndSend(request);
                while (!op.isDone) { }

                if (CheckRequestValidity(request, errorCallback))
                    return request.ParseJsonResult<T>(true);
                else
                    return default;
            }
        }
        
        // ================================================================================
        // DELETE
        
        public async Task Delete(string path, ErrorCallbackAction errorCallback = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Delete(uri))
            {
                await InitAndSend(request);

                //no need to deal with response as it is a delete
                CheckRequestValidity(request, errorCallback);
            }
        }
        
        public void DeleteSync<T>(string path)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Delete(uri))
            {
                // Request and wait for the desired page.
                var op = InitAndSend(request);
                while (!op.isDone) { }
                
                //no need to deal with response as it is a delete
                CheckRequestValidity(request);
            }
        }
        
        // ================================================================================
        // POST

        public async Task Post(string path, string json, WebRequestAction handleRequest = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                await InitAndSend(request);
                handleRequest?.Invoke(request);
            }
        }
        
        public async Task Post(string path, WWWForm form, WebRequestAction handleRequest = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Post(uri, form))
            {
                // Request and wait for the desired page.
                await InitAndSend(request);
                handleRequest?.Invoke(request);
            }
        }
        
        public async Task Post(string path, object o, WebRequestAction handleRequest = null)
            => await Post(path, Utility.ToJson(o, true), handleRequest);

        public async Task<T> Post<T>(string path, string json, ErrorCallbackAction errorCallback = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                await InitAndSend(request);

                if(CheckRequestValidity(request, errorCallback))
                    return request.ParseJsonResult<T>(true);
                else
                    return default;
            }
        }

        public async Task<TResult> Post<TResult>(string path, object o, ErrorCallbackAction errorCallback = null)
            => await Post<TResult>(path, Utility.ToJson(o, true), errorCallback);

        public TResult PostSync<TResult>(string path, string json)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                var op = InitAndSend(request);
                while (!op.isDone) { }

                if (CheckRequestValidity(request))
                    return request.ParseJsonResult<TResult>(true);
                else
                    return default;
            }
        }
        
        public TResult PostSync<TResult>(string path, object o, ErrorCallbackAction errorCallback = null)
            => PostSync<TResult>(path, Utility.ToJson(o, true), errorCallback);
        
        
        // ================================================================================
        // PUT
        
        public async Task Put(string path, string json, WebRequestAction handleRequest = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Put(uri, json))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                await InitAndSend(request);
                handleRequest?.Invoke(request);
            }
        }
        
        public async Task Put(string path, WWWForm form, WebRequestAction handleRequest = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Put(uri, form.data))
            {
                foreach ((string name, string value) in form.headers)
                    request.SetRequestHeader(name, value);
                
                // Request and wait for the desired page.
                await InitAndSend(request);
                handleRequest?.Invoke(request);
            }
        }
        
        public async Task Put(string path, object o, WebRequestAction handleRequest = null)
            => await Put(path, Utility.ToJson(o, true), handleRequest);

        public async Task<T> Put<T>(string path, string json, ErrorCallbackAction errorCallback = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Put(uri, json))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                await InitAndSend(request);

                if (CheckRequestValidity(request, errorCallback))
                    return request.ParseJsonResult<T>(true);
                else
                    return default;
            }
        }

        public async Task<TResult> Put<TResult>(string path, object o, ErrorCallbackAction errorCallback = null)
            => await Put<TResult>(path, Utility.ToJson(o, true), errorCallback);

        public TResult PutSync<TResult>(string path, string json, ErrorCallbackAction errorCallback = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Put(uri, json))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                var op = InitAndSend(request);
                while (!op.isDone) { }

                if (CheckRequestValidity(request, errorCallback))
                    return request.ParseJsonResult<TResult>(true);
                else
                    return default;
            }
        }
        
        public TResult PutSync<TResult>(string path, object o)
            => PutSync<TResult>(path, Utility.ToJson(o, true));
    }
}