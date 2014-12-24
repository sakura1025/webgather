using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text;
using Newtonsoft;
using System.Net;
using Newtonsoft.Json;
using System.Data.Entity;
using System.Collections;
using System.Threading;
using log4net;


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
            //log4net.Config.XmlConfigurator.Configure();
            log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(@"C:\Users\SunSa\documents\visual studio 2012\Projects\WebGather\WebGather\log4net.xml"));
            // Application.Run(new Form1());
            //var response = HttpUtil.GetHttpResponse("http://www.aspjzy.com/13430.html", null, null, null);

            //Stream myResponseStream = response.GetResponseStream();
            //StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.Default);
            //string retString = myStreamReader.ReadToEnd();
            //myStreamReader.Close();
            //myResponseStream.Close();
            ////Console.WriteLine(retString);
            //File.WriteAllText("c://test.txt", retString);

            using (var db = new GatherContext())
            {
                log.DebugFormat("asdfasdfasdf{0}", "孙沙");
                //var name = Console.ReadLine();
                try
                {
                    var model = new Question { Title = DateTime.Now.Ticks.ToString() };

                    //model.QuesAbilityModelId = 1;
                    model.QuesAbility = new QuesAbilityModel();
                    model.QuesAbility.ID = 19;
                    model.QuesAbilityModelId = 19;
                    model.QuesAbility.Name = "asdfasdfa";
                    model.QuesTypeModelId = 1;
                    model.QuesDiffModelId = 1;
                    db.Questions.Add(model);
                    // db.SaveChanges();
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.ToString());
                }
                //Display all Blogs from the database 
                var query = from b in db.Questions
                            orderby b.ID
                            select b;

                Console.WriteLine("All blogs in the database:");
                foreach (var item in query)
                {
                    Console.WriteLine(item.ID + "\t" + item.Title);
                    // Console.WriteLine(item.QuesAbility.ID);
                }

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }

            //Test();

        }

        static void Test()
        {
            string json = string.Empty;
            json = File.ReadAllText("c://test.txt");
            var ret = JsonConvert.DeserializeObject<List<Question>>(json);
            Console.Read();
        }
    }

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

    public class GatherContext : DbContext
    {
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuesAbilityModel> QuesAbility { get; set; }
        public DbSet<QuesDiffModel> QuesDiff { get; set; }
        public DbSet<QuesTypeModel> QuesType { get; set; }
    }

    public class WebOperation
    {

    }

    public class IWebOperation<T>
    {

    }


    public class WebMain2
    {
        public Queue<WebUrl> QuePage { get; set; }

        public void Run()
        {

        }



        public void Func(WebUrl wp)
        {
            string url = "";
            HttpWebResponse response = HttpUtil.GetHttpResponse(url, 5000, null, null);
            if (response == null)
            {
                Console.WriteLine("返回值为Response为null，return");
                return;
            }
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.Default);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            if (string.IsNullOrEmpty(retString))
            {
                Console.WriteLine("返回值为Response的html内容为空，return");
                return;
            }

        }
    }


    public class Test
    {
        List<WebUrl> listStartUrl = new List<WebUrl>();
        Queue<ListPage> queListPage = new Queue<ListPage>();
        Queue<ContentPage> queContentPage = new Queue<ContentPage>();
        Thread[] thread;
        private log4net.ILog log;

        public Test(log4net.ILog mlog)
        {
            log = mlog;
        }

        public void Start()
        {
            int i=0;
            foreach (WebUrl wu in listStartUrl)
            {
                thread[i] = new Thread(new ParameterizedThreadStart(ProcessWebUrl));
                thread[i].Name = "ProcessWebUrl" + i;
                thread[i].IsBackground = true;
                thread[i].Start(wu);
                i++;
            }

        }

        public void ProcessWebUrl(object weburl)
        {
            WebUrl wb = weburl as WebUrl;
            if (null == wb)
            {
                log.InfoFormat("weburl对象为空", null);
            }
        }
    }


    public abstract class WebUrl
    {
        public string RegexName { get; set; }
        public string Url { get; set; }
        public Encoding PageEncoding { get; set; }
    }

    public class ListPage : WebUrl
    {
        public string ParamRegex { get; set; }
        public string ListRegex { get; set; }
        public string NextPageRegex { get; set; }
    }

    public class ContentPage : WebUrl
    {
        public List<Regex> ContentRegex { get; set; }
    }

    public class Regex
    {
        public string Name { get; set; }
        public string RegexStr { get; set; }
    }
}
