using GTranslate.Translators;
using System.Text;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

class Program
{


    static void Main(string[] args)
    {
        var translateData = Program.LoadTranslateData();
        Console.WriteLine($"已载入字典文件共：{translateData.Count}项");
        Console.WriteLine("请输入需要翻译的文件夹路径（会自动扫描子目录内的文件）");
        var path = Console.ReadLine();
        if (System.IO.Directory.Exists(path) == false)
        {
            Console.WriteLine($"输入路径{path}无效");
            return;
        }


        Console.WriteLine("是否更新字典文件?(y/n)");
        if (Console.ReadLine().ToUpper() == "Y")
        {
            var source = new System.Collections.Concurrent.ConcurrentQueue<string>(LoadXmlData(path).Where(k => translateData.ContainsKey(k) == false));
            Console.WriteLine($"载入等待翻译的语句共计：{source.Count()}项");
            var thread_num = 5;
            var temp_dic = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
            var task_list = new List<Task>();
            for (int i = 0; i < thread_num; i++)
            {
                var task = new Task(() =>
                {
                    var translator = new AggregateTranslator();
                    while (source.Count > 0)
                    {
                        try
                        {
                            var sb = new StringBuilder();
                            var array = DequeueArray(source, 4000);
                            foreach (var item in array)
                            {
                                sb.AppendLine(item);
                                sb.AppendLine("@@@@");
                            }

                            var result = translator.TranslateAsync(sb.ToString(), "zh-cn").Result;
                            if (result == null || string.IsNullOrWhiteSpace(result.Translation))
                                continue;

                            var result_dic = AnalyzeText(array, result.Translation);
                            foreach (var item in result_dic)
                            {
                                Console.WriteLine($"{temp_dic.Count}/{source.Count}\t{item.Key}\t{item.Value}");
                                translateData[item.Key] = item.Value;
                                temp_dic[item.Key] = item.Value;
                                if (temp_dic.Count > 10000)
                                {
                                    lock (temp_dic)
                                    {
                                        if (temp_dic.Count > 10000)
                                        {
                                            var dic2 = temp_dic;
                                            temp_dic = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
                                            SaveDataFile(dic2);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }, TaskCreationOptions.LongRunning);
                task.Start();
                task_list.Add(task);
            }

            Task.WaitAll(task_list.ToArray());

            if (temp_dic.Count >= 0)
            {
                SaveDataFile(temp_dic);
                temp_dic.Clear();
            }

            Console.WriteLine("更新字典完成");
        }
        Console.WriteLine("使用字典翻译xml文件?(Y/N)");
        if (Console.ReadLine().ToUpper() == "Y")
        {
            TranslateXml(translateData, path);
            Console.WriteLine("翻译完成，请检查translate文件夹，如需使用请手动复制替换原有文件。");
        }
        Console.ReadLine();
    }
    /// <summary>
    /// 读取xml
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<XmlNode> ReadXmlNodes(XmlNode node)
    {
        foreach (XmlNode item in node.ChildNodes)
        {
            if (item.ChildNodes.Count > 0)
                foreach (XmlNode sub_item in ReadXmlNodes(item))
                {
                    if (sub_item.Value != null && sub_item.NodeType == XmlNodeType.Text && (item.ParentNode == null || item.ParentNode.Name != "name"))
                        yield return sub_item;
                }
            else if (item.Value != null && item.NodeType == XmlNodeType.Text && (item.ParentNode == null || item.ParentNode.Name != "name"))
                yield return item;
        }
    }

    /// <summary>
    /// 解析翻译后的内容
    /// </summary>
    /// <param name="source"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Dictionary<string, string> AnalyzeText(string[] source, string text)
    {
        if (System.Text.RegularExpressions.Regex.Matches(text, "@@@@").Count != source.Length)
            throw new ArgumentOutOfRangeException();

        var reader = new System.IO.StringReader(text);
        var dic = new Dictionary<string, string>();
        var sb = new StringBuilder();
        var index = 0;
        while (true)
        {
            var line = reader.ReadLine();
            if (line == null)
                return dic;
            else if (line == "@@@@")
            {
                dic[source[index++]] = sb.ToString();
                sb.Clear();
            }
            else
                sb.AppendLine(line);
        }
    }
    /// <summary>
    /// 从列队中取出字符串
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="limit_char_number"></param>
    /// <returns></returns>
    public static string[] DequeueArray(System.Collections.Concurrent.ConcurrentQueue<string> queue, int limit_char_number)
    {
        int total_char_number = 0;
        int total_line_number = 0;
        List<string> list = new List<string>();
        while (queue.Count > 0)
        {
            if (queue.TryDequeue(out string result))
            {

                if (result.Length > 2000)
                    continue;

                if (total_char_number + (total_line_number * (2 + 4)) > limit_char_number)
                {
                    queue.Enqueue(result);
                    break;
                }
                else
                {
                    total_char_number += result.Length;
                    total_line_number++;
                    list.Add(result);
                }
            }
        }
        return list.ToArray();
    }

    public static void SaveDataFile(IEnumerable<KeyValuePair<string, string>> temp_dic)
    {
        System.IO.File.WriteAllText($@"..\..\..\Data\{System.Environment.GetEnvironmentVariable("UserName").ToString()}_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.json", Newtonsoft.Json.JsonConvert.SerializeObject(temp_dic));
        Console.WriteLine("写入json文件完成");
    }

    /// <summary>
    /// 使用字典文件,翻译指定目录的所有文件
    /// </summary>
    /// <param name="dic"></param>
    /// <param name="path"></param>
    public static void TranslateXml(Dictionary<string, string> dic, string path)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Parse;

        foreach (var filename in System.IO.Directory.GetFiles(path, @"*.XML", SearchOption.AllDirectories))
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filename);
                if (IsIntellisenseXml(doc) == false)
                    continue;

                var fileInfo = new System.IO.FileInfo(filename);
                Console.WriteLine("output " + fileInfo.Name);
                var outPath = filename.Substring(path.Length, filename.Length - path.Length - fileInfo.Name.Length - 1);
                System.IO.Directory.CreateDirectory(@$".\translate\{outPath}");
                System.IO.Directory.CreateDirectory(@$".\backup\{outPath}");
                var outFilename = @$".\translate\{outPath}\{fileInfo.Name}";
                var backupFilename = @$".\backup\{outPath}\{fileInfo.Name}";
                doc.Save(backupFilename);

                foreach (var item in ReadXmlNodes(doc))
                {
                    if (item.Value == null)
                        continue;
                    var text = item.Value;
                    if (dic.ContainsKey(text))
                        item.Value = dic[text] + "\r\n" + text;
                }
                doc.Save(outFilename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }

    /// <summary>
    /// 载入xml文件,并过滤重复的语句
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string> LoadXmlData(string path)
    {
        var hash = new HashSet<string>();

        Dictionary<string, string> dic = new Dictionary<string, string>();

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Parse;

        foreach (var fileName in System.IO.Directory.GetFiles(path, @"*.xml", SearchOption.AllDirectories))
        {
            Console.WriteLine($"load {fileName}");
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fileName);
                if (IsIntellisenseXml(doc) == false)
                    continue;
                foreach (var item in ReadXmlNodes(doc))
                {
                    if (item.Value == null)
                        continue;
                    if (HasChinese(item.Value) == false)
                        hash.Add(item.Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        return hash;
    }

    public static XmlNode FindXmlNote(XmlNodeList nodes, string name)
    {
        foreach (XmlNode item in nodes)
        {
            if (item.Name == name)
                return item;
        }
        return null;
    }
    public static bool IsIntellisenseXml(XmlDocument doc)
    {
        var doc_node = FindXmlNote(doc.ChildNodes, "doc");
        if (doc_node == null)
            return false;

        var assembly_node = FindXmlNote(doc_node.ChildNodes, "assembly");
        if (doc_node == null)
            return false;

        if (FindXmlNote(assembly_node.ChildNodes, "name") == null)
            return false;

        if (FindXmlNote(doc_node.ChildNodes, "members") == null)
            return false;

        return true;
    }
    /// <summary>
    /// 载入已经翻译过的数据字典
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, string> LoadTranslateData()
    {
        var result = new Dictionary<string, string>();
        foreach (var filename in System.IO.Directory.GetFiles(@"..\..\..\Data\"))
        {
            try
            {
                var json = System.IO.File.ReadAllText(filename);
                var items = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                foreach (var item in items.Where(k => string.IsNullOrWhiteSpace(k.Value) == false))
                    result[item.Key] = item.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"载入{filename}文件出现异常:{ex.Message}\r\n{ex.StackTrace}");
            }
        }
        return result;

    }
    /// <summary>
    /// 判断字符串中是否包含中文
    /// </summary>
    /// <param name="str">需要判断的字符串</param>
    /// <returns>判断结果</returns>
    public static bool HasChinese(string str)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
    }
}