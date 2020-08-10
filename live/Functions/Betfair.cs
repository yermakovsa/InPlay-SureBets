using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using live.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Titanium.Web.Proxy.Models;

namespace live.Functions
{
    class Betfair
    {
		static HashSet<string> blackList = new HashSet<string>(), used = new HashSet<string>();
		static Dictionary<string, List<string>> forks = new Dictionary<string, List<string>>();
		static Dictionary<string, Match> matches = new Dictionary<string, Match>();
		public static bool start = false, check = false;
		static string res = "";
		static Browser browser;
		public static List<HttpHeader> walletHeaders = new List<HttpHeader>();
		public static string walletURL = "";
		static double balance = 15.5;
		static List<double> availableOdds = new List<double>();
		public static string Request(string url, string matchID)
		{
			if (!matches.ContainsKey(matchID) && !url.Contains("was.betfair.com")) {
				return "empty"; 
			}
			var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "GET";
			//httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.97 Safari/537.36";
			if (!url.Contains("was.betfair.com"))
			{
				List<HttpHeader> httpHeaders = matches[matchID].httpHeaders;
				for (int i = 0; i < httpHeaders.Count; i++)
				{
					//if (httpHeaders[i].Name == "Accept-Encoding") continue;
					if (httpHeaders[i].Name == "Host" && url.Contains("ips.betfair.com"))
						httpWebRequest.Headers.Add(httpHeaders[i].Name, "ips.betfair.com");
					else httpWebRequest.Headers.Add(httpHeaders[i].Name, httpHeaders[i].Value);
				}
			}
            else
            {
				for(int i = 0; i < walletHeaders.Count; i++)
                {
					httpWebRequest.Headers.Add(walletHeaders[i].Name, walletHeaders[i].Value);
                }
            }
			httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;
			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var responseText = streamReader.ReadToEnd();
				return responseText;
			}
		}
		public static void Start()
        {
			browser = new Browser();
			Console.ReadLine();
			Task.Run(browser.Start);
			Thread.Sleep(30000);
			Console.WriteLine("START");
			start = true;
            while (true)
            {
				var t = new Dictionary<string, Match>(matches);
				double cnt = t.Count;
				int kk = 0, mm = 0;
				Console.WriteLine("COUNT: " + cnt + " " + DateTime.Now.ToLongTimeString());
				if (cnt == 0 /*|| walletHeaders.Count < 4 || walletURL.Length < 4*/) {
					Console.WriteLine(walletURL);
					Console.WriteLine(walletHeaders.Count());
					Thread.Sleep(10000); 
				}
				else
				{
					foreach (var elem in t)
					{
						string matchID = elem.Key;
						Match match = elem.Value;
						if (match.frst && match.scnd)
						{
							Task.Run(() => Process(match, matchID));
						}
						else Console.WriteLine("NO " + matchID + " " + match.inPlay + " " + match.frst + " " + match.scnd);
						Thread.Sleep((int)(240.0 / cnt));
					}
					Thread.Sleep(60);
					kk++;
					if(kk == 600)
                    {
						Console.WriteLine("REQUEST BALANCE");
						balance = ParseWallet(Request(walletURL, "1"));
						Console.WriteLine("BALANCE: " + balance);
						mm++;
						kk = 0;
                    }
					if(mm == 6)
                    {
						Console.WriteLine("CLEAR, CLEAR, CLEAR!!!");
						Thread.Sleep(10000);
						matches.Clear();
						mm = 0;
                    }
				}


				/*if (res.Length > 4) {
					using (System.IO.StreamWriter sw =
						File.AppendText(@"C:\Users\Entin\Documents\Programs\live\live\bin\Debug\netcoreapp3.1\vilki.txt"))
					{
							sw.Write(res);
					}
					res = "";
				}*/
			}
        }
		static void Process(Match match, string matchID)
        {
			ParseTimeLine(Request(match.urlTime, matchID), match.urlTime);
			if (!matches[matchID].inPlay || int.Parse(matches[matchID].currentMinute) >= 85)
			{
				matches.Remove(matchID);
				browser.AddToDel(matchID);
				return;
			}
			Parse(Request(match.urlOdds, matchID), match.urlOdds, new List<HttpHeader>(), true);
		}
		static double ParseWallet(string s)
        {
			JObject json = JObject.Parse("{\"value\":" + s + "}");
			if (json["value"][0]["details"]["amount"] == null) return 0;
			return double.Parse(json["value"][0]["details"]["amount"].ToString());
		}
		public static void Parse(string s, string url, List<HttpHeader> httpHeaders, bool ch)
        {
			if (s.Length < 10) return;
			List<Bet> bets = new List<Bet>();
            JObject json = JObject.Parse(s);
			if (json["eventTypes"].Count() > 1) return;
			JToken value = json["eventTypes"][0]["eventNodes"][0]["marketNodes"];
			string matchID = json["eventTypes"][0]["eventNodes"][0]["eventId"].ToString();
			if (!matches.ContainsKey(matchID)) return;
			if (!used.Contains(matchID)) Check(matchID);
			foreach (JToken token in value)
            {
                string marketId = token["marketId"].ToString();
				if (blackList.Contains(marketId) || token["runners"] == null) continue;
				if (token["state"]["status"] == null || token["state"]["status"].ToString() != "OPEN") {
					continue; 
				}
				foreach (JToken bet in token["runners"])
				{
					if (bet["exchange"]["availableToBack"] == null || bet["selectionId"] == null) continue;
					bets.Add(GetBet(marketId, bet["selectionId"].ToString(),
						bet["exchange"]["availableToBack"][0]["price"].ToString(),
						bet["exchange"]["availableToBack"][0]["size"].ToString()));
				}
            }
			for(int i = 0; i < bets.Count; i++)
            {
				bets[i].coef = (bets[i].coef - 1) * 0.945 + 1;
				//Console.WriteLine(bets[i].name + " " + bets[i].coef + " " + bets[i].size);
			}
			//Console.ReadLine();
			if(ch) FindForks(bets, matchID);
			if(matches[matchID].urlOdds != url)
            {
				matches[matchID].urlOdds = url;
			}
			if(httpHeaders.Count > 5)
            {
				matches[matchID].httpHeaders = httpHeaders; 
			}
			matches[matchID].frst = true;
		}
		public static void ParseTimeLine(string s, string url)
        {
			JObject json = JObject.Parse(s);
			if (json["eventId"] == null) return;
			string matchID = json["eventId"].ToString();
			if (!matches.ContainsKey(matchID)) matches.Add(matchID, new Match());
			if (json["status"].ToString() != "IN_PLAY") matches[matchID].inPlay = false;
			else matches[matchID].inPlay = true;
			string home = json["score"]["home"]["score"].ToString();
			string away = json["score"]["away"]["score"].ToString();
			string homeName = json["score"]["home"]["name"].ToString();
			string awayName = json["score"]["away"]["name"].ToString();
			matches[matchID].score = home + ":" + away;
			matches[matchID].name = homeName + " v " + awayName;
			matches[matchID].currentMinute = json["timeElapsed"].ToString();
			if (matches[matchID].urlTime != url) matches[matchID].urlTime = url;
			matches[matchID].scnd = true;
		}
		
