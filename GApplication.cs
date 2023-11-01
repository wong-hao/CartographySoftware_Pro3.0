using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Mapping;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Windows.Input;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Internal.Mapping;
using log4net.Config;
using log4net;
using System.Windows;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace SMGI_Common
{
    /// <summary>
    /// 存放全局变量
    /// </summary>
    public class GApplication
    {
        #region 单例模式
        private static readonly Lazy<GApplication> lazy = new Lazy<GApplication>(() => new GApplication());
        public static GApplication Application { get { return lazy.Value; } }

        public GApplication()
        {

        }
        #endregion
        //  public static bool Register = CheckRegistration();

        #region 注册验证
        static string[] cc = { "" };
        static DateTime dtStart;
        static DateTime dtStop;

        private static bool viii(string name)
        {
            try
            {
                bool a = cc.Contains(name);
                a = a & DateTime.Now > dtStart;
                a = a & DateTime.Now < dtStop;
                return a;
            }
            catch
            {
                return false;
            }
        }

        internal event Action Clicked;

        public static bool CheckRegistration(string productNmae)
        {
            #region check
            if (!viii(productNmae))
            {
                try
                {
                    if (RegistryHelper.IsRegistryExist(Registry.LocalMachine, "SOFTWARE", "SMGI"))
                    {
                        RegistryKey rsg = Registry.LocalMachine.OpenSubKey("SOFTWARE\\SMGI", true); //true表可修改
                        if (rsg.GetValue("SMGI") != null)  //如果值不为空
                        {
                            var rv = rsg.GetValue("SMGI");
                            StringReader sr = new StringReader(rv.ToString());
                            List<string> regValue = new List<string>();
                            regValue.Add(sr.ReadLine());
                            regValue.Add(sr.ReadLine());
                            regValue.Add(sr.ReadLine());
                            regValue.Add(sr.ReadLine());
                            sr.Dispose();
                            //string[] regValue = rv.Split('\n','\r');
                            if (regValue.Count == 4)
                            {
                                //读取值
                                string sysMachineName;
                                BinaryFormatter formatter = new BinaryFormatter();
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    formatter.Serialize(ms, Environment.MachineName);
                                    sysMachineName = Convert.ToBase64String(ms.ToArray());
                                }

                                string mN = sysMachineName;
                                //string mN = SoftRegister.CalculateSeialNum(origNum);
                                string k = regValue[0];
                                StringBuilder sb = new StringBuilder();
                                dtStart = DateTime.Parse(regValue[1]);
                                dtStop = DateTime.Parse(regValue[2]);
                                string product = regValue[3];
                                cc = product.Split(new char[] { ';' });

                                sb.AppendLine(regValue[1]);
                                sb.AppendLine(regValue[2]);
                                sb.AppendLine(product);
                                string info = mN + sb.ToString();

                                bool a = false;
                                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                                {
                                    RSA.ImportCspBlob(pubKey);

                                    a = RSA.VerifyData(System.Text.Encoding.Unicode.GetBytes(info), new SHA1CryptoServiceProvider(), Convert.FromBase64String(k));
                                }
                                if (!a)
                                {
                                    cc = null;
                                }
                                rsg.Close();
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            if (viii(productNmae))
            {
                return true;
            }
            else
            {
                RegFrom reg = new RegFrom();
                reg.ShowDialog();
                return false;
            }
            #endregion
        }

        public static string Product
        {
            get
            {
                return "EmergencyMap";
            }
        }

        static byte[] pubKey = {6,2,0,0,0,164,0,0,82,83,
                                65,49,0,4,0,0,1,0,1,0,
                                193,32,12,206,92,114,242,214,168,189,
                                136,34,82,178,118,254,229,85,15,40,
                                122,59,60,204,198,213,13,72,131,139,
                                28,126,217,191,53,143,138,112,171,153,
                                89,170,117,225,136,76,39,191,88,200,
                                192,77,26,101,132,181,209,42,109,229,
                                105,63,11,9,52,211,225,96,147,161,
                                83,97,73,63,16,78,236,79,113,108,
                                134,106,41,114,49,222,247,141,170,234,
                                197,90,55,139,26,95,61,32,196,139,
                                146,60,215,21,88,84,83,111,29,218,
                                203,105,3,216,76,119,254,161,223,40,
                                109,6,163,206,181,188,105,176};


        public static void CheckReg(string productNmae)
        {
            #region check
            if (!viii(productNmae))
            {
                try
                {
                    if (RegistryHelper.IsRegistryExist(Registry.LocalMachine, "SOFTWARE", "SMGI"))
                    {
                        RegistryKey rsg = Registry.LocalMachine.OpenSubKey("SOFTWARE\\SMGI", true); //true表可修改
                        if (rsg.GetValue("SMGI") != null)  //如果值不为空
                        {
                            string rv = rsg.GetValue("SMGI").ToString();
                            StringReader sr = new StringReader(rv);
                            List<string> regValue = new List<string>();
                            regValue.Add(sr.ReadLine());
                            regValue.Add(sr.ReadLine());
                            regValue.Add(sr.ReadLine());
                            regValue.Add(sr.ReadLine());
                            sr.Dispose();
                            //string[] regValue = rv.Split('\n','\r');
                            if (regValue.Count == 4)
                            {
                                //读取值
                                string sysMachineName;
                                BinaryFormatter formatter = new BinaryFormatter();
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    formatter.Serialize(ms, System.Environment.MachineName);
                                    sysMachineName = Convert.ToBase64String(ms.ToArray());
                                }

                                string mN = sysMachineName;
                                //string mN = SoftRegister.CalculateSeialNum(origNum);
                                string k = regValue[0];
                                StringBuilder sb = new StringBuilder();
                                dtStart = DateTime.Parse(regValue[1]);
                                dtStop = DateTime.Parse(regValue[2]);
                                string product = regValue[3];
                                cc = product.Split(new char[] { ';' });

                                sb.AppendLine(regValue[1]);
                                sb.AppendLine(regValue[2]);
                                sb.AppendLine(product);
                                string info = mN + sb.ToString();

                                bool a = false;
                                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                                {
                                    RSA.ImportCspBlob(pubKey);

                                    a = RSA.VerifyData(System.Text.Encoding.Unicode.GetBytes(info), new SHA1CryptoServiceProvider(), Convert.FromBase64String(k));
                                }
                                if (!a)
                                {
                                    cc = null;
                                }
                                rsg.Close();
                            }
                        }
                    }

                }
                catch
                {
                }
            }
            #endregion
            if (viii(Product))
            {
                try
                {

                }
                catch (Exception ex)
                {
                    MessageBox.Show($@"Error: {ex}");

                }
            }
            else
            {
                RegFrom reg = new RegFrom();
                reg.ShowDialog();
            }
        }


        #endregion

        public Dictionary<string, bool> CmdsUID = new Dictionary<string, bool>();

        //编辑
        public double ReferenceScale = 10000;
        //处理窗体信息
        public string MsgForm = string.Empty;
        public delegate bool LayerChecker(Layer info);

        private MapView mapView;
        public MapView MapView
        {
            get => mapView;
        }

        private Map map;
        public Map Map
        {
            get => mapView.Map;
        }

        public string Caption
        {
            get
            {
                XDocument doc = XDocument.Load(GApplication.Application.ResourcePath + @"\Resources\Template\Template.xml");
                return doc.Element("Template").Element("ClassName").Value;
            }
        }

        public string TargetProjectPath
        {
            get
            {
                XDocument doc = XDocument.Load(GApplication.Application.ResourcePath + @"\Resources\Template\Template.xml");
                string captionName = doc.Element("Template").Element("ClassName").Value;
                return GApplication.Application.ResourcePath + @"\Resources\Template\" + captionName;
            }
        }

        public string ExePath
        {
            get
            {
                string codeBase = System.Reflection.Assembly.GetExecutingAssembly().Location;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return System.IO.Path.GetDirectoryName(path); ;
            }
        }

        public string ResourcePath
        {
            get
            {
                string codeBase = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string assemblyCacheFolder = Path.GetDirectoryName(new Uri(codeBase).LocalPath);
                return assemblyCacheFolder;
            }
        }

        /// <summary>
        /// 图层数据字典匹配：英文对中文
        /// </summary>
        public static Dictionary<string, string> LayersMappingENtoCH
        {
            get
            {
                var keyValuePairs = new Dictionary<string, string>()
                {
                    { "LANNO",  "整饰注记"} , { "LPOINT",  "图例花边"} , { "LLINE",  "图廓裁切线"} , { "LPOLY",  "挡白"} ,  { "ANNO",  "注记"} , { "GRID",  "格网线"} , { "AGNP",  "居民地点"} , { "POI",  "自然地名"} ,
                    { "LFCP",  "交通附属点"} , { "HYDP",  "水系点"} , { "HFCP",  "水系附属点"} , { "JJTH",  "境界跳绘线"} , { "BOUL",  "境界线"} ,  { "AANL",  "地貌线"} , { "BRGL",  "自然保护区线"} , { "BRGA",  "自然保护区面"} ,
                    { "LFCL",  "交通附属线"} ,{ "LRRL",  "铁路"} , { "LRDL",  "公路"} , { "HFCL",  "水系附属线"} , { "HYDATOLINE",  "水系面边线"} , { "HYDL",  "水系线"} , { "HFCA",  "水系附属面"} , { "CJL",  "侧界"} , { "SDM",  "色带面"} ,
                    { "QJL",  "骑界线"} ,{ "HYDA",  "水系面"} , { "RESA",  "居民地面"} , { "QJA",  "骑界面"} ,  { "BOUA6",  "乡镇级行政区面"} , { "BOUA5",  "县级行政区面"} , { "BOUA4",  "地级行政区面"} , { "BOUA2",  "省级行政区面"} ,{ "ClipBoundary",  "纸张[不打开]"},{ "底图",  "底图"}
                };
                return keyValuePairs;
            }
        }

        /// <summary>
        ///  图层数据字典匹配：中午对英文
        /// </summary>
        public static Dictionary<string, string> LayersMappingCHtoEN
        {
            get
            {
                var keyValuePairs = new Dictionary<string, string>()
                {
                    { "整饰注记",  "LANNO"} , { "图例花边",  "LPOINT"} , { "图廓裁切线",  "LLINE"} , { "挡白",  "LPOLY"} ,  { "注记",  "ANNO"} , { "格网线",  "GRID"} , { "居民地点",  "AGNP"} , { "自然地名",  "POI"} ,
                    { "交通附属点",  "LFCP"} , { "水系点",  "HYDP"} , { "水系附属点",  "HFCP"} , { "境界跳绘线",  "JJTH"} , { "境界线",  "BOUL"} ,  { "地貌线",  "AANL"} , { "自然保护区线",  "BRGL"} , { "自然保护区面",  "BRGA"} ,
                    { "交通附属线",  "LFCL"} ,{ "铁路",  "LRRL"} , { "公路",  "LRDL"} , { "水系附属线",  "HFCL"} , { "水系面边线",  "HYDATOLINE"} , { "水系线",  "HYDL"} , { "水系附属面",  "HFCA"} , { "侧界",  "CJL"} , { "色带面",  "SDM"} ,
                    { "骑界线",  "QJL"} ,{ "水系面",  "HYDA"} , { "居民地面",  "RESA"} , { "骑界面",  "QJA"} ,  { "乡镇级行政区面",  "BOUA6"} , { "县级行政区面",  "BOUA5"} , { "地级行政区面",  "BOUA4"} , { "省级行政区面",  "BOUA2"} ,{ "纸张[不打开]",  "ClipBoundary"} ,{ "底图",  "底图"}
                };
                return keyValuePairs;
            }
        }

        Thread thread;
        internal event Action<GApplication> abort;
        internal event Action<string> waitText;
        internal event Action<int> maxValue;
        internal event Action<int> step;

        public WaitOperation SetBusy()
        {
            bool inited = false;
            ThreadStart start = () =>
            {
                //WaitForm wait = new WaitForm(this);
                //inited = true;
                //wait.ShowDialog();
            };
            thread = new Thread(start);
            thread.Start();
            WaitOperation wo = new WaitOperation();
            wo.SetText = (v) =>
            {
                if (waitText != null)
                    waitText(v);
            };
            wo.SetMaxValue = (v) =>
            {
                if (maxValue != null)
                    maxValue(v);
            };
            wo.Step = (v) =>
            {
                if (step != null)
                {
                    step(v);
                }
            };
            wo.dispose = (x) =>
            {
                if (abort != null)
                    abort(this);

            };
            return wo;
        }

        public static string GetAppDataPath()
        {
            var dp = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var di = new DirectoryInfo(dp);
            var ds = di.GetDirectories("SMGI");
            if (ds == null || ds.Length == 0)
            {
                var sdi = di.CreateSubdirectory("SMGI");
                return sdi.FullName;
            }
            else
            {
                return ds[0].FullName;
            }
        }

        #region 日志相关（NuGet包管理器安装log4net库2.0.15版本）

        private static ILog sysLog { get; set; } // 系统日志
        private static ILog dataLog { get; set; } // 数据日志

        // 日志消息类型
        public const string INFO = "INFO";
        public const string WARN = "WARN";
        public const string FATAL = "FATAL";
        public const string ERROR = "ERROR";

        public static void loadLog(string fileLocation, bool storageType)
        {
            try
            {
                // 判断是系统日志还是数据日志
                string storageLocation = string.Empty;

                // 时间戳
                string fixedIdentifier = string.Empty;

                // log4net配置文件路径
                string configFilePath = GetAppDataPath() + "\\log4net.config";

                #region 动态设置日志文件存储路径

                if (storageType)
                {
                    storageLocation = GetAppDataPath() + "\\log\\";

                    if (!File.Exists(storageLocation))
                    {
                        Directory.CreateDirectory(storageLocation);
                    }

                    GlobalContext.Properties["SysStorageLocation"] = storageLocation;

                }
                else
                {
                    storageLocation = fileLocation + "\\log\\";

                    if (!File.Exists(storageLocation))
                    {
                        Directory.CreateDirectory(storageLocation);
                    }

                    GlobalContext.Properties["DataStorageLocation"] = storageLocation;

                }

                fixedIdentifier = DateTimeOffset.Now.ToString("yyyy_MM_dd_HH_mm_ss");
                GlobalContext.Properties["FixedIdentifier"] = fixedIdentifier;

                #endregion

                #region 启用配置文件

                if (storageType)
                {
                    sysLog = LogManager.GetLogger("SysRollingFile");
                }
                else
                {
                    dataLog = LogManager.GetLogger("DataRollingFile");
                }

                if (!File.Exists(configFilePath))
                {
                    MessageBox.Show("日志配置文件" + configFilePath + "日志问题", "错误提示", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                XmlConfigurator.ConfigureAndWatch(new FileInfo(configFilePath));

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "日志问题", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public static void InitializeLog()
        {
            if (sysLog != null && dataLog != null)
            {
                return;
            }

            // 加载系统日志
            loadLog("", true);
            // 加载数据日志
            loadLog(Project.Current.HomeFolderPath, false);
        }

        public static void writeLog(string message, string messageType, bool storageType, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                string filename = Path.GetFileName(filePath);
                message = message + " [" + filename + " : 行号" + lineNumber + "]";

                switch (messageType)
                {
                    case INFO:
                        if (storageType)
                        {
                            sysLog.Info(message);
                        }
                        else
                        {
                            dataLog.Info(message);
                        }
                        break;
                    case WARN:
                        if (storageType)
                        {
                            sysLog.Warn(message);
                        }
                        else
                        {
                            dataLog.Warn(message);
                        }
                        break;
                    case ERROR:
                        if (storageType)
                        {
                            sysLog.Error(message);
                        }
                        else
                        {
                            dataLog.Error(message);
                        }
                        break;
                    case FATAL:
                        if (storageType)
                        {
                            sysLog.Fatal(message);
                        }
                        else
                        {
                            dataLog.Fatal(message);
                        }
                        break;
                    default:
                        MessageBox.Show("日志消息类型不受支持!", "日志问题", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "日志问题", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
    }

    #endregion

    public class WaitOperation : IDisposable
    {
        public Action<string> SetText;
        public Action<int> SetMaxValue;
        public Action<int> Step;
        internal Action<int> dispose;
        public void Dispose()
        {
            dispose(0);
        }
    }
}
