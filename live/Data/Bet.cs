using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace live.Data
{
    public class Bet
    {
        public string name;
        public double coef;
        public double size;
        public string marketID;
        public string selectionID;

        public Bet(string BetName, double BetCoef, double BetSize, string marketId, string selectionId)
        {
            name = BetName;
            coef = BetCoef;
            size = BetSize;
            marketID = marketId;
            selectionID = selectionId;
        }
    }
}