		static void sendMessage(string text)
		{
			string urlString = "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}";
			string apiToken = "950038087:AAEoJY_FOlnmuBmr6U-sG6BbpVKI5yfoS8o";
			string chatId = "-1001493789616";
			urlString = String.Format(urlString, apiToken, chatId, text);
			WebRequest request = WebRequest.Create(urlString);
			Stream rs = request.GetResponse().GetResponseStream();
			/*StreamReader reader = new StreamReader(rs);
            string line = "";
            StringBuilder sb = new StringBuilder();  urlString "https://api.telegram.org/bot950038087:AAEoJY_FOlnmuBmr6U-sG6BbpVKI5yfoS8o/sendMessage?chat_id=-1001493789616&text=" string

            while (line != null)
            {
                line = reader.ReadLine();
                if (line != null)
                    sb.Append(line);
            }
            string response = sb.ToString();*/
			// Do what you want with response
		}
		static void FindForks(List<Bet> bets, string matchID)
        {
			if (!forks.ContainsKey(matches[matchID].score)) return;
			int cnt = 0;
			List<string> ls = forks[matches[matchID].score];
			foreach(string s in ls)
            {
				HashSet<string> st = new HashSet<string>();
				string[] arr = s.Split('|');
				foreach (string tmp in arr)
					st.Add(tmp);
				double sum = 0, mx = 1.0;
				List<Bet> listOfBets = new List<Bet>();
				foreach(Bet bet in bets)
                {
                    if (st.Contains(bet.name))
                    {
						sum += 1.0 / bet.coef;
						mx = Math.Max(mx, bet.coef);
						st.Remove(bet.name);
						listOfBets.Add(bet);
                    }
                }
				double profit = 1.0 - sum;
				double minBet = 2.0 / ((1.0 / mx) / sum);
				if (st.Count == 0) cnt++;
				if(profit >= 0.005 && profit <= 0.1 && st.Count == 0)
                {
					double mxBet = 0;
					for(int i = 0; i < listOfBets.Count(); i++)
                    {
						bool ch = false;
						double ss = listOfBets[i].size / ((1.0 / mx) / sum);
						for(int j = 0; j < listOfBets.Count(); j++)
                        {
							double need = ((1.0 / listOfBets[j].coef) / sum) * ss;
							if (need > listOfBets[j].size + 0.1 || need < 2) ch = true;
                        }
						if (!ch) mxBet = Math.Max(mxBet, ss);
					}
					if (mxBet < minBet) continue;
					Console.WriteLine("balance: " + balance);
					if (balance < minBet - 0.1) return;
					string a = "[";
					for (int j = 0; j < listOfBets.Count(); j++)
					{
						double need = ((1.0 / listOfBets[j].coef) / sum) * minBet;
						a += GetJsonBet(listOfBets[j].marketID, listOfBets[j].selectionID, Math.Round(need, 2).ToString(),
								GetOdds(Math.Max(1.01, listOfBets[j].coef * 0.9)).ToString());
						if (j != listOfBets.Count - 1) a += ",";
					}
					a += "]";
					string response = JsonRequestBetfair(a, true);
					if (response.ToLower().Contains("failure")) sendMessage("Something get wrong!!!");
					else
					{
						sendMessage("Bet is successful\nprofit: " + Math.Round(profit * 100, 2) + "%\namount: " + Math.Round(minBet, 2) + " EUR");
						balance -= (minBet + 0.1);
					}
					res += a + "\n" + response + "\n";
					using (System.IO.StreamWriter sw =
						File.AppendText(@"C:\Users\Entin\Documents\Programs\live\live\bin\Debug\netcoreapp3.1\vilki.txt"))
					{
						sw.Write(res);
					}
					Environment.Exit(0);
				}
				/*res += "{\n";
				res += " \"date\": " + "\"" + DateTime.Now.ToLongTimeString() +"\",\n";
				res += " \"name\": " + "\"" + matches[matchID].name + "\",\n";
				res += " \"score\": " + "\"" + matches[matchID].score + "\",\n";
				res += " \"minute\": " + "\"" + matches[matchID].currentMinute + "\",\n";
				res += " \"profit\": " + "\"" + Math.Round(profit * 100, 2) + "\",\n";
				res += " \"minBet\": " + "\"" + Math.Round(minBet, 2) + "\",\n";
				res += " \"mxBet\": " + "\"" + Math.Round(mxBet, 2) + "\",\n";
				res += " \"type\": " + "\"" + s + "\"\n";
				res += "},\n";*/
				//sendMessage(s + "\n" + "profit: " + Math.Round(profit * 100, 2) + "%");

			}
			
			Console.WriteLine("number of vilkas: " + cnt + " " + res.Length);
        }
		static void Check(string matchID)
        {
			string json = GetJson("listBets", matchID);
			string response = JsonRequestBetfair(json, false);
			JObject obj = JObject.Parse(response);
			//if (obj["result"] == null) return;
			foreach (JToken token in obj["result"])
            {
				if (token["marketName"].ToString().Contains("Half")) blackList.Add(token["marketId"].ToString());
			}
			used.Add(matchID);
        }
		
