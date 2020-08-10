using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace live.Data
{
    public class Bookmaker
    {
        public string name;
        public List<Match> listOfMatches;

        public Bookmaker(string bookName, List<Match> matches)
        {
            name = bookName;
            listOfMatches = matches;
        }

    }
}
