using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Wox.Plugin;

namespace WoxTranslate
{
    public class Main : IPlugin
    {
        /**
         * PluginInitContext context
         *      context.CurrentPluginMetadata.PluginDirectory：获取插件的安装目录
         *      context.API.ChangeQuery：改变查询关键字
         *      context.API.ShowMsg：在桌面右下角显示信息
         * Query query
         *      query.ActionKeyword：插件的触发关键字
         *      query.Search：用户输入的查询关键字
         *      query.RawQuery：触发关键字+查询关键字
         */

        private PluginInitContext _context;
        private List<string> languages = new List<string>();
        private static readonly string GOOGLE_TRANSLATE_URL = "https://translate.google.cn/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}";

        public void Init(PluginInitContext context)
        {
            this._context = context;
            this.languages = GetLanguages("languages.json");
        }

        public List<Result> Query(Query query)
        {
            var from_language = "auto";
            var to_language = "zh-CN";
            var search = query.Search.Trim();
            if (search.Contains(" "))
            {
                var index = search.IndexOf(" ");
                var sub = search.Substring(0, index);
                var pre = search.Substring(index + 1);
                if (sub.Contains(":"))
                {
                    var splits = search.Split(':');
                    if (splits.Length == 2 && this.languages.Intersect(splits).Count() == 2)
                    {
                        from_language = splits[0];
                        to_language = splits[1];
                        search = pre;
                    }
                }
                else
                {
                    if (this.languages.Contains(sub))
                    {
                        to_language = sub;
                        search = pre;
                    }
                }
            }
            var lstResult = new List<Result>();

            var translate = search;
            var isTranslate = translate.EndsWith(@"\e") || translate.EndsWith(@"\E");
            if (isTranslate)
            {
                search = search.TrimEnd('E', 'e').TrimEnd('\\');
                translate = Translate(search, to_language, from_language);
            }
            
            var result = new Result
            {
                Title = $"Google Translate: {translate}",
                SubTitle = $"Translate from {from_language} to {to_language}: {search}",
                Action = e =>
                {
                    Clipboard.SetText(translate);
                    return false;
                }
            };
            lstResult.Add(result);
            if(!isTranslate)
            {
                lstResult.Add(new Result
                {
                    Title = @"Type \E to start translate",
                    SubTitle = $"Translate from {from_language} to {to_language}: {search}",
                    Action = e =>
                    {
                        return false;
                    }
                });
            }

            return new List<Result> { result };
        }

        private List<string> GetLanguages(string fileName)
        {
            var path = Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, fileName);
            var reader = new StreamReader(path);
            var content = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<List<string>>(content);
        }

        private string Translate(string word, string to_language = "zh-CN", string from_language = "auto")
        {
            var url = string.Format(GOOGLE_TRANSLATE_URL, from_language, to_language, HttpUtility.UrlEncode(word));
            var client = new HttpClient();
            var tResponse = client.GetAsync(url);
            tResponse.Wait();
            var response = tResponse.Result;
            var tContent = response.Content.ReadAsStringAsync();
            tContent.Wait();
            var result = tContent.Result;
            var jResult = JsonConvert.DeserializeObject<JArray>(result);
            return jResult.First.First.First.Value<string>();
        }
    }
}
