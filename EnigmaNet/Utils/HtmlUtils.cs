using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EnigmaNet.Utils
{
    public static class HtmlUtils
    {
        /// <summary>
        /// 文本内容转换为html标签
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string TextToHtml(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                return text
                    .Replace("\n", "<br/>")
                    .Replace("\r",string.Empty)
                    .Replace(" ", "&nbsp;");
            }
            return text;
        }

        /// <summary>
        /// 过滤Html标签
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string FilterHtmlTag(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            //删除脚本
            html = Regex.Replace(html, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);
            //删除HTML 
            html = Regex.Replace(html, @"<(.[^>]*)>", "", RegexOptions.IgnoreCase);
            //html = Regex.Replace(html, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase);//空格不算标签，不必删除
            html = Regex.Replace(html, @"–>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<!–.*", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(lt|#60);", "<", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(nbsp|#160);", "   ", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(iexcl|#161);", "\xa1", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(cent|#162);", "\xa2", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(pound|#163);", "\xa3", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(copy|#169);", "\xa9", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&#(\d+);", "", RegexOptions.IgnoreCase);
            //html.Replace("<", "");
            //html.Replace(">", "");
            //html.Replace("\r\n", "");
            html = System.Web.HttpUtility.HtmlEncode(html);//.Trim();
            return html;
        }
    }
}
