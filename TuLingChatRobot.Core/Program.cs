using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TuLingChatRobot.Core
{
    class Program
    {
        /// <summary>
        /// 机器人接口地址 
        /// </summary>
        private static readonly string RobotAddress = @"http://www.tuling123.com/openapi/api";
        /// <summary>
        /// 认证私匙
        /// </summary>
        private static readonly string API_KEY = @"7bf230dada7d450da41874cb50af193a";
        /// <summary>
        /// 机器人名字
        /// </summary>
        private static readonly string RobotName = @"Mathilda";
        /// <summary>
        /// 启动时随机生成用户ID，可以在每次启动时关联上下文语境
        /// </summary>
        private static readonly string UserID = new Random().Next(0, int.MaxValue).ToString();
        /// <summary>
        /// 用户输入消息
        /// </summary>
        private static string UserMessage = string.Empty;
        /// <summary>
        /// 返回消息
        /// </summary>
        private static string ReturnMessage = string.Empty;
        /// <summary>
        /// 机器人返回代码
        /// </summary>
        private static string RobotCode = string.Empty;
        /// <summary>
        /// 机器人返回消息
        /// </summary>
        private static string RobotMessage = string.Empty;
        /// <summary>
        /// 机器人返回链接列表
        /// </summary>
        private static List<string> RobotLinks = new List<string>();
        /// <summary>
        /// 聊天客户端
        /// </summary>
        private static WebClient ChatClient = null;

        static void Main(string[] args)
        {
            try
            {
                Console.CancelKeyPress += new ConsoleCancelEventHandler((s, e) => { Environment.Exit(0); });
                Console.WriteLine("中断事件绑定完毕.");

                ChatClient = new WebClient() { BaseAddress = RobotAddress, Encoding = Encoding.UTF8 };
                Console.WriteLine("WebClient 对象创建完毕.");

                ChatClient.QueryString.Add("key", API_KEY);
                ChatClient.QueryString.Add("info", string.Empty);
                ChatClient.QueryString.Add("userid", UserID);
                Console.WriteLine("初始化 QueryString 完成.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("——————————");
                Console.WriteLine("程序初始化失败：{0}", ex.Message);
                Console.WriteLine("\n(按任意键结束...)");
                Console.Read();
                Environment.Exit(0);
            }
            Console.Clear();

            SayHello();

            while (true)
            {
                UserMessage = Console.ReadLine();
                //过滤消息
                if (MessageFilter(ref UserMessage)) continue;
                //把用户消息加入 QueryString
                ChatClient.QueryString.Set("info", UserMessage);
                Debug.Print("用户消息: {0}", UserMessage);

                //获取返回消息
                ReturnMessage = ChatClient.DownloadString(RobotAddress);

                //分析消息
                if (!AnalysisMessage(ReturnMessage, ref RobotCode, ref RobotMessage))
                {
                    RobotSay("无法理解的机器人消息: {0}", ReturnMessage);
                    continue;
                }

                //正常的返回结果
                if (RobotLinks.Count == 0)
                    RobotSay(RobotMessage);
                else
                    RobotSay(RobotMessage, RobotLinks);
            }
        }

        /// <summary>
        /// 用户消息过滤器
        /// </summary>
        /// <param name="Message">用户消息</param>
        /// <returns>是否拦截此消息</returns>
        private static bool MessageFilter(ref string Message)
        {
            string TempMessage = Message.ToLower().Trim();
            switch (TempMessage)
            {
                case "/clear":
                    {
                        RobotLinks.Clear();
                        Console.Clear();
                        SayHello();
                        return true;
                    }
                case "/link":
                    {
                        if (RobotLinks.Count > 0)
                        {
                            RobotSay("正在为您打开{0}个链接...", RobotLinks.Count);
                            foreach (string Link in RobotLinks)
                                Process.Start(Link);
                        }
                        else
                            RobotSay("没有可以打开的链接。");
                        return true;
                    }
                case "/exit":
                    {
                        Environment.Exit(0);
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        /// <summary>
        /// 带链接的机器人说话
        /// </summary>
        /// <param name="Message">机器人消息</param>
        /// <param name="robotLinks">机器人链接</param>
        private static void RobotSay(string Message, List<string> robotLinks)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[{0}] {1} : ", DateTime.Now.ToString("yyy-MM-dd hh:mm:ss"), RobotName);
            Console.ResetColor();
            Console.WriteLine("<<<\t{0}", Message);

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\t" + string.Join("\n\t", robotLinks.ToArray()));
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("* Me : ");
            Console.ResetColor();
            Console.Write(">>>\t");
        }

        /// <summary>
        /// 机器人说话
        /// </summary>
        /// <param name="Message">机器人消息</param>
        /// <param name="MessageValues">消息参数</param>
        private static void RobotSay(string Message, params object[] MessageValues)
        {
            RobotSay(string.Format(Message, MessageValues));
        }

        /// <summary>
        /// 机器人说话
        /// </summary>
        /// <param name="Message">机器人消息</param>
        private static void RobotSay(string Message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[{0}] {1} : ", DateTime.Now.ToString("yyy-MM-dd hh:mm:ss"), RobotName);
            Console.ResetColor();
            Console.WriteLine("<<<\t{0}", Message);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("* Me : ");
            Console.ResetColor();
            Console.Write(">>>\t");
        }

        /// <summary>
        /// 打招呼
        /// </summary>
        private static void SayHello()
        {
            Console.Title = string.Format("{0} ID: [{1}]（使用 [Ctrl+C] 结束聊天）", RobotName, UserID);
            RobotSay("我是{0}，您好哦。", RobotName);
        }

        /// <summary>
        /// 分析返回消息
        /// </summary>
        /// <param name="returnMessage">返回消息</param>
        /// <param name="robotCode">返回代码</param>
        /// <param name="robotMessage">机器人消息</param>
        /// <returns>是否能够分析消息</returns>
        private static bool AnalysisMessage(string returnMessage, ref string robotCode, ref string robotMessage)
        {
            Debug.Print("分析返回消息: {0}", returnMessage);
            RobotLinks.Clear();
            try
            {
                string MessagePattern = "{.*?\"code\":(?<RobotCode>.+?),.*?}";
                Regex MessageRegex = new Regex(MessagePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline); ;
                Match MessageMatch = MessageRegex.Match(returnMessage);
                if (MessageMatch.Success)
                {
                    robotCode = MessageMatch.Groups["RobotCode"].Value as string;
                    Debug.Print("获取返回代码: {0}", robotCode);
                }
                else
                {
                    Debug.Print("无法获取返回代码: {0}", returnMessage);
                    return false;
                }

                switch (robotCode)
                {
                    //普通消息
                    case "100000":
                        {
                            MessagePattern = "{.*?\"text\":\"(?<RobotMessage>.+?)\".*?}";
                            MessageRegex = new Regex(MessagePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            MessageMatch = MessageRegex.Match(returnMessage);
                            if (MessageMatch.Success)
                            {
                                robotMessage = MessageMatch.Groups["RobotMessage"].Value as string;
                                return true;
                            }
                            else
                            {
                                Debug.Print("无法正则匹配的消息: {0}", returnMessage);
                                return false;
                            }
                        }
                    //链接消息
                    case "200000":
                        {
                            MessagePattern = "{.*?\"text\":\"(?<RobotMessage>.*?)\",\"url\":\"(?<RobotLink>.*?)\".*?}";
                            MessageRegex = new Regex(MessagePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            MessageMatch = MessageRegex.Match(returnMessage);
                            if (MessageMatch.Success)
                            {
                                robotMessage = MessageMatch.Groups["RobotMessage"].Value as string;
                                RobotLinks.Add(MessageMatch.Groups["RobotLink"].Value as string);
                                return true;
                            }
                            else
                            {
                                Debug.Print("无法正则匹配的消息: {0}", returnMessage);
                                return false;
                            }
                        }
                    //新闻类
                    case "302000":
                        {
                            returnMessage = "{\"code\":302000,\"text\":\"亲，已帮您找到相关新闻\",\"list\":[{\"article\":\"2017年中朝双边贸易额50.6亿美元 同比下降逾一成\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/wx4/large/006FSgCbly1fndmu2swa4j30go0b4mzc.jpg/w120h90l50t1743.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqnick9328459.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"长春市长谈今年发展目标:地区生产总值增长约7.8%\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w600h401/20180112/C-Xj-fyqnick9279872.jpg/w120h90l50t10d2.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqqciz5805790.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"首例穿山甲公益诉讼:东莞林业局被判答复原告申请\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/news/crawl/w550h733/20180112/WvZg-fyqnick9201896.jpg/w120h90l50t1a73.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqqciz5793459.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"习近平提出未来全面从严治党五条举措\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/news/crawl/w550h739/20180112/FZYC-fyqnick9060815.jpg/w120h90l50t1a36.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqqieu5886007.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"中国官方一句话 让恐慌两天的美国人松了一大口气\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/front/w437h246/20180112/MMMx-fyqnick9054034.jpg/w120h90l50t1a2f.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqqieu5885356.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"新华社：万豪迟到的道歉 网民为何“不买账”\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/news/crawl/w481h189/20180112/9b7V-fyqnick8997295.jpg/w120h90l50t1c90.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqqieu5882505.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"为何高铁不等人？专家：晚点5分钟全国高铁都会乱\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w447h364/20180112/kJHD-fyqnick8977378.jpg/w120h90l50t15d1.jpg\",\"detailurl\":\"https://news.sina.cn/2018-01-12/detail-ifyqptqv8121380.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"陈水扁遭批装病心情差想赴岛外治疗 被蔡当局驳回\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w405h270/20180112/JFHe-fyqnick9096780.jpg/w120h90l50t1e15.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqqciz5782762.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"民进党锁定蒋万安为2018年台北市长选战假想敌\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/news/crawl/w456h500/20180112/OEQ7-fyqnick8862902.jpg/w120h90l50t1d9f.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqnick8864619.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"委内瑞拉多地发生哄抢商店和抢劫案 中使馆提醒\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w990h545/20180112/LmJb-fyqnick8837496.jpg/w120h90l50t1136.jpg\",\"detailurl\":\"https://news.sina.cn/2018-01-12/detail-ifyqptqv8100877.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"“独派”挺赖清德民调 港媒:公开挑战蔡英文连任\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w721h480/20180112/plgH-fyqnick8999547.jpg/w120h90l50t1b5e.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqqciz5775161.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"下一个万豪？达美航空中文网列西藏是“国家”\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w774h604/20180112/Bn9B-fyqnick8753344.png/w120h90l50t13b6.jpg\",\"detailurl\":\"https://news.sina.cn/gj/2018-01-12/detail-ifyqptqv8088780.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"新华日报迎创刊80周年 系中共首份全国政治机关报\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w600h494/20180112/R49i-fyqnick8744811.jpg/w120h90l50t176e.jpg\",\"detailurl\":\"https://news.sina.cn/2018-01-12/detail-ifyqptqv8087462.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"军报：各国网军建设加速 网络争夺或将掀起新高潮\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/news/transform/w400h266/20180112/pt6y-fyqnick8717670.jpg/w120h90l50t15dc.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqqieu5861645.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"故宫前星门斗匾汉字不见了？工作人员回应\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/news/crawl/w400h300/20180112/gBCv-fyqnick8745672.jpg/w120h90l50t131b.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqptqv8084137.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"中国近海发生碰撞巴拿马籍起火油船已漂浮至日本\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w1024h768/20180112/6Qal-fyqnick8721561.jpg/w120h90l50t1baa.jpg\",\"detailurl\":\"https://news.sina.cn/gj/2018-01-12/detail-ifyqptqv8083877.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"2017年中国国防科工十大新闻 国产航母歼20受关注\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w2048h1286/20180112/JRiC-fyqnick8713129.jpg/w120h90l50t15f4.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqptqv8082174.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/front/w826h830/20180112/RZLF-fyqnick8694419.jpg/w120h90l50t1e27.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqptqv8079048.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"中纪委年度大会有啥看点 新闻联播有线索\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/news/transform/w550h366/20180112/EI5n-fyqnick8673970.jpg/w120h90l50t16fa.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqnick8678566.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w1280h721/20180112/r6sk-fyqnick8679053.jpg/w120h90l50t140d.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqptqv8076222.d.html?vt=4&pos=8&cid=56261\"},{\"article\":\"\",\"source\":\"新浪新闻\",\"icon\":\"http://k.sinaimg.cn/n/translate/w1280h720/20180112/aj73-fyqnick8676644.jpg/w120h90l50t15fb.jpg\",\"detailurl\":\"https://news.sina.cn/gn/2018-01-12/detail-ifyqqciz5723024.d.html?vt=4&pos=8&cid=56261\"}]}";
                            MessagePattern = "{.*?\"text\":\"(?<RobotMessage>.*?)\".*?}";
                            MessageRegex = new Regex(MessagePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            MessageMatch = MessageRegex.Match(returnMessage);
                            if (MessageMatch.Success)
                            {
                                robotMessage = MessageMatch.Groups["RobotMessage"].Value as string;
                            }
                            else
                            {
                                Debug.Print("无法正则匹配的消息: {0}", returnMessage);
                                return false;
                            }
                            MessagePattern = "{\"article\":\"(?<NewsTitle>.*?)\",\"source\":\"(?<NewsSource>.*?)\",\"icon\":\"(?<NewsIcon>.*?)\",\"detailurl\":\"(?<NewsLink>.*?)\"}";
                            MessageRegex = new Regex(MessagePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            int NewsIndex = 0;
                            foreach (Match NewsMatch in MessageRegex.Matches(returnMessage))
                            {
                                NewsIndex++;
                                robotMessage += string.Format("\n\t[{0}] {1}-(来自：{2})",
                                    NewsIndex,
                                    NewsMatch.Groups["NewsTitle"].Value as string,
                                    NewsMatch.Groups["NewsSource"].Value as string
                                );
                                RobotLinks.Add(NewsMatch.Groups["NewsLink"].Value as string);
                            }
                            return true;
                        }
                    //菜谱类
                    case "308000":
                        {
                            MessagePattern = "{.*?\"text\":\"(?<RobotMessage>.*?)\".*?}";
                            MessageRegex = new Regex(MessagePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            MessageMatch = MessageRegex.Match(returnMessage);
                            if (MessageMatch.Success)
                            {
                                robotMessage = MessageMatch.Groups["RobotMessage"].Value as string;
                            }
                            else
                            {
                                Debug.Print("无法正则匹配的消息: {0}", returnMessage);
                                return false;
                            }
                            MessagePattern = "{\"name\":\"(?<FoodName>.*?)\",\"icon\":\"(?<FoodIcon>.*?)\",\"info\":\"(?<FoodInfo>.*?)\",\"detailurl\":\"(?<FoodLink>.*?)\"}";
                            MessageRegex = new Regex(MessagePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            int NewsIndex = 0;
                            foreach (Match NewsMatch in MessageRegex.Matches(returnMessage))
                            {
                                NewsIndex++;
                                robotMessage += string.Format("\n\t[{0}] {1}-(来自：{2})",
                                    NewsIndex,
                                    NewsMatch.Groups["FoodName"].Value as string,
                                    NewsMatch.Groups["FoodInfo"].Value as string
                                );
                                RobotLinks.Add(NewsMatch.Groups["FoodLink"].Value as string);
                            }
                            return true;
                        }
                    default:
                        {
                            // "40001"/"40002"/"40004"/"40007" : 遇到异常的返回代码
                            Debug.Print("遇到未知的返回代码：{0}", robotCode);
                            MessagePattern = "{.*?\"text\":\"(?<ErrorMessage>.*?)\".*?}";
                            MessageRegex = new Regex(MessagePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            MessageMatch = MessageRegex.Match(returnMessage);
                            if (MessageMatch.Success)
                            {
                                robotMessage = MessageMatch.Groups["ErrorMessage"].Value as string;
                                return true;
                            }
                            else
                            {
                                Debug.Print("无法正则匹配的消息: {0}", returnMessage);
                                return false;
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("分析消息遇到异常: {0}", ex.Message);
                return false;
            }
        }

    }
}
