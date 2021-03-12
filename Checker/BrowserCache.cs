using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidChecker
{
    public class BrowserCache
    {
        private readonly Stack<ChromeDriver> availableStack;
        private readonly List<ChromeDriver> holdList;
        private readonly int maxBrowsers;
        private readonly object data;
        private readonly Action<ChromeDriver,object> initializer;
        private readonly Action<ChromeDriver> resetter;
        private int loadedBrowsers;

        public BrowserCache(int browsers, object data, Action<ChromeDriver,object> initializer, Action<ChromeDriver> resetter)
        {
            availableStack = new Stack<ChromeDriver>(browsers);
            holdList = new List<ChromeDriver>();
            maxBrowsers = browsers;
            this.data = data;
            this.initializer = initializer;
            this.resetter = resetter;
        }

        public ChromeDriver Pop()
        {
            if (!availableStack.TryPop(out var browser))
            {
                if (loadedBrowsers >= maxBrowsers)
                {
                    return null;
                }
                var options = new ChromeOptions()
                {
                    //BinaryLocation = @"C:/develelopment/home/RiteAidWatcher/chromedriver/chromedriver.exe",
                    //AcceptInsecureCertificates = false,
                };
                options.AddArgument("--window-size=1920,1080");
                browser = new ChromeDriver(options);
                initializer(browser, data);
                loadedBrowsers += 1;
            }

            return browser;
        }

        public void Push(ChromeDriver browser)
        {
            if (browser == null)
                return;

            resetter(browser);
            availableStack.Push(browser);
        }

        public void Hold(ChromeDriver browser)
        {
            holdList.Add(browser);
        }

        public bool Release(ChromeDriver browser)
        {
            return holdList.Remove(browser);
        }

        public void Preload()
        {
            ChromeDriver browser = null;
            var browsers = new List<ChromeDriver>();
            do
            {
                browser = Pop();
                if (browser != null)
                {
                    browsers.Add(browser);
                }
            } while (browser != null);
            browsers.ForEach(b => Push(b));
        }
    }
}
