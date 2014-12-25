using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft;
using System.Net;
using Newtonsoft.Json;
using System.Data.Entity;
using System.Collections;
using System.Threading;
using log4net;
using System.Runtime.InteropServices;
using System.Reflection;


namespace WebGather
{
    static class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("ConsoleAppender");
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Application.Run(new Form1());

            Gather gt = new Gather(log);
            gt.Start();


            while (true)
            {
                Console.WriteLine("主线程休眠10秒");
                Thread.Sleep(1000 * 100);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            }

     
        static void Test()
        {
            string json = string.Empty;
            json = File.ReadAllText("c://test.txt");
            var ret = JsonConvert.DeserializeObject<List<Question>>(json);
            Console.Read();
        }
    }

    public class Gather
    {

        public static int StopSecond = 1;
        public static int ThreadRequestMaxNum = 1;
        public static int ThreadResultMaxNum = 1;

        Queue<WebPage> queWebPage = new Queue<WebPage>();

        Queue<Dictionary<string, string>> queResult = new Queue<Dictionary<string, string>>();

        Thread[] threadRequest = new Thread[ThreadRequestMaxNum];
        Thread[] threadResult = new Thread[ThreadResultMaxNum];
        private log4net.ILog log;

        public Gather(log4net.ILog mlog)
        {
            log = mlog;
            queWebPage.Enqueue(new WebPage { IsListPage = true, Url = "http://dyzsks.com/KuaiXun", PageEncoding = Encoding.UTF8, RegexContent = new Regex("<div class=\"article\">(?<desc>[\\s\\S]+?)</div>\\s*</div>\\s*</div>\\s*<div class=\"rightColumn clearfix\">"), RegexList = new Regex("<li><a href=\"(?<contenturl>[\\s\\S]+?)\" style=\"color:#\\d*\" target=\"_blank\" title=\"(?<title>[\\s\\S]+?)\">[\\s\\S]+?</a></li>"), RegexParam = new Regex("<title>(?<webname>[\\s\\S]+?)</title>"), ParamObj = null });
        }

        public void Start()
        {
            for (int i = 0; i < ThreadRequestMaxNum; i++)
            {
                threadRequest[i] = new Thread(new ThreadStart(ProcessWebPage));
                threadRequest[i].Name = "ProcessWebPage" + i;
                threadRequest[i].IsBackground = true;
                threadRequest[i].Start();
            }

            for (int i = 0; i < ThreadResultMaxNum; i++)
            {
                threadResult[i] = new Thread(new ThreadStart(ProcessResult));
                threadResult[i].Name = "ProcessResult" + i;
                threadResult[i].IsBackground = true;
                threadResult[i].Start();
            }

        }

        public void Stop()
        {
            for (int i = 0; i < ThreadRequestMaxNum; i++)
            {
                if (threadRequest[i] != null && threadRequest[i].IsAlive == true)
                {
                    threadRequest[i].Abort();
                    threadRequest[i] = null;
                }
            }

            for (int i = 0; i < ThreadResultMaxNum; i++)
            {
                if (threadResult[i] != null && threadResult[i].IsAlive == true)
                {
                    threadResult[i].Abort();
                    threadResult[i] = null;
                }
            }
        }

        private Dictionary<string, string> Merger(Dictionary<string, string> firstDic,Dictionary<string, string> secondDic)
        {
            if (firstDic == null)
            {
                return secondDic;
            }
            if (secondDic == null)
            {
                return null;
            }
            foreach (var kp in secondDic)
            {
                if (firstDic.ContainsKey(kp.Key))
                {
                    firstDic[kp.Key] = kp.Value;
                }
                else
                {
                    firstDic.Add(kp.Key,kp.Value);
                }
            }
            return firstDic;

        }

        public void ProcessWebPage()
        {
            while (true)
            {
                WebPage wp = null;
                if (queWebPage.Count > 0)
                {
                   wp = queWebPage.Dequeue();
                }
                if (null == wp)
                {
                    log.InfoFormat("weburl对象为空，页面队列数量{0}，暂停{1}秒", queWebPage.Count, StopSecond);
                    Thread.Sleep(1000 * StopSecond);
                    continue;
                }
                string HtmlContent = HttpUtil.GetHttpResponse(wp.Url, 5000, null, null,wp.PageEncoding);
                if (string.IsNullOrEmpty(HtmlContent))
                {
                    log.InfoFormat("weburl的Url：{0}，获取内容为空字符串，忽略", wp.Url);
                    continue;
                }
                if (!wp.IsListPage && null != wp.RegexContent)
                {
                    Dictionary<string, string> dic = new Dictionary<string, string>();

                    Match mContent = wp.RegexContent.Match(HtmlContent);
                    if (mContent != null && mContent.Groups.Count > 0)
                    {
                        foreach (string key in wp.RegexContent.GetGroupNames())
                        {
                            if (key == "0")
                                        continue;
                            dic.Add(key, mContent.Groups[key].Value);
                        }
                    }
                    if (dic != null)
                    {
                        if (wp.ParamObj != null)
                        {
                            dic = Merger(dic, (Dictionary<string, string>)wp.ParamObj);
                        }
                        if (dic.ContainsKey("morecontenturl"))
                        {
                            WebPage nextwp = new WebPage();
                            nextwp.Url = "http://dyzsks.com"+dic["morecontenturl"];
                            nextwp.PageEncoding = wp.PageEncoding;
                            nextwp.ParamObj = dic;
                            nextwp.RegexList = null;
                            nextwp.RegexParam = null;
                            nextwp.RegexContent = wp.RegexContent;
                            nextwp.IsListPage = false;
                            queWebPage.Enqueue(nextwp);
                            dic.Remove("morecontenturl");
                        }
                        else
                        {
                            queResult.Enqueue(dic);
                        }
                        if (queWebPage.Count > 1000)
                        {
                            log.InfoFormat("weburl对象为空，页面队列数量{0}，暂停{1}秒", queWebPage.Count, StopSecond * 10);
                            System.Threading.Thread.Sleep(1000 * 10 * StopSecond);
                        }
                    }
                }
                else
                {
                    if (null != wp.RegexParam)
                    {
                        Match mParam = wp.RegexParam.Match(HtmlContent);
                        if (mParam != null && mParam.Groups.Count > 0)
                        {
                            Dictionary<string, string> dic = new Dictionary<string, string>();
                            foreach (string key in wp.RegexParam.GetGroupNames())
                            {
                                if (key == "0")
                                    continue;
                                dic.Add(key, mParam.Groups[key].Value);
                            }
                            if (wp.ParamObj != null)
                            {
                                dic = Merger(dic, (Dictionary<string, string>)wp.ParamObj);
                            }
                            wp.ParamObj = dic;
                        }
                    }
                    if (null != wp.RegexList)
                    {
                        MatchCollection mcList = wp.RegexList.Matches(HtmlContent);
                        foreach (Match m in mcList)
                        {
                            if (null != m && !string.IsNullOrEmpty(m.Groups["contenturl"].Value))
                            {
                                Dictionary<string, string> dic = new Dictionary<string, string>();
                                foreach (string key in wp.RegexList.GetGroupNames())
                                {
                                    if (key == "0")
                                        continue;
                                    dic.Add(key, m.Groups[key].Value);
                                }
                                if (wp.ParamObj != null)
                                {
                                 // wp.ParamObj=  dic.Union((Dictionary<string, string>)wp.ParamObj);
                                    dic = Merger(dic, (Dictionary<string, string>)wp.ParamObj);
                                }
                                WebPage nextwp = new WebPage();
                                nextwp.Url = "http://dyzsks.com" + m.Groups["contenturl"].Value;
                                nextwp.PageEncoding = wp.PageEncoding;
                                nextwp.ParamObj = dic;
                                nextwp.RegexList = null;
                                nextwp.RegexParam = null;
                                nextwp.RegexContent = wp.RegexContent;
                                nextwp.IsListPage = false;
                                queWebPage.Enqueue(nextwp);
                            }
                        }
                    }
                }
            }
        }
            

        

        public void ProcessResult()
        {
            while (true)
            {
                Dictionary<string, string> dicresult = new Dictionary<string, string>();
                if (queResult.Count > 0)
                {
                    dicresult = queResult.Dequeue();
                    File.AppendAllText("c:\\Testasdf.txt)",string.Format("附带参数:{0}\n标题:{1}\n网址:{3}\n详细内容:{2}\n------------------------------\n\n", dicresult["webname"], dicresult["title"], dicresult["desc"], dicresult["contenturl"]));
                }
                const int StopSencond = 3;
                if (null == dicresult||dicresult.Keys.Count==0)
                {
                    log.InfoFormat("dicresult对象为空，结果队列数量{0}，暂停{1}秒", queResult.Count, StopSencond);
                    Thread.Sleep(1000 * StopSencond);
                    continue;
                }
            }
        }
    }


    #region Match帮助类

    public class MatchHelper
    {
        public static T PopulateEntityFromCollection<T>(T entity, Match m) where T : new()
        {
            //初始化 如果为null
            if (entity == null)
            {
                entity = new T();
            }
            //得到类型
            Type type = typeof(T);
            //取得属性集合
            PropertyInfo[] pi = type.GetProperties();
            foreach (PropertyInfo item in pi)
            {
                string value = m.Groups[item.Name.ToLower()].Value;
                //给属性赋值
                if (m.Groups[item.Name.ToLower()].Value != null)
                {
                    
                    item.SetValue(entity, string.IsNullOrEmpty(value) ? null : Convert.ChangeType(value, item.PropertyType), null);
                }
            }
            return entity;
        }
    }

    public interface IMatchToModel
    {
        dynamic GetModel(string input);
    }


    #endregion

    #region 采集类

    public class WebPage
    {
        public string Url { get; set; }
        public Encoding PageEncoding { get; set; }
        public Regex RegexList { get; set; }
        public Regex RegexParam { get; set; }
        public Regex RegexContent { get; set; }
        public dynamic ParamObj { get; set; }
        public bool IsListPage { get; set; }
    }

    public class WebRegex
    {
        public string Url { get; set; }
        public Regex RegexStr { get; set; }
    }

    public enum RegexTypeEnum
    {
        Content = 1,
        List,
        Param
    }

    #endregion

    #region 试卷类


    public class Question
    {
        public string Categories { get; set; }
        public int ChildNum { get; set; }
        public string From { get; set; }
        public string Guid { get; set; }
        public int ID { get; set; }
        public string Knowledge { get; set; }
        public string QuesAnswer { get; set; }
        public string QuesBody { get; set; }
        public string QuesParse { get; set; }
        public string Time { get; set; }
        public string Title { get; set; }
        public int ActuaUseSum { get; set; }
        public int AvgScore { get; set; }
        public int Grade { get; set; }
        public int IsSave { get; set; }
        public int UseSum { get; set; }

        public int QuesAbilityModelId { get; set; }
        public int QuesDiffModelId { get; set; }
        public int QuesTypeModelId { get; set; }



        public virtual QuesAbilityModel QuesAbility { get; set; }
        public virtual QuesDiffModel QuesDiff { get; set; }
        public virtual QuesTypeModel QuesType { get; set; }
    }

    public class QuesAbilityModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class QuesDiffModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class QuesTypeModel
    {
        public int ID { get; set; }
        public bool IsSelectType { get; set; }
        public string Name { get; set; }
    }

    #endregion

    #region 数据访问

    /// <summary>
    /// 数据库访问
    /// </summary>
    public class GatherContext : DbContext
    {
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuesAbilityModel> QuesAbility { get; set; }
        public DbSet<QuesDiffModel> QuesDiff { get; set; }
        public DbSet<QuesTypeModel> QuesType { get; set; }
    }

    #endregion
}
