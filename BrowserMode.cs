using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Microsoft.Win32;
using System.IO;
namespace Image2Char
{
    public class BrowserMode
    {
        /// <summary>  
        /// 修改注册表信息来兼容当前程序  
        ///   
        /// </summary>  
        public static void SetWebBrowserFeatures(int ieVersion)
        {
            // don't change the registry if running in-proc inside Visual Studio  
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime)
                return;
            //获取程序及名称  
            var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //得到浏览器的模式的值  
            UInt32 ieMode = GeoEmulationModee(ieVersion);
            var featureControlRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\";
            //设置浏览器对应用程序（appName）以什么模式（ieMode）运行  
            Registry.SetValue(featureControlRegKey + "FEATURE_BROWSER_EMULATION",
                appName, ieMode, RegistryValueKind.DWord);
            // enable the features which are "On" for the full Internet Explorer browser  
            //不晓得设置有什么用  
            Registry.SetValue(featureControlRegKey + "FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION",
                appName, 1, RegistryValueKind.DWord);


            //Registry.SetValue(featureControlRegKey + "FEATURE_AJAX_CONNECTIONEVENTS",  
            //    appName, 1, RegistryValueKind.DWord);  


            //Registry.SetValue(featureControlRegKey + "FEATURE_GPU_RENDERING",  
            //    appName, 1, RegistryValueKind.DWord);  


            //Registry.SetValue(featureControlRegKey + "FEATURE_WEBOC_DOCUMENT_ZOOM",  
            //    appName, 1, RegistryValueKind.DWord);  


            //Registry.SetValue(featureControlRegKey + "FEATURE_NINPUT_LEGACYMODE",  
            //    appName, 0, RegistryValueKind.DWord);  
        }
        /// <summary>  
        /// 获取浏览器的版本  
        /// </summary>  
        /// <returns></returns>  
        public static int GetBrowserVersion()
        {
            int browserVersion = 0;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree,
                System.Security.AccessControl.RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue("svcVersion");
                if (null == version)
                {
                    version = ieKey.GetValue("Version");
                    if (null == version)
                        throw new ApplicationException("Microsoft Internet Explorer is required!");
                }
                int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }
            //如果小于7  
            if (browserVersion < 7)
            {
                throw new ApplicationException("不支持的浏览器版本!");
            }
            return browserVersion;
        }
        /// <summary>  
        /// 通过版本得到浏览器模式的值  
        /// </summary>  
        /// <param name="browserVersion"></param>  
        /// <returns></returns>  
        public static UInt32 GeoEmulationModee(int browserVersion)
        {
            UInt32 mode = 11000; // Internet Explorer 11. Webpages containing standards-based !DOCTYPE directives are displayed in IE11 Standards mode.   
            switch (browserVersion)
            {
                case 7:
                    mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode.   
                    break;
                case 8:
                    mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode.   
                    break;
                case 9:
                    mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode.                      
                    break;
                case 10:
                    mode = 10000; // Internet Explorer 10.  
                    break;
                case 11:
                    mode = 11000; // Internet Explorer 11  
                    break;
            }
            return mode;
        }
    }

    public class HtmlClass
    {
        /// <summary>
        /// 模版文件
        /// </summary>
        public string template { set=> _template=value;}
        /// <summary>
        /// 生成的文件目录
        /// </summary>
        public string path { set=> _path = value; }
        /// <summary>
        /// 生成的文件名
        /// </summary>
        public string htmlname { set => _htmlname = value; }
        /// <summary>
        /// 字典
        /// </summary>
        public Dictionary<string, string> dic { set => _dic = value; }


        private string _template;
        private string _path;
        private string _htmlname;
        private Dictionary<string, string> _dic;

        public HtmlClass()
        {
            
        }

        /// <summary>
        /// 生成Html
        /// </summary>
        /// <param name="template">模版文件</param>
        /// <param name="path">生成的文件目录</param>
        /// <param name="htmlname">生成的文件名</param>
        /// <param name="dic">字典</param>
        /// <param name="message">异常消息</param>
        /// <returns></returns>
        public bool Create(ref string message,ref string pathshow)
        {
            bool result = false;
            //string templatepath = System.Web.HttpContext.Current.Server.MapPath(_template);
            string htmlpath = _path;
            string htmlnamepath = Path.Combine(htmlpath, _htmlname);
            pathshow = htmlnamepath;
            Encoding encode = Encoding.UTF8;
            StringBuilder html = new StringBuilder();

            try
            {
                //读取模版
                html.Append(File.ReadAllText(_template, encode));
            }
            catch (FileNotFoundException ex)
            {
                message = ex.Message;
                return false;
            }

            foreach (KeyValuePair<string, string> d in _dic)
            {
                //替换数据
                html.Replace(
                    string.Format("${0}$", d.Key),
                    d.Value);
            }

            try
            {
                //写入html文件
                if (!Directory.Exists(htmlpath))
                    Directory.CreateDirectory(htmlpath);
                File.WriteAllText(htmlnamepath, html.ToString(), encode);
                result = true;
            }
            catch (IOException ex)
            {
                message = ex.Message;
                return false;
            }
            return result;
        }
    }
}

