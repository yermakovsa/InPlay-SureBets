using live.Data;
using live.Functions;
using System;
using System.Net;

namespace live
{
    class Program
    {
        static PServer pServer = new PServer();
        static void Main(string[] args)
        {
            Console.WriteLine("START " + DateTime.Now.ToLongTimeString());
            ServicePointManager.DefaultConnectionLimit = 1000;
            Betfair.Init();
            pServer.Start();
            //Console.WriteLine(Betfair.GetJsonBet("1.171685987", "1222347", "0.1", "1.01"));
            //Console.WriteLine("Start2: " + DateTime.Now.ToLongTimeString());
            //Console.WriteLine((Betfair.JsonRequestBetfair("[" + Betfair.GetJsonBet("1.171775699", "3806543", "1", "20.01") + "]")));
            //Console.WriteLine("End2: " + DateTime.Now.ToLongTimeString());
            Betfair.Start();
            Console.ReadLine();
        }
    }
}
