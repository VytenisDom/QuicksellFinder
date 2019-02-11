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
            table.Columns.Add("Potencial ROI", typeof(double));
            table.Columns.Add("ROI in %", typeof(string));
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
        }

        public static string profit(double Ad, double Real)
        {
            if(Real != 0){
                double b = Math.Round(Real - Ad, 2);
                return b.ToString();
            } else {
                return "0";
            }
           
        }

        public static string profitPercentage(double Ad, double Real)
        {
            if (Real != 0)
            {
                double b = Math.Round((1 - (Ad / Real) ) * 100, 2);
                return b.ToString();
            }
            else
            {
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
        int pageNum = 0;
        private async void timer1_Tick(object sender, EventArgs e)
        {
            pageNum++;
            var Listings = await ListingsFromPage(pageNum);
            foreach (var listing in Listings)
            {
                string fullName = listing.Effect + " " + listing.HatBaseName;
                double finalAdPrice = finalPrice(listing.AdPrice);
                double finalRealPrice = finalPrice(listing.RealPrice);
                string profitMargin = profit(finalAdPrice, finalRealPrice);
                string profitPercent = profitPercentage(finalAdPrice, finalRealPrice);
                //if(Double.Parse(profitPercent) > 0)
                table.Rows.Add(fullName, finalAdPrice, finalRealPrice, profitMargin, profitPercent + "%");
            }
        }
        bool on = false;
        private void button1_Click(object sender, EventArgs e)
        {
            if (!on) {
                timer1.Enabled = true;
                on = true;
                button1.BackColor = System.Drawing.Color.Red;
                button1.Text = "STOP";
                timer1.Interval = int.Parse(textBox1.Text);
                textBox1.Enabled = false;
            } else {
                timer1.Enabled = false;
                on = false;
                button1.BackColor = System.Drawing.Color.LimeGreen;
                button1.Text = "START";
                textBox1.Enabled = true;
            }
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