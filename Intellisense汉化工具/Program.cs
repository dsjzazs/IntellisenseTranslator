using GTranslate.Translators;
using Intellisense汉化工具;
using static System.Net.Mime.MediaTypeNames;

class Program
{

    static void Main(string[] args)
    {
        var translator = new AggregateTranslator();

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
            var thread_num = 30;
            var temp_dic = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
            var task_list = new List<Task>();
            for (int i = 0; i < thread_num; i++)
            {
                var task = new Task(() =>
                {
                    while (source.Count > 0)
                    {
                        if (source.TryDequeue(out string text))
                        {
                            if (translateData.ContainsKey(text))
                                return;
                            try
                            {
                                Console.WriteLine("\t" + text);
                                var result = translator.TranslateAsync(text, "zh-cn").Result;
                                if (result != null && string.IsNullOrWhiteSpace(result.Translation) == false)
                                {
                                    Console.WriteLine($"{temp_dic.Count}/{source.Count}\t{text}\t{result.Translation}");
                                    translateData[text] = result.Translation;
                                    temp_dic[text] = result.Translation;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
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
        var factory = new System.Xml.Serialization.XmlSerializerFactory();
        var serial = factory.CreateSerializer(typeof(doc));

        foreach (var filename in System.IO.Directory.GetFiles(path, @"*.XML", SearchOption.AllDirectories))
        {
            try
            {
                var fileInfo = new System.IO.FileInfo(filename);
                var doc = XmlReader.Read(filename);
                foreach (var item2 in XmlReader.ReadTextEnumerable(doc.members))
                {
                    for (int i = 0; i < item2.Length; i++)
                    {
                        if (dic.ContainsKey(item2[i]))
                            item2[i] = dic[item2[i]] + "\r\n" + item2[i];
                    }
                }
                Console.WriteLine("output " + fileInfo.Name);
                var outPath = filename.Substring(path.Length, filename.Length - path.Length - fileInfo.Name.Length - 1);
                System.IO.Directory.CreateDirectory(@$".\translate\{outPath}");
                var outFilename = @$".\translate\{outPath}\{fileInfo.Name}";
                var writeStream = System.IO.File.Create(outFilename);
                serial.Serialize(writeStream, doc);
                writeStream.Dispose();
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
        foreach (var fileName in System.IO.Directory.GetFiles(path, @"*.xml", SearchOption.AllDirectories))
        {
            Console.WriteLine($"load {fileName}");
            var fileInfo = new System.IO.FileInfo(fileName);
            try
            {
                var doc = XmlReader.Read(fileName);

                foreach (var text in XmlReader.ReadTextEnumerable(doc.members))
                    foreach (var simText in text)
                    {
                        if (HasChinese(simText) == false)
                            hash.Add(simText);
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        return hash;
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