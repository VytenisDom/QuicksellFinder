using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Score", typeof(string));
            table.Columns.Add("NumReviews", typeof(string));
            dataGridView1.DataSource = table;
        }

        private async Task<List<GameReview>> GameRankingsFromPage(int pageNum)
        {
            string url = @"https://backpack.tf/classifieds";
            
            var doc = await Task.Factory.StartNew(() => web.Load(url));
            var noderrrr = doc.DocumentNode.SelectSingleNode("//head/title");
            var nameNodes = doc.DocumentNode.SelectNodes("//*[@id=\"page-content\"]/div[4]/div[1]/div/div[2]/ul");
            string[] ids = new string[11];
            var id = 0;
            foreach (var nNode in nameNodes.Descendants("li"))
            {
                if (nNode.NodeType == HtmlNodeType.Element)
                {
                    ids[id] = nNode.GetAttributeValue("id", "").ToString();
                    id++;
                }
            }


            var scoreNodes = doc.DocumentNode.SelectNodes("//*[@id=\"page-content\"]/div[3]/div[1]/div/div[1]/span[1]");
            var numReviewNodes = doc.DocumentNode.SelectNodes("//*[@id=\"page-content\"]/div[3]/div[1]/div/div[1]/span[1]");
            //If these are null it means the name/score nodes couldn't be found on the html page
            if (nameNodes == null || scoreNodes == null || numReviewNodes == null)
                return new List<GameReview>();

            var names = nameNodes.Select(node => node.InnerText).ToList();
            var scores = scoreNodes.Select(node => node.InnerText).ToList();
            var numReviews = numReviewNodes.Select(node => node.LastChild.InnerText).ToList();

            List<GameReview> toReturn = new List<GameReview>();
            for (int i = 0; i < names.Count(); ++i)
                toReturn.Add(new GameReview() { Name = names[i], Score = scores[i], NumReviews = numReviews[i] });

            return toReturn;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            int pageNum = 0;
            var rankings = await GameRankingsFromPage(0);
            while (rankings.Count > 0)
            {
                foreach (var ranking in rankings)
                    table.Rows.Add(ranking.Name, ranking.Score, ranking.NumReviews);
                pageNum++;
                rankings = await GameRankingsFromPage(pageNum);
            }
        }
    }
    public class GameReview
    {
        public string Name { get; set; }
        public string Score { get; set; }
        public string NumReviews { get; set; }
    }
}