		public static string JsonRequestBetfair(string json, bool ok)
		{
			if (check && ok) return "OPA, USHE VSE";
			if (ok) { 
				check = true;
				//return "FIRST TIME";
			}
			var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.betfair.com/exchange/betting/json-rpc/v1");
			httpWebRequest.Method = "POST";
			httpWebRequest.Headers.Add("X-Application", "QvTGCXsIS9JNYASP");
			httpWebRequest.Headers.Add("X-Authentication", "R/4bhtwYNiEyEEdtkK9iXqRYSqzuyXBlpe1soBJYno8=");
			httpWebRequest.ContentType = "application/json";
			//Console.WriteLine(httpWebRequest.Headers + " ");
			using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
			{
				streamWriter.Write(json);
			}
			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var responseText = streamReader.ReadToEnd();
				return responseText;
			}
		}

		public static string GetJson(string info, string id)
		{
			if (info == "listSports")
			{
				return "{\"jsonrpc\": \"2.0\", \"method\": \"SportsAPING/v1.0/listEventTypes\", \"params\": {\"filter\":{ }}, \"id\": 1}";
			}
			else if (info == "listMatches")
			{
				DateTime dateTime = DateTime.Now;
				string today = dateTime.Year + "-" + dateTime.Month + "-" + dateTime.Day;
				dateTime = dateTime.AddDays(1);
				string tomorrow = dateTime.Year + "-" + dateTime.Month + "-" + dateTime.Day;
				return "{\"jsonrpc\": \"2.0\", \"method\": \"SportsAPING/v1.0/listEvents\", \"params\": " +
					"{\"filter\":" +
					"{\"eventTypeIds\":[\"" + id + "\"] }," +
					"\"marketStartTime\": {" +
					"\"from\": \"" + today + "T00:00Z\"," +
					"\"to\": \"" + tomorrow + "T23:59Z\"" +
					"}}, \"id\": 1}";
			}
			else if (info == "listBets")
			{
				return "{\"jsonrpc\": \"2.0\",\"method\": \"SportsAPING/v1.0/listMarketCatalogue\",\"params\": { \"filter\": { \"eventIds\": [\"" + id + "\"]},\"maxResults\": \"200\",\"marketProjection\": [\"RUNNER_METADATA\"]},\"id\": 1}";
			}
			else if (info == "betInfo")
			{
				return "{\"jsonrpc\": \"2.0\",\"method\": \"SportsAPING/v1.0/listMarketBook\",\"params\": {\"marketIds\": [" + id + "],\"priceProjection\": {\"priceData\": [\"EX_BEST_OFFERS\"],  \"virtualise\": \"true\"}},\"id\": 1}";
			}
			return "error: incorrect string 'INFO'";
		}
		public static string GetJsonBet(string marketId, string selectionId, string size, string price)
        {
			return "{ \"jsonrpc\": \"2.0\", \"method\": \"SportsAPING/v1.0/placeOrders\", \"params\": { \"marketId\":\"" + marketId + "\", \"instructions\": [ { \"selectionId\": \"" + selectionId + "\", \"handicap\": \"0\", \"side\": \"BACK\", \"orderType\": \"LIMIT\", \"limitOrder\": { \"size\":\"" + size + "\", \"price\": \"" + price + "\", \"persistenceType\": \"PERSIST\" } } ] }, \"id\": 1 }";
        }
		static Bet GetBet(string marketId, string id, string coef, string size)
		{
			Bet bet = new Bet("entin", 1, 1, marketId, id);
			if (id == "47972")
			{
				bet = (new Bet("Under 2.5", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "47973")
			{
				bet = (new Bet("Over 2.5", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "1222347")
			{
				bet = (new Bet("Under 4.5", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "1222346")
			{
				bet = (new Bet("Over 4.5", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "1221385")
			{
				bet = (new Bet("Under 1.5", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "1221386")
			{
				bet = (new Bet("Over 1.5", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "1222344")
			{
				bet = (new Bet("Under 3.5", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "1222345")
			{
				bet = (new Bet("Over 3.5", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "5851482")
			{
				bet = (new Bet("Under 0.5", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "5851483")
			{
				bet = (new Bet("Over 0.5", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "1")
			{
				bet = (new Bet("0:0", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "2")
			{
				bet = (new Bet("1:0", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "3")
			{
				bet = (new Bet("1:1", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "4")
			{
				bet = (new Bet("0:1", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "5")
			{
				bet = (new Bet("2:0", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "6")
			{
				bet = (new Bet("2:1", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "7")
			{
				bet = (new Bet("2:2", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "8")
			{
				bet = (new Bet("1:2", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "9")
			{
				bet = (new Bet("0:2", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "10")
			{
				bet = (new Bet("3:0", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "11")
			{
				bet = (new Bet("3:1", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "12")
			{
				bet = (new Bet("3:2", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "13")
			{
				bet = (new Bet("3:3", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "14")
			{
				bet = (new Bet("2:3", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "15")
			{
				bet = (new Bet("1:3", double.Parse(coef), double.Parse(size), marketId, id));
			}
			else if (id == "16")
			{
				bet = (new Bet("0:3", double.Parse(coef), double.Parse(size), marketId, id));
			}
			return bet;
		}
		static double GetOdds(double a)
        {
			for (int i = 1; i < availableOdds.Count; i++)
				if (availableOdds[i] > a && availableOdds[i] - a < a - availableOdds[i - 1]) return availableOdds[i];
				else if(availableOdds[i] > a) return availableOdds[i - 1];
			return 1.01;
        }
		public static void Init()
        {

			string s = File.ReadAllText("C:\\Users\\Entin\\Documents\\Programs\\live\\live\\bin\\Debug\\netcoreapp3.1\\odds.txt");
			string[] arr = s.Replace(" ", "").Split(',');
			foreach (string a in arr)
				availableOdds.Add(double.Parse(a));
			availableOdds.Add(10000);
			List<string> ls = new List<string>();
			
			// 0:0
			ls.Add("Over 0.5|0:0");
			ls.Add("Over 1.5|1:0|0:1|0:0");
            ls.Add("Over 2.5|1:0|0:1|2:0|0:2|1:1|0:0");
			forks.Add("0:0", new List<string>(ls));
			ls.Clear();

			// 1:0
			ls.Add("Over 1.5|1:0");
			ls.Add("Over 2.5|1:1|2:0|1:0");
			ls.Add("Over 3.5|3:0|2:1|1:2|2:0|1:1|1:0");
			ls.Add("Over 3.5|Under 1.5|3:0|2:1|1:2|2:0|1:1");
			ls.Add("Over 3.5|Under 2.5|3:0|2:1|1:2");
			forks.Add("1:0", new List<string>(ls));
			ls.Clear();

			// 0:1
			ls.Add("Over 1.5|0:1");
			ls.Add("Over 2.5|1:1|0:2|0:1");
			ls.Add("Over 3.5|0:3|1:2|2:1|0:2|1:1|0:1");
			ls.Add("Over 3.5|Under 1.5|0:3|1:2|2:1|1:1|0:2");
			ls.Add("Over 3.5|Under 2.5|0:3|1:2|2:1");
			forks.Add("0:1", new List<string>(ls));
			ls.Clear();
			
			// 1:1
			ls.Add("Over 2.5|1:1");
			ls.Add("Over 3.5|2:1|1:2|1:1");
			ls.Add("Over 4.5|3:1|1:3|2:2|2:1|1:2|1:1");
			ls.Add("Over 4.5|Under 2.5|2:1|1:2|3:1|1:3|2:2");
			ls.Add("Over 4.5|Under 3.5|3:1|1:3|2:2");
			forks.Add("1:1", new List<string>(ls));
			ls.Clear();

			// 2:0
			ls.Add("Over 2.5|2:0");
			ls.Add("Over 3.5|2:1|2:0|3:0");
			ls.Add("Over 4.5|4:0|3:1|2:2|2:1|2:0|3:0");
			ls.Add("Over 3.5|Under 2.5|2:1|3:0");
			forks.Add("2:0", new List<string>(ls));
			ls.Clear();

			// 0:2
			ls.Add("Over 2.5|0:2");
			ls.Add("Over 3.5|1:2|0:3|0:2");
			ls.Add("Over 4.5|0:4|1:3|2:2|1:2|0:2|0:3");
			ls.Add("Over 3.5|Under 2.5|0:3|1:2");
			forks.Add("0:2", new List<string>(ls));
			ls.Clear();

			// 2:1
			ls.Add("Over 3.5|2:1");
			ls.Add("Over 4.5|2:2|3:1|2:1");
			ls.Add("Over 5.5|4:1|2:3|2:2|3:1|2:1");
			ls.Add("Over 4.5|Under 3.5|3:1|2:2");
			forks.Add("2:1", new List<string>(ls));
			ls.Clear();

			// 1:2
			ls.Add("Over 3.5|1:2");
			ls.Add("Over 4.5|2:2|1:3|1:2");
			ls.Add("Over 5.5|1:4|3:2|2:2|1:3|1:2");
			ls.Add("Over 4.5|Under 3.5|1:3|2:2");
			forks.Add("1:2", new List<string>(ls));
			ls.Clear();

			// 1:3
			ls.Add("Over 4.5|1:3");
			forks.Add("1:3", new List<string>(ls));
			ls.Clear();

			// 3:1
			ls.Add("Over 4.5|3:1");
			forks.Add("3:1", new List<string>(ls));
			ls.Clear();

			// 2:2
			ls.Add("Over 4.5|2:2");
			ls.Add("Over 5.5|3:2|2:3|2:2");
			forks.Add("2:2", new List<string>(ls));
			ls.Clear();

			// 3:2
			ls.Add("Over 5.5|3:2");
			forks.Add("3:2", new List<string>(ls));
			ls.Clear();

			// 2:3
			ls.Add("Over 5.5|2:3");
			forks.Add("2:3", new List<string>(ls));
			ls.Clear();

			// 3:3
			ls.Add("Over 6.5|3:3");
			forks.Add("3:3", new List<string>(ls));
			ls.Clear();

		}
	}
}
