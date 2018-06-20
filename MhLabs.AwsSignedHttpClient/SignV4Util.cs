using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MhLabs.AwsSignedHttpClient.Credentials;
using Microsoft.AspNetCore.WebUtilities;

namespace MhLabs.AwsSignedHttpClient
{
    public static class SignV4Util
    {
        private static readonly char[] _datePartSplitChars = { 'T' };

        private static readonly byte[] _emptyBytes = new byte[0];
        public static bool DebugLogging { get; set; }

        private static readonly UTF8Encoding _encoding = new UTF8Encoding(false);

        public static void SignRequest(HttpRequestMessage request, byte[] body, AwsCredentials credentials,
            string region, string service)
        {
            var date = DateTime.UtcNow;
            var dateStamp = date.ToString("yyyyMMdd");
            var amzDate = date.ToString("yyyyMMddTHHmmssZ");
            request.Headers.Add("X-Amz-Date", amzDate);

            var signingKey = GetSigningKey(credentials.SecretKey, dateStamp, region, service);
            var stringToSign = GetStringToSign(request, body, region, service);
            if (DebugLogging)
            {
                Debug.Write("========== String to Sign ==========\r\n{0}\r\n========== String to Sign ==========\r\n",
                    stringToSign);
            }
            var signature = signingKey.GetHmacSha256Hash(stringToSign).ToLowercaseHex();
            var auth = string.Format(
                "AWS4-HMAC-SHA256 Credential={0}/{1}, SignedHeaders={2}, Signature={3}",
                credentials.AccessKey,
                GetCredentialScope(dateStamp, region, service),
                GetSignedHeaders(request),
                signature);
            if (DebugLogging)
            {
                Console.WriteLine(auth);
            }

            request.Headers.TryAddWithoutValidation("Authorization", auth);
            if (!string.IsNullOrWhiteSpace(credentials.Token))
                request.Headers.Add("X-Amz-Security-Token", credentials.Token);

        }

        public static byte[] GetSigningKey(string secretKey, string dateStamp, string region, string service)
        {
            return _encoding.GetBytes("AWS4" + secretKey)
                .GetHmacSha256Hash(dateStamp)
                .GetHmacSha256Hash(region)
                .GetHmacSha256Hash(service)
                .GetHmacSha256Hash("aws4_request");
        }

        private static byte[] GetHmacSha256Hash(this byte[] key, string data)
        {
            using (var kha = new HMACSHA256())
            {
                kha.Key = key;
                return kha.ComputeHash(_encoding.GetBytes(data));
            }
        }

        public static string GetStringToSign(HttpRequestMessage request, byte[] data, string region, string service)
        {
            var canonicalRequest = GetCanonicalRequest(request, data);
            Debug.Write("========== Canonical Request ==========\r\n{0}\r\n========== Canonical Request ==========\r\n",
                canonicalRequest);
            var awsDate = request.Headers.GetValues("X-Amz-Date").FirstOrDefault();
            Debug.Assert(Regex.IsMatch(awsDate, @"\d{8}T\d{6}Z"));
            var datePart = awsDate.Split(_datePartSplitChars, 2)[0];
            return string.Join("\n",
                "AWS4-HMAC-SHA256",
                awsDate,
                GetCredentialScope(datePart, region, service),
                GetHash(canonicalRequest).ToLowercaseHex()
            );
        }

        private static string GetCredentialScope(string date, string region, string service)
        {
            return string.Format("{0}/{1}/{2}/aws4_request", date, region, service);
        }

        private static Dictionary<string, string> GetCanonicalHeaders(this HttpRequestMessage request)
        {
            var q = from KeyValuePair<string, IEnumerable<string>> key in request.Headers
                    let headerName = key.Key.ToLowerInvariant()
                    let headerValues = string.Join(",",
                        request.Headers
                            .GetValues(key.Key) ?? Enumerable.Empty<string>()
                            .Select(v => v.TrimAll())
                    )
                    select new { headerName, headerValues };
            var result = q.ToDictionary(v => v.headerName, v => v.headerValues);
            result["host"] = request.RequestUri.Host.ToLowerInvariant();
            return result;
        }

