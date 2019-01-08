using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {

            List<string> urls = new List<string>();
            urls.Add("https://www.facebook.com/groups/ventas.negocios/");
            urls.Add("https://www.facebook.com/groups/459471367481129/");
            urls.Add("https://www.facebook.com/groups/412606125424610/");
            urls.Add("https://www.facebook.com/groups/550777368267732/");
            urls.Add("https://www.facebook.com/groups/1792822610978168/");
            urls.Add("https://www.facebook.com/groups/538567392860618");
            urls.Add("https://www.facebook.com/groups/usadoscomonuevos/");
            urls.Add("https://www.facebook.com/groups/247470108932994/");

            foreach (var url in urls)
            {
                string content = GetFacebookContent(url);
                var results = GetResult(content, url);
                InsertResults(results);
            }

            Console.WriteLine("Done.");
        }

        static string GetFacebookContent(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            //<div class="mtm">(.*?)class="_2lc5"
            //<p>(.*?)</p>
            //<div class=\"_l53\"><span class=\"_5s8v\"></span><span>(.*?)</div>

            return responseString;
        }

        static List<Result> GetResult(string content, string groupName)
        {
            //MatchCollection matches = Regex.Matches(content, "<div class=\"mtm\">(.*?)class=\"_2lc5\"");
            MatchCollection matchesMain = Regex.Matches(content, "<div class=\"_1dwg _1w_m _q7o\">.*</div>");

            List<Result> resultados = new List<Result>();
            foreach (var match in matchesMain)
            {
                MatchCollection matchName = Regex.Matches(match.ToString(), "<span class=\"_39_n\">.*?</span>");
                MatchCollection matchesTitle = Regex.Matches(match.ToString(), "<div class=\"_l53\"><span class=\"_5s8v\"></span><span>(.*?)</div>");
                MatchCollection matchesDesc = Regex.Matches(match.ToString(), "<div data-ad-preview=\"message\" class=\"_5pbx userContent _[0-9]{4}\" data-ft=\".*?<p>.*?</div>");
                MatchCollection matchesPrice = Regex.Matches(match.ToString(), "<div class=\"_l57\">(.*?)</div>");
                MatchCollection matchesLocation = Regex.Matches(match.ToString(), "<div class=\"_l58\">(.*?)</div>");

                try
                {
                    Result result = new Result();
                    result.publisherName = ReplaceAllHTMLTags(matchName[0].ToString());
                    result.title = ReplaceAllHTMLTags(matchesTitle[0].ToString());
                    result.description = ReplaceAllHTMLTags(matchesDesc[0].ToString());
                    result.price = ReplaceAllHTMLTags(matchesPrice[0].ToString());
                    result.location = ReplaceAllHTMLTags(matchesLocation[0].ToString());

                    //has Phone
                    var matchesPhoneDesc = Regex.Matches(result.description, "3[0-9]{9}");
                    if (matchesPhoneDesc.Count > 0){
                        result.phone = matchesPhoneDesc[0].ToString();
                    }

                    result.groupName = groupName;
                    resultados.Add(result);
                }
                catch (Exception ex)
                {

                }
            }

            return resultados;
        }

        public static string ReplaceAllHTMLTags(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

        static void InsertResults(List<Result> results)
        {
            var sql_con = new SQLiteConnection("Data Source=C:\\scrapper\\database.sqlite;Version=3;");
            sql_con.Open();

            foreach(var result in results)
            {

                string existQuery = $"SELECT * FROM posts WHERE title = '{result.title}'";
                SQLiteCommand existCommand = new SQLiteCommand(existQuery, sql_con);
                SQLiteDataReader reader = existCommand.ExecuteReader();

                if (reader.HasRows){
                    continue;
                }

                string sqlquery = $@"INSERT INTO posts 
                    (id,
                      title,
                      [desc],
                      [group],
                       price,
                       publishername,
                       phone,
                       location)
                  VALUES(
                      NULL,
                      '{result.title}',
                      '{result.description}',
                      '{result.groupName}',
                      '{result.price}',
                      '{result.publisherName}',
                      '{result.phone}',
                      '{result.location}'
                  );";

                SQLiteCommand insertSQL = new SQLiteCommand(sqlquery, sql_con);
                try
                {
                    insertSQL.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }


            };
        }
    }

    public class Result
    {
        public string publisherName;
        public string title;
        public string description;
        public string groupName;
        public string price;
        public string location;
        public string phone;
    }
}
