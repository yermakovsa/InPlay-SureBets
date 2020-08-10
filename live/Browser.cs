using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace live
{
    class Browser
    {
        IWebDriver driver;
        string path, simple;
        List<string> toDelete, tabs, used;
        int numberOfTabs = 3;
        HashSet<string> usedLinks = new HashSet<string>();
        public Browser()
        {
            toDelete = new List<string>();
            path = AppDomain.CurrentDomain.BaseDirectory + '\\';
            simple = "https://www.york.ac.uk/teaching/cws/wws/webpage1.html";

            ChromeOptions options = new ChromeOptions();
            //options.PageLoadStrategy = PageLoadStrategy.None;
            options.AddArguments("--proxy-server=127.0.0.1:8000");
            //options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
            //options.AddArguments("headless");
            //options.AddArguments("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36");
            driver = new ChromeDriver(path, options, TimeSpan.FromSeconds(300));
            driver.Navigate().GoToUrl("https://www.betfair.com/exchange/plus/inplay/football");
            driver.Manage().Window.Maximize();
        }
        public void AddToDel(string id)
        {
            toDelete.Add(id);
        }
        public void Delete()
        {
            for (int i = 0; i < toDelete.Count(); i++)
            {
                for (int j = 0; j < tabs.Count - 1; j++)
                    if (used[j] == toDelete[i])
                    {
                        driver.SwitchTo().Window(tabs[j + 1]);
                        driver.Navigate().GoToUrl(simple);
                        used[j] = "empty";
                        toDelete.RemoveAt(i);
                        i--;
                        break;
                    }
            }
            Console.WriteLine("toDelete: " + toDelete.Count());
        }
        public void Start()
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            for (int j = 0; j < numberOfTabs; j++)
            {
                js.ExecuteScript("window.open()");
            }
            tabs = new List<String>(driver.WindowHandles);
            used = new List<string>(); ;
            List<IWebElement> elements;
            for (int j = 0; j < tabs.Count - 1; j++)
            {
                driver.SwitchTo().Window(tabs[j + 1]);
                driver.Navigate().GoToUrl(simple);
                used.Add("empty");
            }
            while (true)
            {
                driver.SwitchTo().Window(tabs[0]);
                Thread.Sleep(10000);
                List<Tab> listOfTabs = new List<Tab>();
                elements = driver.FindElements(By.XPath("//a[@class='mod-link']")).ToList();
                Console.WriteLine(elements.Count + " - links");
                for (int i = 0; i < elements.Count; i++)
                {
                    Tab tab = new Tab();
                    tab.id = elements[i].GetAttribute("data-event-or-meeting-id");
                    tab.url = elements[i].GetAttribute("href");
                    listOfTabs.Add(tab);
                }
                elements = driver.FindElements(By.XPath("//a[@class='mod-link']/event-line/section/ul[3]")).ToList();
                Console.WriteLine(elements.Count + " - matched");
                for (int i = 0; i < elements.Count; i++)
                {
                    int sum = 0;
                    string s = elements[i].GetAttribute("title");
                    foreach (char x in s)
                        if (x >= '0' && x <= '9') sum = sum * 10 + x - '0';
                    listOfTabs[i].matched = sum;
                }
                elements = driver.FindElements(By.XPath("//data-bf-livescores-time-elapsed/ng-include/div/div/div")).ToList();
                Console.WriteLine(elements.Count + " - inPlay");
                for (int i = 0; i < Math.Min(elements.Count, listOfTabs.Count()); i++)
                    if (elements[i].Text != "END") listOfTabs[i].inPlay = true;
                if (toDelete.Count > 0) Delete();
                Console.WriteLine("STARTTTTT");
                Console.WriteLine(listOfTabs.Count + " - tabs");
                Console.WriteLine(used.Count + " - used");
                int ttmp = 0;
                for(int i = 0; i < used.Count(); i++)
                {
                    if (used[i] == "empty") ttmp++;
                }
                Console.WriteLine("TTMP: " + ttmp);
                int cnt = 0;
                for (int i = 0; i < listOfTabs.Count(); i++)
                {
                    if (listOfTabs[i].inPlay && listOfTabs[i].matched > 6000) cnt++;
                }
                Console.WriteLine(cnt + " - cnt");
                for (int i = cnt / 30; i < listOfTabs.Count; i++)
                {
                    if (listOfTabs[i].inPlay && listOfTabs[i].matched > 6000)
                    {
                        for (int j = 0; j < used.Count; j++)
                            if (used[j] == "empty" && !usedLinks.Contains(listOfTabs[i].id))
                            {
                                Console.WriteLine(listOfTabs[i].id);
                                used[j] = listOfTabs[i].id;
                                usedLinks.Add(listOfTabs[i].id);
                                driver.SwitchTo().Window(tabs[j + 1]);
                                driver.Navigate().GoToUrl(listOfTabs[i].url);
                                break;
                            }
                    }
                }
                for (int i = 1; i < tabs.Count; i++)
                {
                    driver.SwitchTo().Window(tabs[i]);
                    Thread.Sleep(5000);
                }
                Thread.Sleep(30000);
            }
        }
    }
    class Tab
    {
        public string url;
        public string id;
        public int matched;
        public bool inPlay;
        public Tab()
        {
            url = "entin";
            id = "1";
            matched = 0;
            inPlay = false;
        }
    }
}
