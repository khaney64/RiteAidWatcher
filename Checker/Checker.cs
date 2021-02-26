using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading;
using ExpectedConditions = SeleniumExtras.WaitHelpers.ExpectedConditions;

namespace RiteAidChecker
{
    public class Checker : IDisposable
    {
        private readonly RiteAidData data;
        private readonly ChromeDriver driver;

        public Checker(RiteAidData data)
        {
            this.data = data;
            var options = new ChromeOptions()
            {
                //BinaryLocation = @"C:/develelopment/home/RiteAidWatcher/chromedriver/chromedriver.exe",
                //AcceptInsecureCertificates = false,
            };
            options.AddArgument("--window-size=1920,1080");
            driver = new ChromeDriver(options);
        }

        public bool Check(string zip, string store)
        {
            var homeURL = "https://www.riteaid.com/pharmacy/covid-qualifier";
            driver.ExecuteJavaScript("document.body.style.zoom='50%'");
            driver.Navigate().GoToUrl(homeURL);
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            // Birth Date
            wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@id=\"dateOfBirth\"]")));
            driver.FindElement(By.XPath("//*/input[@id=\"dateOfBirth\"]")).SendKeys(data.BirthDate);

            // Zip
            driver.FindElement(By.XPath("//*[@id=\"zip\"]")).Click();

            // Occupation
            var occupationDropdown = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"Occupation\"]")));
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            occupationDropdown.Click();

            wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
            occupationDropdown.SendKeys(data.Occupation.Format());

            var occupationItem = By.XPath("//*[@id=\"eligibility\"]/div/div[2]/div[1]/div/div/ul/li/a");

            var item = wait.Until(ExpectedConditions.ElementToBeClickable(occupationItem));
            item.Click();

            // City
            driver.FindElement(By.XPath("//*[@id=\"city\"]")).SendKeys(data.City);

            // Medical Condition
            ScrollElementIntoView("//*[@id=\"mediconditions\"]", clickable: true);
            var conditionDropdown = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"mediconditions\"]")));
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            conditionDropdown.Click();
            wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
            conditionDropdown.SendKeys(data.Condition.Format());

            var conditionItem = By.XPath("//*[@id=\"eligibility\"]/div/div[2]/div[2]/div/div/ul/li/a");

            item = wait.Until(ExpectedConditions.ElementToBeClickable(conditionItem));
            item.Click();

            // State
            var stateBox = ScrollElementIntoView("//*[@id=\"eligibility_state\"]", clickable: true);
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            stateBox.Click();
            // wait for this div to change
            wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
            driver.FindElement(By.XPath("//*[@id=\"eligibility_state\"]")).SendKeys(data.State + "\t");

            // Zip
            driver.FindElement(By.XPath("//*[@id=\"zip\"]")).SendKeys(zip + "\t");

            // Next
            var nextButton = ScrollElementIntoView("//*[@id=\"continue\"]", clickable: true);
            nextButton.Click();

            // Continue
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            ScrollElementIntoView("//*[@id=\"learnmorebttn\"]", clickable: true);
            var continueButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"learnmorebttn\"]")));
            continueButton.Click();

            //// scheduler

            // Zip
            var zipBox = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"covid-store-search\"]")));
            zipBox.Clear();
            zipBox.SendKeys(zip);

            // Find
            var findButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"btn-find-store\"]")));
            findButton.Click();

            // Store check
            var buttonBy = By.CssSelector($"a[class*=\"covid-store__store__anchor--unselected\"][data-loc-id=\"{store}\"]");
            var storeButton = wait.Until(ExpectedConditions.ElementToBeClickable(buttonBy));
            ScrollElementIntoView(buttonBy, clickable: true);
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            storeButton.Click();

            // Next
            nextButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"continue\"]")));
            nextButton.Click();

            Thread.Sleep(1000);
            // if it fails slots test it'll display a warning now
            if (IsElementPresent(By.CssSelector("div[class=\"covid-store__slot-template\"][data-template-id=\"covid-store__slot-template-id\"][style=\"\"]")))
            {
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            driver.Close();
        }

        private bool IsElementPresent(By by)
        {
            try
            {
                driver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        private IWebElement ScrollElementIntoView(string xpath, bool clickable = false)
        {
            var wait = new WebDriverWait(driver, System.TimeSpan.FromSeconds(15));
            var element = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath(xpath)));
            ((IJavaScriptExecutor)driver).ExecuteScript("window.scroll(" + element.Location.X + "," + (element.Location.Y - 200) + ");");

            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath(xpath)));
            if (clickable)
            {
                element = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(xpath)));
            }

            return element;
        }

        private IWebElement ScrollElementIntoView(By by, bool clickable = false)
        {
            var wait = new WebDriverWait(driver, System.TimeSpan.FromSeconds(15));
            var element = wait.Until(ExpectedConditions.ElementIsVisible(by));
            ((IJavaScriptExecutor)driver).ExecuteScript("window.scroll(" + element.Location.X + "," + (element.Location.Y - 200) + ");");

            wait.Until(ExpectedConditions.ElementIsVisible(by));
            if (clickable)
            {
                element = wait.Until(ExpectedConditions.ElementToBeClickable(by));
            }

            return element;
        }

    }
}
