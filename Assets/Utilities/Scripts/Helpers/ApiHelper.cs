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
        
        public delegate void WebRequestAction(UnityWebRequest request);
        
        // ================================================================================
        // GET

        public async Task Get(string path, WebRequestAction handleRequest)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                await InitAndSend(request);
                
                if (request.IsRequestValid(out string error))
                    handleRequest?.Invoke(request);
                else
                    PLog.Error<UtilityLogger>(error);
            }
        }
        
        public async Task<T> Get<T>(string path)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                await InitAndSend(request);

                if (request.IsRequestValid(out string error))
                {
                    T result = request.ParseJsonResult<T>(true);
                    return result;
                }
                else
                    PLog.Error<UtilityLogger>(error);
            }

            return default;
        }

        public IEnumerator Get<T>(string path, Action<T> callback)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return InitAndSend(request);
                
                if (request.IsRequestValid(out string error))
                {
                    T result = request.ParseJsonResult<T>(true);
                    callback?.Invoke(result);
                }
                else
                    PLog.Error<UtilityLogger>(error);
            }
        }

        public T GetSync<T>(string path)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                var op = InitAndSend(request);
                while (!op.isDone) { }

                return request.ParseJsonResult<T>(true);
            }
        }
        
        // ================================================================================
        // DELETE
        
        public async Task Delete(string path)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Delete(uri))
            {
                await InitAndSend(request);

                if (!request.IsRequestValid(out string error))
                {
                    PLog.Error<UtilityLogger>(error);
                }
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

                if (request.IsRequestValid(out string error))
                    handleRequest?.Invoke(request);
                else
                {
                    PLog.Error<UtilityLogger>(error);
                    handleRequest?.Invoke(request);
                }
            }
        }
        
        public async Task Post(string path, WWWForm form, WebRequestAction handleRequest = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Post(uri, form))
            {
                // Request and wait for the desired page.
                await InitAndSend(request);

                if (request.IsRequestValid(out string error))
                    handleRequest?.Invoke(request);
                else
                {
                    PLog.Error<UtilityLogger>(error);
                    handleRequest?.Invoke(request);
                }
            }
        }
        
        public async Task Post(string path, object o, WebRequestAction handleRequest = null)
            => await Post(path, Utility.ToJson(o, true), handleRequest);

        public async Task<T> Post<T>(string path, string json)
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

                return request.ParseJsonResult<T>(true);
            }
        }

        public async Task<TResult> Post<TResult>(string path, object o)
            => await Post<TResult>(path, Utility.ToJson(o, true));

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

                return request.ParseJsonResult<TResult>(true);
            }
        }
        
        public TResult PostSync<TResult>(string path, object o)
            => PostSync<TResult>(path, Utility.ToJson(o, true));
        
        
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

                if (request.IsRequestValid(out string error))
                    handleRequest?.Invoke(request);
                else
                {
                    PLog.Error<UtilityLogger>(error);
                    handleRequest?.Invoke(request);
                }
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

                if (request.IsRequestValid(out string error))
                    handleRequest?.Invoke(request);
                else
                {
                    PLog.Error<UtilityLogger>(error);
                    handleRequest?.Invoke(request);
                }
            }
        }
        
        public async Task Put(string path, object o, WebRequestAction handleRequest = null)
            => await Put(path, Utility.ToJson(o, true), handleRequest);

        public async Task<T> Put<T>(string path, string json)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Put(uri, json))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                await InitAndSend(request);

                return request.ParseJsonResult<T>(true);
            }
        }

        public async Task<TResult> Put<TResult>(string path, object o)
            => await Put<TResult>(path, Utility.ToJson(o, true));

        public TResult PutSync<TResult>(string path, string json)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Put(uri, json))
            {
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                var op = InitAndSend(request);
                while (!op.isDone) { }

                return request.ParseJsonResult<TResult>(true);
            }
        }
        
        public TResult PutSync<TResult>(string path, object o)
            => PutSync<TResult>(path, Utility.ToJson(o, true));

    }
}