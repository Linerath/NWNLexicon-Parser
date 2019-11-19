//using HtmlAgilityPack;
using Fizzler;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HtmlParser
{
    class Program
    {
        static void Main(string[] args)
        {
            ParseNwnLexicon();

        }

        static void ParseNwnLexicon()
        {
            String uri = "https://nwnlexicon.com/index.php?title=Category:Resources_Items";

            HtmlWeb web = new HtmlWeb();

            var htmlDoc = web.Load(uri);

            var rows = htmlDoc.DocumentNode.QuerySelectorAll("table tr");

            List<ItemCategory> items = new List<ItemCategory>(rows.Count());
            foreach (var r in rows)
            {
                var a = r.QuerySelector("a");

                if (a == null)
                    continue;

                ItemCategory item = new ItemCategory
                {
                    Category = a.InnerText.Replace("\n", ""),
                };

                item.CategoryDescription = r.QuerySelectorAll("td").LastOrDefault().InnerText.Replace("\n", "");
                item.Href = a.Attributes["href"]?.Value;

                items.Add(item);
            }

            foreach (var itemCategory in items)
            {
                itemCategory.Items = ParseItems(itemCategory.Href);

                //Console.WriteLine($"{item.Category} \t {item.CategoryDescription} \t {item.Href}");
            }

            foreach (var itemCat in items)
            {
                String str = $"{itemCat.CategoryDescription}\n\n";

                foreach (var item in itemCat.Items)
                {
                    str += $"{item.Name} ({item.ResRef} | {item.Tag}) {item.GPValue}\n";
                }

                itemCat.Category = itemCat.Category.Replace("/", "");
                itemCat.Category = itemCat.Category.Replace(@"\", "");

                File.WriteAllText($@"D:\Other\NWN\Ignis Mare\aurora\loot parser\{itemCat.Category}.txt", str);
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
                    Name = tds[0].InnerText.Replace("\n", ""),
                    ResRef = tds[1].InnerText.Replace("\n", ""),
                    Tag = tds[2].InnerText.Replace("\n", ""),
                    GPValue = tds[3].InnerText.Replace("\n", ""),
                };

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
        public String GPValue { get; set; }
    }
}
