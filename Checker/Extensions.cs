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
                case ConditionType.Cancer:
                case ConditionType.Diabetes:
                case ConditionType.Obesity:
                case ConditionType.Smoking:
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
                case OccupationType.ChildcareWorker:
                    return "Childcare Worker";
                case OccupationType.NoneOfTheAbove:
                    return "None of the Above";
                default:
                    throw new Exception($"Unexpected Condition {occupation}");
            }
        }

        public static string Format(this SexType sex)
        {
            switch (sex)
            {
                case SexType.Male:
                case SexType.Female:
                    return sex.ToString();
                case SexType.DeclineToAnswer:
                    return "Decline to Answer";
                default:
                    throw new Exception($"Unexpected Sex {sex}");
            }
        }

        public static string Format(this RaceType race)
        {
            switch (race)
            {
                case RaceType.AmericanIndianorAlaskaNative:
                    return "American Indian or Alaska Native";
                case RaceType.BlackorAfricanAmerican:
                    return "Black or African American";
                case RaceType.NativeHawaiianorOtherPacificIslander:
                    return "Native Hawaiian or Other Pacific Islander";
                case RaceType.Asian:
                case RaceType.White:
                    return race.ToString();
                default:
                    throw new Exception($"Unexpected Race {race}");
            }
        }

        public static string Format(this HispanicType hispanic)
        {
            switch (hispanic)
            {
                case HispanicType.HispanicorLatino:
                    return "Hispanic or Latino";
                case HispanicType.NotHispanicorLatino:
                    return "Not Hispanic or Latino";
                case HispanicType.UnknownEthnicity:
                    return "Unknown Ethnicity";
                default:
                    throw new Exception($"Unexpected Hispanic Type {hispanic}");
            }
        }

        public static string Format(this AnswerType answer)
        {
            switch (answer)
            {
                case AnswerType.Yes:
                    return "ys";
                case AnswerType.No:
                    return "no";
                case AnswerType.DontKnow:
                    return "na";
                default:
                    throw new Exception($"Unexpected Answer {answer}");
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

        public static bool IsAlertPresent(this ChromeDriver driver)
        {
            try
            {
                driver.SwitchTo().Alert();
                return true;
            } // try
            catch (Exception)
            {
                return false;
            } // catch
        }

        public static bool IsElementClickable(this ChromeDriver driver, By by)
        {
            try
            {
                if (!driver.IsElementPresent(by))
                {
                    return false;
                }

                driver.ScrollElementIntoView(by, clickable: true);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
