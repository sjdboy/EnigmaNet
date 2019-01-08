using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace EnigmaNet.Extensions
{
    public static class UrlExtensions
    {
        public static string AddQueryParamIf(this string source, string key, string value, bool filter)
        {
            if (filter)
            {
                return source.AddQueryParam(key, value);
            }
            else
            {
                return source;
            }
        }

        public static string AddQueryParamIf(this string source, string key, string value, Encoding encode, bool filter)
        {
            if (filter)
            {
                return source.AddQueryParam(key, value, encode);
            }
            else
            {
                return source;
            }
        }

        public static string AddQueryParamIf(this string source, string key, string value, Func<bool> filter)
        {
            if (filter.Invoke())
            {
                return source.AddQueryParam(key, value);
            }
            else
            {
                return source;
            }
        }

        public static string AddQueryParamIf(this string source, string key, string value, Encoding encode, Func<bool> filter)
        {
            if (filter.Invoke())
            {
                return source.AddQueryParam(key, value, encode);
            }
            else
            {
                return source;
            }
        }

        public static string AddQueryParam(this string source, string key, int value, Encoding encode = null)
        {
            return source.AddQueryParam(key, value.ToString(), encode);
        }
        public static string AddQueryParam(this string source, string key, string value, Encoding encode = null)
        {
            string delim;
            if (string.IsNullOrEmpty(source) || !source.Contains("?"))
            {
                delim = "?";
            }
            else if (source.EndsWith("&"))
            {
                delim = string.Empty;
            }
            else
            {
                delim = "&";
            }

            if (encode == null)
            {
                return source + delim + HttpUtility.UrlEncode(key) + "=" + HttpUtility.UrlEncode(value);
            }
            else
            {
                return source + delim + HttpUtility.UrlEncode(key, encode) + "=" + HttpUtility.UrlEncode(value, encode);
            }
        }

        public static string SetQueryParam(this string source, string key, int value, Encoding encode = null)
        {
            return source.SetQueryParam(key, value.ToString(), encode);
        }
        public static string SetQueryParam(this string source, string key, string value, Encoding encode = null)
        {
            source = source.RemoveQueryParam(key);
            return source.AddQueryParam(key, value, encode);
        }

        public static string RemoveQueryParam(this string source, string key, Encoding encode = null)
        {
            if (string.IsNullOrEmpty(source) || !source.Contains("?") || source.EndsWith("?"))
            {
                return source;
            }

            var urlSplit = source.Split("?".ToCharArray(), StringSplitOptions.None);

            NameValueCollection query;
            if (encode == null)
            {
                query = System.Web.HttpUtility.ParseQueryString(urlSplit[1]);
            }
            else
            {
                query = System.Web.HttpUtility.ParseQueryString(urlSplit[1], encode);
            }

            if (!query.HasKeys() || query.Get(key) == null)
            {
                return source;
            }

            query.Remove(key);

            if (query.Count == 0)
            {
                return urlSplit[0];
            }

            StringBuilder sb = new StringBuilder();
            foreach (var itemKey in query.AllKeys)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                if (encode != null)
                {
                    sb.AppendFormat("{0}={1}", HttpUtility.UrlEncode(itemKey, encode), HttpUtility.UrlEncode(query[itemKey], encode));
                }
                else
                {
                    sb.AppendFormat("{0}={1}", HttpUtility.UrlEncode(itemKey), HttpUtility.UrlEncode(query[itemKey]));
                }
            }
            return urlSplit[0] + "?" + sb.ToString();
        }

        public static string RemoveEmptyQueryParam(this string source, Encoding encode = null)
        {
            if (string.IsNullOrEmpty(source) || !source.Contains("?") || source.EndsWith("?"))
            {
                return source;
            }

            var urlSplit = source.Split("?".ToCharArray(), StringSplitOptions.None);

            NameValueCollection query;
            if (encode == null)
            {
                query = System.Web.HttpUtility.ParseQueryString(urlSplit[1]);
            }
            else
            {
                query = System.Web.HttpUtility.ParseQueryString(urlSplit[1], encode);
            }

            if (!query.HasKeys())
            {
                return source;
            }

            StringBuilder sb = new StringBuilder();
            foreach (var itemKey in query.AllKeys)
            {
                var value = query[itemKey];
                if (!string.IsNullOrEmpty(value))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append("&");
                    }
                    if (encode != null)
                    {
                        sb.AppendFormat("{0}={1}", HttpUtility.UrlEncode(itemKey, encode), HttpUtility.UrlEncode(value, encode));
                    }
                    else
                    {
                        sb.AppendFormat("{0}={1}", HttpUtility.UrlEncode(itemKey), HttpUtility.UrlEncode(value));
                    }
                }
            }

            if (sb.Length > 0)
            {
                return urlSplit[0] + "?" + sb.ToString();
            }
            else
            {
                return urlSplit[0];
            }
        }
    }
}
