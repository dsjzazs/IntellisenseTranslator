using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intellisense汉化工具
{
    /// <summary>
    /// xml文件读取器
    /// </summary>
    public static class XmlReader
    {
        public static doc Read(string fileName)
        {
            var factory = new System.Xml.Serialization.XmlSerializerFactory();
            var serial = factory.CreateSerializer(typeof(doc));
            using (var openStream = System.IO.File.Open(fileName, System.IO.FileMode.Open))
            {
                var obj = (doc)serial.Deserialize(openStream);
                return obj;
            }
        }
        public static IEnumerable<string[]> ReadTextEnumerable(IEnumerable<docMember> members)
        {
            foreach (var item in members)
            {
                if (item.summary?.para?.Text != null)
                    yield return item.summary.para.Text;
                if (item.param != null)
                {
                    foreach (var param in item.param)
                        if (param.Text != null)
                            yield return param.Text;
                }
                if (item.returns?.Text != null)
                    yield return item.returns.Text;
                if (item.value?.Text != null)
                    yield return item.value.Text;
            }
        }
    }
}
