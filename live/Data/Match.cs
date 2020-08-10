using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy.Models;

namespace live.Data
{
    public class Match
    {
        public string name = "entin";
        public string urlOdds = "https://www.betfair.com/exchange/plus/en/";
        public string urlTime = "https://www.betfair.com/exchange/plus/en/";
        public string score = "0:0";
        public string currentMinute = "";
        public bool frst = false;
        public bool scnd = false;
        public bool inPlay = false;
        public List<HttpHeader> httpHeaders = new List<HttpHeader>();
    }
}
