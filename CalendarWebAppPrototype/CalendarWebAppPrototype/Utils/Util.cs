﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net.Http.Formatting;
using System.Dynamic;


namespace CalendarWebAppPrototype.Utils
{
    static class Util
    {
        public const string baseUri = @"https://outlook.office365.com/api/beta/Me/";

        internal static async Task<T> GetItemAsync<T>(string uri)
        {
            return await DoHttp<T, T>(HttpMethod.Get, uri, default(T));
        }

        internal static async Task<T> GetItemsAsync<T>(string uri)
        {
            var coll = await DoHttp<ODataCollection<T>, ODataCollection<T>>(HttpMethod.Get, uri, default(ODataCollection<T>));

            return coll.value;
        }

        internal static async Task<T> PostItemAsync<T>(string uri, T item = default(T))
        {
            return await DoHttp<T, T>(HttpMethod.Post, uri, item);
        }

        internal static async Task<T> PostDynamicAsync<T>(string uri, dynamic body)
        {
            return await DoHttp<ExpandoObject, T>(HttpMethod.Post, uri, body);
        }

        internal static async Task DeleteAsync(string uri)
        {
            using (HttpClient client = await GetHttpClient())
            {
                var response = await client.DeleteAsync(BuildUri(uri));

                response.EnsureSuccessStatusCode();
            }
        }

        internal static async Task<T> PatchItemAsync<T>(string uri, T item)
        {
            return await DoHttp<T, T>("PATCH", uri, item);
        }

        private static async Task<TResult> DoHttp<TBody, TResult>(HttpMethod method, string uri, TBody body)
        {
            HttpResponseMessage response;

            using (HttpClient client = await GetHttpClient())
            {
                var request = new HttpRequestMessage(method, BuildUri(uri));

                if (EqualityComparer<TBody>.Default.Equals(body, default(TBody)) == false)
                {
                    request.Content = new ObjectContent<TBody>(body, new JsonMediaTypeFormatter());
                }

                response = await client.SendAsync(request);
            }

            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResult>(jsonResponse);
        }

        internal static async Task<TResult> DoHttp<TBody, TResult>(string methodName, string uri, TBody body)
        {
            return await DoHttp<TBody, TResult>(new HttpMethod(methodName), uri, body);
        }

        private static string BuildUri(string subUri)
        {
            if (subUri.StartsWith("http"))
            {
                return subUri;
            }

            return baseUri + subUri;
        }

        private static async Task<HttpClient>GetHttpClient()
        {
            HttpClient client = new HttpClient();

            string token = await AuthenticationHelper.GetOutlookAccessTokenAsync();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        private class ODataCollection<TCollection>
        {
            public TCollection value { get; set; }
        }

     }
}
