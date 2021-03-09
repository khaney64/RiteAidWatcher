using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using System;
using System.ComponentModel;
using System.Data;
using System.Threading;
using Newtonsoft.Json.Serialization;
using ExpectedConditions = SeleniumExtras.WaitHelpers.ExpectedConditions;
using System.Linq;

namespace RiteAidChecker
{
    public class Checker
    {
        public static void Initializer(ChromeDriver browser, RiteAidData data)
        {
            var homeURL = "https://www.riteaid.com/pharmacy/covid-qualifier";
            //browser.ExecuteJavaScript("document.body.style.zoom='50%'");
            browser.Navigate().GoToUrl(homeURL);
            WebDriverWait wait = new WebDriverWait(browser, TimeSpan.FromSeconds(20));

            // Birth Date
            wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@id=\"dateOfBirth\"]")));
            browser.FindElement(By.XPath("//*/input[@id=\"dateOfBirth\"]")).SendKeys(data.BirthDate);

            // Zip
            browser.FindElement(By.XPath("//*[@id=\"zip\"]")).Click();

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
            browser.FindElement(By.XPath("//*[@id=\"city\"]")).SendKeys(data.City);

            // Medical Condition
            browser.ScrollElementIntoView("//*[@id=\"mediconditions\"]", clickable: true);
            var conditionDropdown = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"mediconditions\"]")));
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            conditionDropdown.Click();
            wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
            conditionDropdown.SendKeys(data.Condition.Format());

            var conditionItem = By.XPath("//*[@id=\"eligibility\"]/div/div[2]/div[2]/div/div/ul/li/a");

            item = wait.Until(ExpectedConditions.ElementToBeClickable(conditionItem));
            item.Click();

            // State
            var stateBox = browser.ScrollElementIntoView("//*[@id=\"eligibility_state\"]", clickable: true);
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            stateBox.Click();
            // wait for this div to change
            wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
            browser.FindElement(By.XPath("//*[@id=\"eligibility_state\"]")).SendKeys(data.State + "\t");

            // Zip
            browser.FindElement(By.XPath("//*[@id=\"zip\"]")).SendKeys(data.Zip + "\t");

            // Next
            var nextButton = browser.ScrollElementIntoView("//*[@id=\"continue\"]", clickable: true);
            nextButton.Click();

            // Continue
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            browser.ScrollElementIntoView("//*[@id=\"learnmorebttn\"]", clickable: true);
            var continueButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"learnmorebttn\"]")));
            continueButton.Click();

        }

        public static void Resetter(ChromeDriver browser)
        {
            var schedulerUrl = "https://www.riteaid.com/pharmacy/apt-scheduler";
            browser.Navigate().Refresh();
            if (browser.IsAlertPresent())
            {
                browser.SwitchTo().Alert();
                browser.SwitchTo().Alert().Accept();
                browser.SwitchTo().DefaultContent();
            }
            //browser.Navigate().GoToUrl(schedulerUrl);
            //browser.ExecuteJavaScript("document.body.style.zoom='50%'");
            WebDriverWait wait = new WebDriverWait(browser, TimeSpan.FromSeconds(20));

            var zipBox = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"covid-store-search\"]")));
            zipBox.Clear();
        }

        public static (bool haveSlots, string info) Check(string zip, string store, ChromeDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            // Zip
            var zipBox = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"covid-store-search\"]")));
            zipBox.Clear();
            zipBox.SendKeys(zip + Keys.Enter);

            // Find
            //var findButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"btn-find-store\"]")));
            //findButton.Click();

            // Store check
            var buttonBy = By.CssSelector($"a[class*=\"covid-store__store__anchor--unselected\"][data-loc-id=\"{store}\"]");
            //var storeButton = wait.Until(ExpectedConditions.ElementToBeClickable(buttonBy));
            var storeButton = driver.ScrollElementIntoView(buttonBy, clickable: true);
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            //storeButton.Click();
            storeButton.SendKeys(Keys.Enter);

            // Next
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            var nextByXpath = By.XPath("//*[@id=\"continue\"]");

            wait.Until(ExpectedConditions.ElementExists(nextByXpath));
            var nextButton = driver.ScrollElementIntoView(nextByXpath, clickable: true);
            nextButton.Click();

            Thread.Sleep(2000);

            // if it fails slots test it'll display a warning now
            if (driver.IsElementPresent(By.CssSelector("div[class=\"covid-store__slot-template\"][data-template-id=\"covid-store__slot-template-id\"][style=\"\"]")))
            {
                return (false, "no slots");
            }

            var covidTimeByCss = By.CssSelector("input[type=\"radio\"][class=\"covid-time__radio\"]");
            var covidTimes = driver.FindElementsByCssSelector("input[type=\"radio\"][class=\"covid-time__radio\"]");
            foreach (var covidTime in covidTimes)
            {
                driver.ScrollElementIntoView(covidTimeByCss, clickable: true);
                covidTime.Click();
                Thread.Sleep(1000);

                var nextByCss = By.CssSelector("button[id=\"continue\"][class*=\"covid-scheduler__contnuebtn form-btns--continue\"]");
                nextButton = driver.ScrollElementIntoView(nextByCss, clickable:true);
                nextButton.Click();

                Thread.Sleep(1000);
                // if slot it taken, it'll show a warning now instead of advancing
                if (driver.IsElementPresent(By.CssSelector("div[class=\"covid-scheduler__validation-section covid-scheduler__invalid\"]")))
                {
                    Thread.Sleep(1000);
                    continue;
                }

                return (true, "");
            }

            return (false, covidTimes.Any() ? "found slots" : "no slots");
        }
    }
}
