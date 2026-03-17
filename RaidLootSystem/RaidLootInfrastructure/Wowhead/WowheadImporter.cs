using HtmlAgilityPack;
using RaidLootCore.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace RaidLootInfrastructure.Wowhead
{
    public class WowheadImporter
    {
        public async Task<List<Item>> LoadIccLoot()
        {
            var items = new List<Item>();

            string url = "https://www.wowhead.com/wotlk/zone=4812/icecrown-citadel";

            HttpClient client = new HttpClient();
            var html = await client.GetStringAsync(url);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'/item=')]");

            if (nodes == null)
                return items;

            foreach (var node in nodes)
            {
                string name = node.InnerText;
                string link = "https://www.wowhead.com" + node.GetAttributeValue("href", "");

                items.Add(new Item
                {
                    Name = name,
                    WowheadLink = link
                });
            }

            return items;
        }
    }
}