using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace WebScraper
{
    public partial class Form1 : Form
    {
        DataTable table;
        HtmlWeb web = new HtmlWeb();
        public Form1()
        {
            InitializeComponent();
            InitTable();
        }

        private void InitTable()
        {
            table = new DataTable("GameRankingsDataTable");
            table.Columns.Add("Title", typeof(string));
            table.Columns.Add("ListingPrice", typeof(string));
            table.Columns.Add("RealPrice", typeof(string));
            table.Columns.Add("Profit", typeof(string));
            dataGridView1.DataSource = table;
            dataGridView1.Columns[0].Width = 200;
        }

        private async Task<List<Item>> ListingsFromPage(int pageNum)
        {
            string url = @"https://backpack.tf/classifieds?page="+ pageNum.ToString() +"&slot=misc&quality=5";
            
            var doc = await Task.Factory.StartNew(() => web.Load(url));
            var idNodes = doc.DocumentNode.SelectNodes("//*[@id=\"page-content\"]/div[4]/div[1]/div/div[2]/ul");
            List<string> ids = new List<string>();
            List<string> prices = new List<string>();
            List<string> titles = new List<string>();
            List<string> effects = new List<string>();
            List<string> bptfPrices = new List<string>();
            var id = 0;
            foreach (var liNode in idNodes.Descendants("li"))
            {
                ids.Add(liNode.GetAttributeValue("id", "").ToString());
                try{
                    bptfPrices.Add(doc.DocumentNode.SelectSingleNode(" //*[@id=\"" + ids[id] + "\"]/div[1]/div").Attributes["data-p_bptf"].Value);
                } catch {
                    bptfPrices.Add("-");
                }
                prices.Add(doc.DocumentNode.SelectSingleNode(" //*[@id=\"" + ids[id] + "\"]/div[1]/div").Attributes["data-listing_price"].Value);
                titles.Add(doc.DocumentNode.SelectSingleNode(" //*[@id=\"" + ids[id] + "\"]/div[1]/div").Attributes["data-base_name"].Value);
                effects.Add(doc.DocumentNode.SelectSingleNode(" //*[@id=\"" + ids[id] + "\"]/div[1]/div").Attributes["data-effect_name"].Value);
                id++;
            }

            List<Item> toReturn = new List<Item>();
            for (int i = 0; i < ids.Count(); ++i)
                toReturn.Add(new Item() { HatBaseName = titles[i], AdPrice = prices[i], RealPrice = bptfPrices[i], Effect = effects[i] });
                
            return toReturn;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            int pageNum = 1;
            var Listings = await ListingsFromPage(1);
            while (Listings.Count > 0){
                foreach (var listing in Listings){
                    string fullName = listing.Effect + " " + listing.HatBaseName;
                    double finalAdPrice = finalPrice(listing.AdPrice);
                    double finalRealPrice = finalPrice(listing.RealPrice);
                    string profitMargin = profit(finalAdPrice, finalRealPrice);
                    table.Rows.Add(fullName, finalAdPrice, finalRealPrice, profitMargin);
                }
                pageNum++;
                Listings = await ListingsFromPage(pageNum);
            }
        }

        public static string profit(double Ad, double Real)
        {
            if(Real != 0){
                double b = Math.Round(Real - Ad, 2);
                return b.ToString();
            } else {
                return "Unknown";
            }
           
        }

        public static double finalPrice(string str)
        {
            double ratio = 44.11;
            double final = 0;
            double amount_in_keys = 0;
            double amount_in_ref = 0;
            if (str.Contains("–"))
            {
                int index = str.IndexOf("–");
                final = (getDouble(str.Substring(0, index)) + getDouble(str.Substring(index + 1))) / 2;

            }
            else if (str.Contains("keys") && str.Contains("ref"))
            {
                int index = str.IndexOf("keys");
                index += 5;
                amount_in_keys = getDouble(str.Substring(0, index));
                amount_in_ref = getDouble(str.Substring(index));
                final = amount_in_keys + convert(amount_in_ref, ratio);
            }
            else if (str.Contains("keys"))
            {
                final = getDouble(str);
            }
            else
            {
                final = convert(getDouble(str), ratio);
            }

            return final;
        }
        public static double getDouble(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || c == ' ' || c == '.')
                {
                    sb.Append(c);
                }
                else break;
            }
            try
            {
                return Double.Parse(sb.ToString(), CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
           
        }
        public static double convert(double refined, double ratio)
        {
            return Math.Round(refined / ratio, 2);
        }
    }
    public class Item
    {
        public string HatBaseName { get; set; }
        public string AdPrice { get; set; }
        public string RealPrice { get; set; }
        public string Effect { get; set; }
        public string Link { get; set; }
    }
}