﻿using OpenQA.Selenium;
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
using OpenQA.Selenium.Internal;

namespace RiteAidChecker
{
    public class Checker
    {
        /// <summary>
        /// Loads and fills in the covid qualifier page - once initialized we should be on the apt-scheduler page.
        /// Qualification information changes (see the rules url, or rules data that is dumped on startup) so it may not get past qualification.
        /// This page is also finicky - sometimes on startup it times out, so I usually just restart until I get the initialized browsers up successfully.
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="data"></param>
        public static void Initializer(ChromeDriver browser, object data)
        {
            var riteAidData = data as RiteAidData;

            var homeURL = "https://www.riteaid.com/pharmacy/covid-qualifier";
            browser.Navigate().GoToUrl(homeURL);
            WebDriverWait wait = new WebDriverWait(browser, TimeSpan.FromSeconds(20));

            // Birth Date
            // sometimes this times out... adding a wait loop
            var dobBy = By.XPath("//*[@id=\"dateOfBirth\"]");
            int retries = 0;
            while (!browser.IsElementPresent(dobBy) && retries < 3)
            {
                retries++;
                Thread.Sleep(1000);
            }
            browser.FindElement(dobBy).SendKeys(riteAidData.BirthDate);

            // Zip
            browser.FindElement(By.XPath("//*[@id=\"zip\"]")).Click();

            // Occupation
            var occupationDropdown = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"Occupation\"]")));
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            occupationDropdown.Click();

            wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
            occupationDropdown.SendKeys(riteAidData.Occupation.Format());

            var occupationItem = By.XPath("//*[@id=\"eligibility\"]/div/div[2]/div/div[1]/div/div/ul/li/a");

            var item = wait.Until(ExpectedConditions.ElementToBeClickable(occupationItem));
            item.Click();

            // City
            browser.FindElement(By.XPath("//*[@id=\"city\"]")).SendKeys(riteAidData.City);

            // Medical Condition
            browser.ScrollElementIntoView("//*[@id=\"mediconditions\"]", clickable: true);
            var conditionDropdown = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"mediconditions\"]")));
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            conditionDropdown.Click();
            wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
            conditionDropdown.SendKeys(riteAidData.Condition.Format());

            var conditionItem = By.XPath("//*[@id=\"eligibility\"]/div/div[2]/div/div[2]/div/div/div/ul/li/a");

            item = wait.Until(ExpectedConditions.ElementToBeClickable(conditionItem));
            item.Click();

            // State
            var stateBox = browser.ScrollElementIntoView("//*[@id=\"eligibility_state\"]", clickable: true);
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            stateBox.Click();
            // wait for this div to change
            wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
            browser.FindElement(By.XPath("//*[@id=\"eligibility_state\"]")).SendKeys(riteAidData.StateName + "\t");

            // Zip
            browser.FindElement(By.XPath("//*[@id=\"zip\"]")).SendKeys(riteAidData.Zip + "\t");

            // Next
            Thread.Sleep(1000);
            var nextButton = browser.ScrollElementIntoView("//*[@id=\"continue\"]", clickable: true);;
            nextButton.Click();

            // Continue
            Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
            browser.ScrollElementIntoView("//*[@id=\"learnmorebttn\"]", clickable: true);
            Thread.Sleep(1000);
            var continueButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"learnmorebttn\"]")));
            continueButton.Click();

        }

        public static void Resetter(ChromeDriver browser)
        {
            //var schedulerUrl = "https://www.riteaid.com/pharmacy/apt-scheduler";
            //should probably verify that I'm on the apt-scheduler page; that's currently the assumption
            browser.Navigate().Refresh();
            if (browser.IsAlertPresent())
            {
                browser.SwitchTo().Alert();
                browser.SwitchTo().Alert().Accept();
                browser.SwitchTo().DefaultContent();
            }
            //browser.Navigate().GoToUrl(schedulerUrl);
            WebDriverWait wait = new WebDriverWait(browser, TimeSpan.FromSeconds(20));

            // sometimes this times out... adding a wait loop
            var searchBy = By.XPath("//*[@id=\"covid-store-search\"]");
            int retries = 0;
            while (!browser.IsElementPresent(searchBy) && retries < 3)
            {
                retries++;
                Thread.Sleep(1000);
            }
            var zipBox = wait.Until(ExpectedConditions.ElementToBeClickable(searchBy));
            browser.ScrollElementIntoView(searchBy, clickable: true);
            zipBox.Clear();
        }

        public static (bool haveSlots, string info) Check(string zip, string store, RiteAidData data, ChromeDriver driver)
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
            Thread.Sleep(1000);
            //var nextButton = driver.ScrollElementIntoView(nextByXpath, clickable: true);
            //nextButton.Click();
            FindFieldAndClick(driver, wait, nextByXpath);

            Thread.Sleep(2000);

            // if it fails slots test it'll display a warning now
            if (driver.IsElementPresent(By.CssSelector("div[class=\"covid-store__slot-template\"][data-template-id=\"covid-store__slot-template-id\"][style=\"\"]")))
            {
                return (false, "no slots - store");
            }

            var covidTimeByCss = By.CssSelector("input[type=\"radio\"][class=\"covid-time__radio\"]");
            var covidTimes = driver.FindElementsByCssSelector("input[type=\"radio\"][class=\"covid-time__radio\"]");
            foreach (var covidTime in covidTimes)
            {
                driver.ScrollElementIntoView(covidTimeByCss, clickable: true);
                covidTime.Click();
                Thread.Sleep(1000);

                var nextByCss = By.CssSelector("button[id=\"continue\"][class*=\"covid-scheduler__contnuebtn form-btns--continue\"]");
                var nextButton = driver.ScrollElementIntoView(nextByCss, clickable:true);
                Thread.Sleep(1000);
                nextButton.Click();

                Thread.Sleep(1000);
                // if slot it taken, it'll show a warning now instead of advancing
                if (driver.IsElementPresent(By.CssSelector("div[class=\"covid-scheduler__validation-section covid-scheduler__invalid\"]")))
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (PatientInfoPage(driver, data) && MedicalInfoPage(driver, data) && ConsentPage(driver, data))
                { 
                    return (true, "at consent");
                }

                return (true, $"({covidTimes.Count})");
            }

            return (false, covidTimes.Any() ? $"found slots ({covidTimes.Count})" : $"no slots - scheduler");
        }

        private static bool PatientInfoPage(ChromeDriver browser, RiteAidData data)
        {
            /*
             * look for guardian checkbox - //*[@id="ptHasGuardian"]
             * find and fill in
             *   first name : //*[@id="firstName"]
             *   last name : //*[@id="lastName"]
             *   date of birth : //*[@id="dateOfBirth"]
             *   mobile phone : //*[@id="phone"]
             *   street address : //*[@id="addr1"]
             *   email : //*[@id="email"]
             *   city : //*[@id="city"]
             *   state (dropdown) : //*[@id="patient_state"]  element 0 after filling in state = //*[@id="patient-info"]/div[7]/div[1]/div/div/ul/li
             *   zip : //*[@id="zip"]
             *   sms reminder checkbox : //*[@id="sendReminderSMS"]
             *   email reminder checkbox : //*[@id="sendReminderEmail"]
             *   pcp slider : //*[@id="physician"] (click to disable so we don't have to fill it out)
             *   next button : //*[@id="continue"]
             *
             * if successful, will go to medical information
             *
             */

            try
            {
                var wait = new WebDriverWait(browser, TimeSpan.FromSeconds(20));

                // make sure we're on the right page.. look for guardian slider
                var tries = 0;
                const int maxTries = 3;
                while (tries < maxTries && !browser.IsElementPresent(By.XPath("//*[@id=\"ptHasGuardian\"]")))
                {
                    tries++;
                    if (tries == maxTries)
                    {
                        return false;
                    }
                    Thread.Sleep(500);
                }

                // first name
                FindFieldAndSendText(browser, wait, By.XPath("//*[@id=\"firstName\"]"), data.FirstName);
                // last name
                FindFieldAndSendText(browser, wait, By.XPath("//*[@id=\"lastName\"]"), data.LastName);
                // Birth Date
                FindFieldAndSendText(browser, wait, By.XPath("//*[@id=\"dateOfBirth\"]"), data.BirthDate);
                // Mobile Phone
                FindFieldAndSendText(browser, wait, By.XPath("//*[@id=\"phone\"]"), data.MobilePhone);
                // Street Address
                FindFieldAndSendText(browser, wait, By.XPath("//*[@id=\"addr1\"]"), data.StreetAddress);
                // Email
                FindFieldAndSendText(browser, wait, By.XPath("//*[@id=\"email\"]"), data.EmailAddress);
                // City
                FindFieldAndSendText(browser, wait, By.XPath("//*[@id=\"city\"]"), data.City);

                // State
                var stateBox = browser.ScrollElementIntoView("//*[@id=\"patient_state\"]", clickable: true);
                Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
                stateBox.Click();
                // wait for this div to change
                wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
                browser.FindElement(By.XPath("//*[@id=\"patient_state\"]")).SendKeys(data.StateName + "\t");

                // Zip
                FindFieldAndSendText(browser, wait, By.XPath("//*[@id=\"zip\"]"), data.Zip + "\t");

                // sms checkbox 
                var checkbox = browser.ScrollElementIntoView(By.CssSelector("label[for=\"sendReminderSMS\"]"), clickable: true);
                //Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
                checkbox.Click();

                // email checkbox
                checkbox = browser.ScrollElementIntoView(By.CssSelector("label[for=\"sendReminderEmail\"]"), clickable: true);
                //Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
                checkbox.Click();

                // physician slider
                // there are two sliders on the page with the same label - we want the second one
                var by = By.CssSelector("label[class=\"physcian-details__switch\"]");
                var sliders = browser.FindElements(by);
                var slider = sliders[1];
                slider.Click();

                Thread.Sleep(1000);

                // Next
                var nextButton = browser.ScrollElementIntoView("//*[@id=\"continue\"]", clickable: true);
                nextButton.Click();

                return true;
            }
            catch (Exception e)
            {
                Console.Beep(200, 500); // debug
                Console.Error.WriteLine($"Unexpected Patient Info exception : {e.Message}");
                Console.Error.WriteLine(e.StackTrace);
                return false;
            }
        }

        private static bool MedicalInfoPage(ChromeDriver browser, RiteAidData data)
        {
            /*
             * look for Sex dropdown
             *   sex : //*[@id="mi_gender"]  dropdown Decline to Answer, Female, Male   element 0 after filling in sex = /html/body/div[1]/div/div[5]/div/div[2]/div/div/div[3]/form/div[1]/div[4]/div[1]/div[2]/div[1]/div[3]/ul/li
             *                                                                                                           li[class="typeahead__item typeahead__group-group"][data-index="0"]
             *   hispanic : //*[@id="mi_origin"]  dropdown Hispanic or Latino, Not Hispanic or Latino, Unknown Ethnicity,  element zero same as sex above
             *   race : //*[@id="mi_represents"]  dropdown White       element zero same as sex and hispanic
             *   health questions - example first one No : //*[@id="noptHasHealthProblem"]
             *
             *   other health conditions text box : //*[@id="ptHasOtherMedicalCondition"]
             *   next button : //*[@id="continue"]
             *
             *
             * if successful, will go to the consent page
             * look for signature box
             *   signature : //*[@id="signature"]
             *
             *
             */

            try
            {
                var wait = new WebDriverWait(browser, TimeSpan.FromSeconds(20));

                // make sure we're on the right page.. look for gender
                var tries = 0;
                const int maxTries = 3;
                var byGender = By.XPath("//*[@id=\"mi_gender\"]");
                while (tries < maxTries && !browser.IsElementPresent(byGender))
                {
                    tries++;
                    if (tries == maxTries)
                    {
                        return false;
                    }
                    Thread.Sleep(500);
                }

                // Sex
                var sexBy = By.XPath("//*[@id=\"mi_gender\"]");
                var dropdown = FindFieldAndClick(browser, wait, sexBy);
                wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
                dropdown.SendKeys(data.Sex.Format());

                // if male, both female then male will come up in list, so need to pick item 1 for male.
                // if female, it'll be 0th item
                var itemNum = data.Sex == SexType.Male ? 1 : 0;
                var itemBy = By.XPath($"//li[@class=\"typeahead__item typeahead__group-group\"][@data-index=\"{itemNum}\"]");
                var item = wait.Until(ExpectedConditions.ElementToBeClickable(itemBy));
                item.Click();

                // Hispanic
                var hispanicBy = By.XPath("//*[@id=\"mi_origin\"]");
                dropdown = FindFieldAndClick(browser, wait, hispanicBy);
                wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
                dropdown.SendKeys(data.Hispanic.Format());

                itemBy = By.XPath($"//li[@class=\"typeahead__item typeahead__group-group\"][@data-index=\"0\"]");
                // itemBy finds the previous dropdowns, too, so need to find the one with our text and click that one
                var items = browser.FindElements(itemBy);
                item = items.ToList().Find(e => e.Text == data.Hispanic.Format());
                item.Click();

                // Race
                var raceBy = By.XPath("//*[@id=\"mi_represents\"]");
                dropdown = FindFieldAndClick(browser, wait, raceBy);
                wait.Until(ExpectedConditions.ElementExists(By.CssSelector("div[class=\"form__row typeahead__container result\"]")));
                dropdown.SendKeys(data.Race.Format());

                items = browser.FindElements(itemBy);
                item = items.ToList().Find(e => e.Text == data.Race.Format());
                item.Click();

                // questionnaire
                SelectAnswer(browser, "HasHealthProblem", AnswerType.No);
                SelectAnswer(browser, "HasLungProblem", AnswerType.No);
                SelectAnswer(browser, "UsesNicotine", data.Condition == ConditionType.Smoking ? AnswerType.Yes : AnswerType.No);
                SelectAnswer(browser, "HasVaxAllergy", AnswerType.No);
                SelectAnswer(browser, "GotVaxInLast4Weeks", AnswerType.No);
                SelectAnswer(browser, "HasPriorVaxReaction", AnswerType.No);
                SelectAnswer(browser, "HasSeizureHistory", AnswerType.No);
                SelectAnswer(browser, "HasImmuneProblem", AnswerType.No);
                SelectAnswer(browser, "TakesCancerDrugs", AnswerType.No);
                SelectAnswer(browser, "ReceivedTransfusion", AnswerType.No);
                SelectAnswer(browser, "IsInfantCaregiver", AnswerType.No);
                SelectAnswer(browser, "IsPregnant", AnswerType.No);
                SelectAnswer(browser, "HasImmRecCard", AnswerType.DontKnow);
                SelectAnswer(browser, "HasMedAdherenceProgram", AnswerType.DontKnow);
                SelectAnswer(browser, "HadFluShot", AnswerType.Yes);
                SelectAnswer(browser, "HadShinglesShot", AnswerType.No);
                SelectAnswer(browser, "HadWhoopShot", AnswerType.DontKnow);

                // other conditions
                var otherConditionsBy = By.CssSelector("textarea[id=\"ptHasOtherMedicalCondition\"]");
                var otherConditions = browser.ScrollElementIntoView(otherConditionsBy);
                otherConditions.SendKeys(data.OtherConditions + Keys.Tab);

                Thread.Sleep(1000);

                // Next
                var nextButton = browser.ScrollElementIntoView("//*[@id=\"continue\"]", clickable: true);
                nextButton.Click();

                return true;
            }
            catch (Exception e)
            {
                Console.Beep(200, 500); // debug
                Console.Error.WriteLine($"Unexpected Medical Info exception : {e.Message}");
                Console.Error.WriteLine(e.StackTrace);
                return false;
            }
        }

        private static void SelectAnswer(ChromeDriver browser, string questionId, AnswerType answer)
        {
            var elementId = $"{answer.Format()}pt{questionId}";
            var elementBy = By.CssSelector($"button[id=\"{elementId}\"]");

            if (browser.IsElementPresent(elementBy))
            {
                int debug = 1;
            }

            var element = browser.ScrollElementIntoView(elementBy, clickable: true);
            element.Click();
        }

        private static bool ConsentPage(ChromeDriver browser, RiteAidData data)
        {
            /*
             * look for signature box
             *   signature : //*[@id="signature"]
             *
             */

            try
            {
                var wait = new WebDriverWait(browser, TimeSpan.FromSeconds(20));

                // make sure we're on the right page.. look for guardian slider
                var tries = 0;
                const int maxTries = 3;
                var bySignature = By.XPath("//*[@id=\"signature\"]");
                while (tries < maxTries && !browser.IsElementPresent(bySignature))
                {
                    tries++;
                    if (tries == maxTries)
                    {
                        return false;
                    }
                    Thread.Sleep(500);
                }

                Console.Beep(1000, 500); Thread.Sleep(1); Console.Beep(1000, 500); // debug
                // todo - figure out how to write something into the the signature box - could just be a line, but probably has to be something
                var canvas = browser.FindElement(bySignature);
                var size = canvas.Size;

                //Console.WriteLine($"canvas info {size.Width}x{size.Height} empty = {size.IsEmpty}");

                TryCanvas(() => {
                    new Actions(browser)
                        .MoveToElement(canvas, size.Width / 4, size.Height / 2)
                        .Perform();
                });

                TryCanvas(() => {
                new Actions(browser)
                    .MoveToElement(canvas, size.Width / 4, size.Height / 2)
                    .ClickAndHold(canvas)
                    .Release(canvas)
                    .Perform();
                });

                TryCanvas(() => {
                new Actions(browser)
                    .MoveToElement(canvas, size.Width / 4, size.Height / 2)
                    .ClickAndHold(canvas)
                    .MoveToElement(canvas, size.Width / 2, size.Height / 2)
                    .Perform();
                });

                TryCanvas(() => {
                new Actions(browser)
                    .MoveToElement(canvas, size.Width / 4, size.Height / 2)
                    .ClickAndHold(canvas)
                    .MoveByOffset(10, 0)
                    .Release(canvas)
                    .Perform();
                });

                Thread.Sleep(1000);

                // Next
                var nextButton = browser.ScrollElementIntoView("//*[@id=\"continue\"]", clickable: true);
                nextButton.Click();

                return true;
            }
            catch (Exception e)
            {
                Console.Beep(200, 500); // debug
                Console.Error.WriteLine($"Unexpected Consent: {e.Message}");
                Console.Error.WriteLine(e.StackTrace);
                return false;
            }
        }

        private static string TryCanvas(Action action)
        {
            try
            {
                action();
                return "success";
            }
            catch (Exception e)
            {
                return $"fail : {e.Message}";
            }
        }

        private static void FindFieldAndSendText(ChromeDriver browser, WebDriverWait wait, By by, string value)
        {
            wait.Until(ExpectedConditions.ElementExists(by));
            var field = browser.ScrollElementIntoView(by);
            field.SendKeys(value);
        }

        private static IWebElement FindFieldAndClick(ChromeDriver browser, WebDriverWait wait, By by, int maxTries = 3)
        {
            var tries = 0;
            var success = false;
            IWebElement field = null;
            while (tries < maxTries && !success)
            {
                try
                {
                    tries++;
                    wait.Until(ExpectedConditions.ElementExists(by));
                    field = browser.ScrollElementIntoView(by, clickable: true);
                    field.Click();
                    success = true;
                }
                catch (Exception)
                {
                    if (tries >= maxTries)
                    {
                        throw;
                    }
                }
                Thread.Sleep(500);
            }

            return field;
        }
    }
}
