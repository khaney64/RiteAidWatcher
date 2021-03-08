using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidChecker
{
    public static class Extensions
    {
        public static string Format(this ConditionType condition)
        {
            switch (condition)
            {
                case ConditionType.Diabetes:
                case ConditionType.Obesity:
                    return condition.ToString();
                case ConditionType.WeakendImmuneSystem:
                    return "Weakened Immune System";
                default:
                    throw new Exception($"Unexpected Condition {condition}");
            }
        }

        public static string Format(this OccupationType occupation)
        {
            switch (occupation)
            {
                case OccupationType.NoneOfTheAbove:
                    return "None of the Above";
                default:
                    throw new Exception($"Unexpected Condition {occupation}");
            }
        }

        public static IWebElement ScrollElementIntoView(this ChromeDriver driver, string xpath, bool clickable = false)
        {
            var wait = new WebDriverWait(driver, System.TimeSpan.FromSeconds(20));
            var element = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath(xpath)));
            ((IJavaScriptExecutor)driver).ExecuteScript("window.scroll(" + element.Location.X + "," + (element.Location.Y - 200) + ");");

            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath(xpath)));
            if (clickable)
            {
                element = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(xpath)));
            }

            return element;
        }

        public static IWebElement ScrollElementIntoView(this ChromeDriver driver, By by, bool clickable = false)
        {
            var wait = new WebDriverWait(driver, System.TimeSpan.FromSeconds(20));
            var element = wait.Until(ExpectedConditions.ElementIsVisible(by));
            ((IJavaScriptExecutor)driver).ExecuteScript("window.scroll(" + element.Location.X + "," + (element.Location.Y - 200) + ");");

            wait.Until(ExpectedConditions.ElementIsVisible(by));
            if (clickable)
            {
                element = wait.Until(ExpectedConditions.ElementToBeClickable(by));
            }

            return element;
        }

        public static bool IsElementPresent(this ChromeDriver driver, By by)
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

    }
}