        public static string GetCanonicalRequest(HttpRequestMessage request, byte[] data)
        {
            var canonicalHeaders = request.GetCanonicalHeaders();
            var result = new StringBuilder();
            result.Append(request.Method);
            result.Append('\n');
            result.Append(GetPath(request.RequestUri));
            result.Append('\n');
            result.Append(request.RequestUri.GetCanonicalQueryString());
            result.Append('\n');
            WriteCanonicalHeaders(canonicalHeaders, result);
            result.Append('\n');
            WriteSignedHeaders(canonicalHeaders, result);
            result.Append('\n');
            WriteRequestPayloadHash(data, result);
            return result.ToString();
        }

        private static string GetPath(Uri uri)
        {
            var path = uri.AbsolutePath;
            if (path.Length == 0) return "/";

            var segments = path
                .Split('/')
                .Select(segment =>
                    {
                        var escaped = WebUtility.UrlEncode(segment);
                        escaped = escaped.Replace("*", "%2A");
                        return escaped;
                    }
                );
            return string.Join("/", segments);
        }


        private static void WriteCanonicalHeaders(Dictionary<string, string> canonicalHeaders, StringBuilder output)
        {
            var q = from pair in canonicalHeaders
                    orderby pair.Key
                    select string.Format("{0}:{1}\n", pair.Key, pair.Value);
            foreach (var line in q)
                output.Append(line);
        }

        private static string GetSignedHeaders(HttpRequestMessage request)
        {
            var canonicalHeaders = request.GetCanonicalHeaders();
            var result = new StringBuilder();
            WriteSignedHeaders(canonicalHeaders, result);
            return result.ToString();
        }

        private static void WriteSignedHeaders(Dictionary<string, string> canonicalHeaders, StringBuilder output)
        {
            var started = false;
            foreach (var pair in canonicalHeaders.OrderBy(v => v.Key))
            {
                if (started) output.Append(';');
                output.Append(pair.Key.ToLowerInvariant());
                started = true;
            }
        }

        private static IDictionary<string, string> ParseQueryString(string query)
        {
            return QueryHelpers.ParseQuery(query)
                .Aggregate(new Dictionary<string, string>(), (col, kv) =>
                {
                    kv.Value.ToList().ForEach(v => col.Add(kv.Key, v));
                    return col;
                });
        }

        public static string GetCanonicalQueryString(this Uri uri)
        {
            if (string.IsNullOrWhiteSpace(uri.Query)) return string.Empty;
            var queryParams = ParseQueryString(uri.Query);
            //var q = from string key in queryParams
            //    orderby key
            //    from value in queryParams[key]
            //    select new {key, value};

            var output = new StringBuilder();
            foreach (var param in queryParams)
            {
                if (output.Length > 0) output.Append('&');
                output.WriteEncoded(param.Key);
                output.Append('=');
                output.WriteEncoded(param.Value);
            }
            return output.ToString();
        }

        private static void WriteEncoded(this StringBuilder output, string value)
        {
            for (var i = 0; i < value.Length; ++i)
                if (value[i].RequiresEncoding())
                    output.Append(Uri.EscapeDataString(value[i].ToString()));
                else
                    output.Append(value[i]);
        }

        private static bool RequiresEncoding(this char value)
        {
            if ('A' <= value && value <= 'Z') return false;
            if ('a' <= value && value <= 'z') return false;
            if ('0' <= value && value <= '9') return false;
            switch (value)
            {
                case '-':
                case '_':
                case '.':
                case '~':
                    return false;
            }
            return true;
        }

        private static void WriteRequestPayloadHash(byte[] data, StringBuilder output)
        {
            data = data ?? _emptyBytes;
            var hash = GetHash(data);
            foreach (var b in hash)
                output.AppendFormat("{0:x2}", b);
        }

        private static string ToLowercaseHex(this byte[] data)
        {
            var result = new StringBuilder();
            foreach (var b in data)
                result.AppendFormat("{0:x2}", b);
            return result.ToString();
        }

        private static byte[] GetHash(string data)
        {
            return GetHash(_encoding.GetBytes(data));
        }

        private static byte[] GetHash(this byte[] data)
        {
            using (var algo = SHA256.Create())
            {
                return algo.ComputeHash(data);
            }
        }
    }
}