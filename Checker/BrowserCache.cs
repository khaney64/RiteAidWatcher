using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidChecker
{
    public class BrowserCache
    {
        private readonly Stack<ChromeDriver> stack;
        private readonly int maxBrowsers;
        private readonly RiteAidData data;
        private readonly Action<ChromeDriver,RiteAidData> initializer;
        private readonly Action<ChromeDriver> resetter;
        private int loadedBrowsers;

        public BrowserCache(int browsers, RiteAidData data, Action<ChromeDriver,RiteAidData> initializer, Action<ChromeDriver> resetter)
        {
            stack = new Stack<ChromeDriver>(browsers);
            maxBrowsers = browsers;
            this.data = data;
            this.initializer = initializer;
            this.resetter = resetter;
        }

        public ChromeDriver Pop()
        {
            if (!stack.TryPop(out var browser))
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
            resetter(browser);
            stack.Push(browser);
        }

    }
}
