//#define STRING
//#define SERIALIZE

using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace HtmlParser
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Press enter to start");

            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey();
            }
            while (!(key.Modifiers == 0 && key.Key == ConsoleKey.Enter));

            Console.Clear();
            Console.WriteLine("Processing...");

            ParseNwnLexicon();
            //System.Threading.Thread.Sleep(2000);

            Console.WriteLine("\nDone!\n(press any key)");
            Console.ReadKey();
        }

        static void ParseNwnLexicon()
        {
            String path = $"{DebugHelper.PATH}nwnlexicon.txt";

#if !STRING
            String uri = "https://nwnlexicon.com/index.php?title=Category:Resources_Items";

            HtmlWeb web = new HtmlWeb();

            HtmlDocument htmlDoc = web.Load(uri);

#if SERIALIZE
            String html = htmlDoc.Text;
            File.WriteAllText(path, html);
#endif
#else
            String html = File.ReadAllText(path);

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
#endif

            var rows = htmlDoc.DocumentNode.QuerySelectorAll("table tr");

            List<ItemCategory> items = new List<ItemCategory>(rows.Count());
            foreach (var r in rows)
            {
                var a = r.QuerySelector("a");

                if (a == null)
                    continue;

                ItemCategory item = new ItemCategory
                {
                    Category = a.InnerText.Replace("\n", "").Trim(),
                };

                item.CategoryDescription = r.QuerySelectorAll("td").LastOrDefault().InnerText.Replace("\n", "").Trim();
                item.Href = a.Attributes["href"]?.Value;

                items.Add(item);
            }

            foreach (var itemCategory in items)
            {
                Console.Write($"...{itemCategory.Category}... ");
                itemCategory.Items = ParseItems(itemCategory.Href).OrderBy(x => x.GPValue).ToList();
                Console.WriteLine("+");
            }

            foreach (var itemCat in items)
            {
                String[] lines = new String[itemCat.Items.Count * 3 + 1];
                int i = 0;

                lines[i++] = $"{itemCat.CategoryDescription}\n\n";

                int num = 1;
                foreach (var item in itemCat.Items)
                {
                    String space = num < 10
                        ? "   "
                        : "\t";

                    lines[i++] = $"{num++}) {item.Name}";
                    lines[i++] = $"{space}{item.Tag}   {item.ResRef}";
                    lines[i++] = $"{space}{item.GPValue}\n";
                }

                itemCat.Category = itemCat.Category.Replace("/", "").Replace(@"\", "").Trim();

                File.WriteAllLines($@"D:\Other\NWN\Ignis Mare. Design\aurora\loot\nwnlexicon (default)\{itemCat.Category}.txt", lines);
            }
        }

        static List<Item> ParseItems(String uri)
        {
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load($"https://nwnlexicon.com{uri}");

            List<Item> result = new List<Item>();

            var rows = htmlDoc.DocumentNode.QuerySelectorAll("table tr");

            foreach (var r in rows)
            {
                var tds = r.QuerySelectorAll("td").ToList();

                if (tds.Count != 4)
                    continue;

                Item item = new Item
                {
                    Name = tds[0].InnerText.Replace("\n", "").Trim(),
                    ResRef = tds[1].InnerText.Replace("\n", "").Trim(),
                    Tag = tds[2].InnerText.Replace("\n", "").Trim(),
                };

                if (uint.TryParse(tds[3].InnerText.Replace("\n", "").Replace(",", "").Trim(), out uint price))
                    item.GPValue = price;

                result.Add(item);
            }

            return result;
        }
    }

    internal class ItemCategory
    {
        public String Href { get; set; }
        public String Category { get; set; }
        public String CategoryDescription { get; set; }
        public List<Item> Items { get; set; } = new List<Item>();
    }

    internal class Item
    {
        public String Name { get; set; }
        public String ResRef { get; set; }
        public String Tag { get; set; }
        public uint GPValue { get; set; }
    }

    static class DebugHelper
    {
        public const String PATH = @"C:\StringData\";

        public static void Serialize<T>(T obj, String fileName, String path = PATH)
        {
            path = ConcatPath(path, fileName);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, obj);
                String resultStr = textWriter.ToString();
                File.WriteAllText(path, resultStr);
            }
        }

        public static T Deserialize<T>(String fileName, String path = PATH)
        {
            path = ConcatPath(path, fileName);

            if (!File.Exists(path))
                return default(T);

            String str = File.ReadAllText(path);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader textReader = new StringReader(str))
            {
                T obj = (T)xmlSerializer.Deserialize(textReader);

                return obj;
            }
        }

        static String ConcatPath(String path, String fileName)
        {
            if (String.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));
            if (String.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException(nameof(fileName));

            if (!path.EndsWith("\\"))
                path += "\\";

            if (!Regex.IsMatch(fileName, @".+\.\w+", RegexOptions.IgnoreCase))
                fileName += ".txt";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path += fileName;

            return path;
        }
    }
}
