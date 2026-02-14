using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    public static class HttpModule
    {
        private static readonly HttpClient _client = new HttpClient();

        public static ModuleVal Build()
        {
            var m = new Dictionary<string, Value>();

            // Simple GET request
            m["get"] = new ClosureVal(url =>
            {
                try
                {
                    var urlStr = ((StringVal)url).Value;
                    var response = _client.GetStringAsync(urlStr).Result;
                    return new StringVal(response);
                }
                catch (Exception ex)
                {
                    throw new AttoException(new AdtVal("Http",
                        new StringVal($"GET failed: {ex.Message}")));
                }
            });

            // POST with body
            m["post"] = Curry2((url, body) =>
            {
                try
                {
                    var urlStr = ((StringVal)url).Value;
                    var bodyStr = ((StringVal)body).Value;
                    var content = new StringContent(bodyStr, Encoding.UTF8, "text/plain");
                    var response = _client.PostAsync(urlStr, content).Result;
                    return new StringVal(response.Content.ReadAsStringAsync().Result);
                }
                catch (Exception ex)
                {
                    throw new AttoException(new AdtVal("Http",
                        new StringVal($"POST failed: {ex.Message}")));
                }
            });

            // POST JSON
            m["postJson"] = Curry2((url, json) =>
            {
                try
                {
                    var urlStr = ((StringVal)url).Value;
                    var jsonStr = ((StringVal)json).Value;
                    var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
                    var response = _client.PostAsync(urlStr, content).Result;
                    return new StringVal(response.Content.ReadAsStringAsync().Result);
                }
                catch (Exception ex)
                {
                    throw new AttoException(new AdtVal("Http",
                        new StringVal($"POST JSON failed: {ex.Message}")));
                }
            });

            // GET with headers (headers as list of tuples)
            m["getWithHeaders"] = Curry2((url, headers) =>
            {
                try
                {
                    var urlStr = ((StringVal)url).Value;
                    var request = new HttpRequestMessage(HttpMethod.Get, urlStr);

                    // Parse headers list
                    var headersList = (ListVal)headers;
                    foreach (var item in headersList.Items)
                    {
                        var tuple = (TupleVal)item;
                        var key = ((StringVal)tuple.Items[0]).Value;
                        var value = ((StringVal)tuple.Items[1]).Value;
                        request.Headers.Add(key, value);
                    }

                    var response = _client.SendAsync(request).Result;
                    return new StringVal(response.Content.ReadAsStringAsync().Result);
                }
                catch (Exception ex)
                {
                    throw new AttoException(new AdtVal("Http",
                        new StringVal($"GET with headers failed: {ex.Message}")));
                }
            });

            return new ModuleVal(m);
        }

        private static Value Curry2(Func<Value, Value, Value> f)
        {
            return new ClosureVal(a => new ClosureVal(b => f(a, b)));
        }
    }
}
