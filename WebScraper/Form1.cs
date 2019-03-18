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
            table.Columns.Add("ListingPrice", typeof(double));
            table.Columns.Add("RealPrice", typeof(double));
            table.Columns.Add("Potencial ROI", typeof(double));
            table.Columns.Add("ROI in %", typeof(double));
            dataGridView1.DataSource = table;
            dataGridView1.Columns[0].Width = 200;
        }

        private async Task<List<Item>> ListingsFromPage(int pageNum)
        {
            string url = @"https://backpack.tf/classifieds?page="+ pageNum.ToString() + "&quality=5&slot=misc";
            //&particle=3004%2C91%2C59%2C3017%2C98%2C82%2C81%2C19%2C4%2C38%2C75%2C30%2C58%2C121%2C39%2C92%2C73%2C13%2C11%2C69%2C10
            //6 %2C60%2C90%2C94%2C67%2C100%2C80%2C704%2C103%2C62%2C703%2C111%2C34%2C105%2C40%2C37%2C79%2C18%2C3018%2C8%2C3003%2C10
            //2 %2C701%2C3015%2C85%2C3005%2C3012%2C45%2C3013%2C101%2C119%2C115%2C84%2C70%2C17%2C57%2C118%2C110%2C77%2C109%2C93%2C64
            //%2C83%2C29%2C47%2C116%2C96%2C16%2C108%2C36%2C3014%2C3001%2C88%2C12%2C89%2C3010%2C28%2C3016%2C9%2C113%2C46%2C95%2C44%2
            //C63 %2C3022%2C10%2C15%2C3006%2C7%2C31%2C66%2C107%2C3008%2C43%2C56%2C61%2C104%2C32%2C68%2C14%2C35%2C3007%2C3021%2C72%2
            //C3019 %2C65%2C33%2C99%2C112%2C120%2C76%2C117%2C3020%2C74%2C3009%2C114%2C78%2C71%2C97%2C86%2C87%2C6%2C702%2C3011
            label2.Text = "Currently indexing page " + pageNum.ToString();
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
                // Crashina ant unusilifieriu ir effect
                effects.Add(doc.DocumentNode.SelectSingleNode(" //*[@id=\"" + ids[id] + "\"]/div[1]/div").Attributes["data-effect_name"].Value);
                id++;
            }

            List<Item> toReturn = new List<Item>();
            for (int i = 0; i < ids.Count(); ++i)
                toReturn.Add(new Item() {ID = ids[i], HatBaseName = titles[i], AdPrice = prices[i], RealPrice = bptfPrices[i], Effect = effects[i] });
                
            return toReturn;
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

        public static double profitPercentage(double Ad, double Real)
        {
            if (Real != 0)
            {
                return Math.Round((1 - (Ad / Real) ) * 100, 2);
            }
            else
            {
                return 0;
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
        List<string> GreatDealIDs = new List<string>();
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
                double profitPercent = profitPercentage(finalAdPrice, finalRealPrice);
                if (profitPercent > 25){

                    if(finalAdPrice <= maxBudget){

                        if(!GreatDealIDs.Contains(listing.ID)){
                            table.Rows.Add(fullName, finalAdPrice, finalRealPrice, profitMargin, profitPercent);
                            GreatDealIDs.Add(listing.ID);
                        }
                    } else {
                        //Viskas is naujo, ieskom nauju
                        pageNum = int.Parse(textBox2.Text);
                    }
                }   
            }
        }
        bool on = false;
        double maxBudget = 0;
        private void button1_Click(object sender, EventArgs e)
        {
            if (!on) {
                timer1.Enabled = true;
                on = true;
                button1.BackColor = System.Drawing.Color.Red;
                button1.Text = "STOP";
                timer1.Interval = int.Parse(textBox1.Text);
                pageNum = int.Parse(textBox2.Text);
                maxBudget = double.Parse(textBox3.Text);
                textBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox3.Enabled = false;
            } else {
                timer1.Enabled = false;
                on = false;
                button1.BackColor = System.Drawing.Color.LimeGreen;
                button1.Text = "RESUME";
                textBox2.Text = pageNum.ToString();
                textBox1.Enabled = true;
                textBox2.Enabled = true;
                textBox3.Enabled = true;
            }
        }
    }
    public class Item
    {
        public string ID { get; set; }
        public string HatBaseName { get; set; }
        public string AdPrice { get; set; }
        public string RealPrice { get; set; }
        public string Effect { get; set; }
        public string Link { get; set; }
    }
}