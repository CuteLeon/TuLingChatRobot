using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TuLingChatRobot
{
    class Program
    {
        //todo:可以写一个方法根据code返回对应的正则表达式，以分析其他类型的消息

        #region 说明
        /*
        * 此程序使用图灵机器人服务
        * 机器人管理地址：http://www.tuling123.com/member/robot/index.jhtml
        * 机器人设置地址：http://www.tuling123.com/member/robot/1015495/center/frame.jhtml?url=/member/robot/1015495/detail.jhtml
        * 机器人帮助文档：http://www.tuling123.com/help/h_cent_webapi.jhtml?nav=doc
        * 邮箱：2543280836@qq.com
        */
        #endregion

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

                //判断是否遇到错误的返回代码
                if (AnalysisCode(RobotCode)) continue;

                //正常的返回结果
                RobotSay(RobotMessage);
            }
        }

        /// <summary>
        /// 用户消息过滤器
        /// </summary>
        /// <param name="Message">用户消息</param>
        /// <returns>是否拦截此消息</returns>
        private static bool MessageFilter(ref string Message)
        {
            string TempMessage = Message.ToLower();
            switch (TempMessage)
            {
                case "clear":
                    {
                        Console.Clear();
                        SayHello();
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        /// <summary>
        /// 机器人说话
        /// </summary>
        /// <param name="Message">用户消息</param>
        /// <param name="MessageValues">消息参数</param>
        private static void RobotSay(string Message, params object[] MessageValues)
        {
            RobotSay(string.Format(Message, MessageValues));
        }

        /// <summary>
        /// 机器人说话
        /// </summary>
        /// <param name="Message">用户消息</param>
        private static void RobotSay(string Message)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("[{0}] {1} : ", DateTime.Now.ToString("yyy-MM-dd hh:mm:ss"), RobotName);
            Console.ResetColor();
            Console.WriteLine("<<<\t{0}", Message);
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("* Me : ");
            Console.ResetColor();
            Console.Write(">>>\t");
        }

        /// <summary>
        /// 打招呼
        /// </summary>
        private static void SayHello()
        {
            Console.Title = string.Format("{0} - 智能聊天机器人，您的ID: [{1}]（使用 [Ctrl+C] 结束聊天）", RobotName, UserID);
            Console.WriteLine("欢迎使用智能聊天机器人，我是 {0}\n", RobotName);
            RobotSay("您好哦");
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
            try
            {
                string MessagePattern = string.Empty;
                Regex MessageRegex = null;
                Match MessageMatch = null;
                switch (robotCode)
                {
                    //普通消息
                    case "100000":
                        {
                            MessagePattern = "{.*?\"code\":(?<RobotCode>.+?),.*?\"text\":\"(?<RobotMessage>.+?)\".*?}";
                            MessageRegex = new Regex(MessagePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            MessageMatch = MessageRegex.Match(returnMessage);
                            if (MessageMatch.Success)
                            {
                                robotCode = MessageMatch.Groups["RobotCode"].Value as string;
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
                            MessagePattern = "{.*?\"code\":(?<RobotCode>.+?),.*?\"text\":\"(?<RobotMessage>.+?)\",\"url\":\"(?<RobotLink>.+?)\".*?}";
                            MessageRegex = new Regex(MessagePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            MessageMatch = MessageRegex.Match(returnMessage);
                            if (MessageMatch.Success)
                            {
                                robotCode = MessageMatch.Groups["RobotCode"].Value as string;
                                robotMessage = MessageMatch.Groups["RobotMessage"].Value as string;
                                return true;
                            }
                            else
                            {
                                Debug.Print("无法正则匹配的消息: {0}", returnMessage);
                                return false;
                            }
                        }
                    default:
                        {
                            Debug.Print("未知的返回代码: {0}", robotCode);
                            return false;
                        }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("分析消息遇到异常: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 分析返回代码
        /// </summary>
        /// <param name="robotCode"></param>
        /// <returns>是否错误的返回代码</returns>
        private static bool AnalysisCode(string robotCode)
        {
            switch (robotCode)
            {
                case "40001":
                    {
                        RobotSay("遇到错误，错误的 API_KEY 哦。");
                        break;
                    }
                case "40002":
                    {
                        RobotSay("遇到错误，您什么都没说哦。");
                        break;
                    }
                case "40004":
                    {
                        RobotSay("遇到错误，今天说了好多哦，让我休息下，明天再来吧。");
                        break;
                    }
                case "40007":
                    {
                        RobotSay("遇到错误，数据格式异常哦。");
                        break;
                    }
                default:
                    {
                        return false;
                    }
            }
            return true;
        }

        /*
         * 文字类：
        {"code":100000,"text":"你也好 嘻嘻"}
         * 链接类：
        {"code": 200000,"text": "亲，已帮你找到图片","url": "http://m.image.so.com/i?q=%E5%B0%8F%E7%8B%97"}
         * 新闻类：                      
        {"code": 302000,"text": "亲，已帮您找到相关新闻","list": [{"article": "工信部:今年将大幅提网速降手机流量费","source": "网易新闻", "icon": "","detailurl": "http://news.163.com/15/0416/03/AN9SORGH0001124J.html"},{"article": "北京最强沙尘暴午后袭沪 当地叫停广场舞","source": "网易新闻", "icon": "","detailurl": "http://news.163.com/15/0416/14/ANB2VKVC00011229.html"},{"article": "公安部:小客车驾照年内试点自学直考","source": "网易新闻", "icon": "","detailurl": "http://news.163.com/15/0416/01/AN9MM7CK00014AED.html"}]}
         * 列车类：
        {"code": 200000,"text": "亲，已帮你找到列车信息","url": "http://touch.qunar.com/h5/train/trainList?startStation=%E5%8C%97%E4%BA%AC&endStation=%E6%8B%89%E8%90%A8&searchType=stasta&date=2015-12-25&sort=3&filterTrainType=1&filterTrainType=2&filterTrainType=3&filterTrainType=4&filterTrainType=5&filterTrainType=6&filterTrainType=7&filterDeptTimeRa"}
         * 航班类：
        {"code": 200000,"text": "亲，已帮您找到航班信息","url": "http://touch.qunar.com/h5/flight/flightlist?bd_source=chongdong&startCity=%E5%8C%97%E4%BA%AC&destCity=%E6%8B%89%E8%90%A8&startDate=2015-12-25&backDate=&flightType=oneWay&priceSortType=1"}
         * 菜谱类：
        {"code": 308000,"text": "亲，已帮您找到菜谱信息","list": [{"name": "鱼香肉丝","icon": "http://i4.xiachufang.com/image/280/cb1cb7c49ee011e38844b8ca3aeed2d7.jpg","info": "猪肉、鱼香肉丝调料 | 香菇、木耳、红萝卜、黄酒、玉米淀粉、盐","detailurl": "http://m.xiachufang.com/recipe/264781/"}]}
         * 歌曲类：
        {"code": 313000,"text": "开始播放音乐。","function": {"song": "刘德华","singer": "忘情水"}}
         * 诗词类：
        {"code": 314000,"text": "开始朗读诗词。","function": {"author": "李白","name": "望庐山瀑布"}}
         */

    }
}
