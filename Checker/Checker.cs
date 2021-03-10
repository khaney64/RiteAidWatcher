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
            var nextButton = driver.ScrollElementIntoView(nextByXpath, clickable: true);
            nextButton.Click();

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
                nextButton = driver.ScrollElementIntoView(nextByCss, clickable:true);
                Thread.Sleep(1000);
                nextButton.Click();

                Thread.Sleep(1000);
                // if slot it taken, it'll show a warning now instead of advancing
                if (driver.IsElementPresent(By.CssSelector("div[class=\"covid-scheduler__validation-section covid-scheduler__invalid\"]")))
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (PatientInfo(driver, data))
                {
                    return (true, "patient info");
                }

                Console.Beep(200,500); //debug
                return (true, $"({covidTimes.Count})");
            }

            return (false, covidTimes.Any() ? $"found slots ({covidTimes.Count})" : $"no slots - scheduler");
        }

        private static bool PatientInfo(ChromeDriver browser, RiteAidData data)
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
             *<div class="covid-scheduler__section">
                <div class="form__group">
                    <div class="form__row question"><p>Do you have a long-term health problem with heart disease, kidney disease, metabolic disorder (e.g. diabetes), anemia, or blood disorders?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHasHealthProblem" type="button" class="button">Yes</button>
                        <button id="noptHasHealthProblem" type="button" class="button">No</button>
                        <button id="naptHasHealthProblem" type="button" class="button">Don't Know</button>
                        <input name="ptHasHealthProblem" id="ptHasHealthProblem" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Do you have a long-term health problem with lung disease or asthma?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHasLungProblem" type="button" class="button">Yes</button>
                        <button id="noptHasLungProblem" type="button" class="button">No</button>
                        <button id="naptHasLungProblem" type="button" class="button">Don't Know</button>
                        <input name="ptHasLungProblem" id="ptHasLungProblem" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Do you use any nicotine products?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptUsesNicotine" type="button" class="button" name="ptUsesNicotine">Yes</button>
                        <button id="noptUsesNicotine" type="button" class="button">No</button>
                        <button id="naptUsesNicotine" type="button" class="button">Don't Know</button>
                        <input name="ptUsesNicotine" id="ptUsesNicotine" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Do you have allergies to medications, food (i.e. eggs), latex or any vaccine component (e.g. neomycin, formaldehyde, gentamicin, thimerosal, bovine protein, phenol, polymyxin, gelatin, baker's yeast or yeast)?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHasVaxAllergy" type="button" class="button">Yes</button>
                        <button id="noptHasVaxAllergy" type="button" class="button">No</button>
                        <button id="naptHasVaxAllergy" type="button" class="button">Don't Know</button>
                        <input name="ptHasVaxAllergy" id="ptHasVaxAllergy" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Have you received any vaccinations in the past 4 weeks?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptGotVaxInLast4Weeks" type="button" class="button">Yes</button>
                        <button id="noptGotVaxInLast4Weeks" type="button" class="button">No</button>
                        <button id="naptGotVaxInLast4Weeks" type="button" class="button">Don't Know</button>
                        <input name="ptGotVaxInLast4Weeks" id="ptGotVaxInLast4Weeks" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Have you ever had a serious reaction after receiving a vaccination?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHasPriorVaxReaction" type="button" class="button">Yes</button>
                        <button id="noptHasPriorVaxReaction" type="button" class="button">No</button>
                        <button id="naptHasPriorVaxReaction" type="button" class="button">Don't Know</button>
                        <input name="ptHasPriorVaxReaction" id="ptHasPriorVaxReaction" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Do you have a neurological disorder such as seizures or other disorders that affect the brain or have had a disorder that resulted from vaccine (e.g. Guillain-Barre Syndrome)?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHasSeizureHistory" type="button" class="button">Yes</button>
                        <button id="noptHasSeizureHistory" type="button" class="button">No</button>
                        <button id="naptHasSeizureHistory" type="button" class="button">Don't Know</button>
                        <input name="ptHasSeizureHistory" id="ptHasSeizureHistory" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Do you have cancer, leukemia, AIDS, or any other immune system problem? (in some circumstances you may be referred to your physician)</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHasImmuneProblem" type="button" class="button">Yes</button>
                        <button id="noptHasImmuneProblem" type="button" class="button">No</button>
                        <button id="naptHasImmuneProblem" type="button" class="button">Don't Know</button>
                        <input name="ptHasImmuneProblem" id="ptHasImmuneProblem" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Do you take prednisone, other steroids, or anticancer drugs, or have you had radiation treatments?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptTakesCancerDrugs" type="button" class="button">Yes</button>
                        <button id="noptTakesCancerDrugs" type="button" class="button">No</button>
                        <button id="naptTakesCancerDrugs" type="button" class="button">Don't Know</button>
                        <input name="ptTakesCancerDrugs" id="ptTakesCancerDrugs" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>During the past year, have you received a transfusion of blood or blood products, including anti bodies?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptReceivedTransfusion" type="button" class="button">Yes</button>
                        <button id="noptReceivedTransfusion" type="button" class="button">No</button>
                        <button id="naptReceivedTransfusion" type="button" class="button">Don't Know</button>
                        <input name="ptReceivedTransfusion" id="ptReceivedTransfusion" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Are you parent, family member, or caregiver to a new born infant?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptIsInfantCaregiver" type="button" class="button">Yes</button>
                        <button id="noptIsInfantCaregiver" type="button" class="button">No</button>
                        <button id="naptIsInfantCaregiver" type="button" class="button">Don't Know</button>
                        <input name="ptIsInfantCaregiver" id="ptIsInfantCaregiver" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Are you pregnant or could you become pregnant in the next three months?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptIsPregnant" type="button" class="button">Yes</button>
                        <button id="noptIsPregnant" type="button" class="button">No</button>
                        <button id="naptIsPregnant" type="button" class="button">Don't Know</button>
                        <input name="ptIsPregnant" id="ptIsPregnant" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Will you bring your Immunization Record Card with you?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHasImmRecCard" type="button" class="button">Yes</button>
                        <button id="noptHasImmRecCard" type="button" class="button">No</button>
                        <button id="naptHasImmRecCard" type="button" class="button">Don't Know</button>
                        <input name="ptHasImmRecCard" id="ptHasImmRecCard" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Are you currently enrolled in one of our medication adherence programs at Rite Aid (OneTrip Refill, Automated Courtesy Refills, or Rx Messaging - Text, Email, Phone)?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHasMedAdherenceProgram" type="button" class="button">Yes</button>
                        <button id="noptHasMedAdherenceProgram" type="button" class="button">No</button>
                        <button id="naptHasMedAdherenceProgram" type="button" class="button">Don't Know</button>
                        <input name="ptHasMedAdherenceProgram" id="ptHasMedAdherenceProgram" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Have you had a pneumococcal vaccine? (You may need two different pneumococcal shots)</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHadFluShot" type="button" class="button">Yes</button>
                        <button id="noptHadFluShot" type="button" class="button">No</button>
                        <button id="naptHadFluShot" type="button" class="button">Don't Know</button>
                        <input name="ptHadFluShot" id="ptHadFluShot" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Have you had a shingles vaccine?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHadShinglesShot" type="button" class="button">Yes</button>
                        <button id="noptHadShinglesShot" type="button" class="button">No</button>
                        <button id="naptHadShinglesShot" type="button" class="button">Don't Know</button>
                        <input name="ptHadShinglesShot" id="ptHadShinglesShot" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
                <div class="form__group">
                    <div class="form__row question"><p>Have you had a whooping cough(Tdap/Td) vaccine?</p></div>
                    <div class="form__row btn-group">
                        <button id="ysptHadWhoopShot" type="button" class="button">Yes</button>
                        <button id="noptHadWhoopShot" type="button" class="button">No</button>
                        <button id="naptHadWhoopShot" type="button" class="button">Don't Know</button>
                        <input name="ptHadWhoopShot" id="ptHadWhoopShot" class="form__input questionscreen4" hidden="">
                    </div>
                </div>
				<div class="form__group">
                	<div class="form__row question">
                    	<label for="ptHasOtherMedicalCondition">Do you have any other medical conditions? (optional)</label>
                        <textarea id="ptHasOtherMedicalCondition" name="ptHasOtherMedicalCondition" class="form__input" aria-label="Enter other medical conditions"></textarea><br>
                	</div>
            	</div>
             </div>
             *
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
                FindFieldAndSendText(browser, wait, By.XPath("//*[@id=\"lastName\"]"), data.BirthDate);
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
                browser.FindElement(By.XPath("//*[@id=\"patient_state\"]")).SendKeys(data.State + "\t");

                // Zip
                FindFieldAndSendText(browser, wait, By.XPath("//*[@id=\"zip\"]"), data.Zip);

                // sms checkbox
                var checkbox = browser.ScrollElementIntoView("//*[@id=\"sendReminderSMS\"]", clickable: true);
                //Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
                checkbox.Click();

                // email checkbox
                checkbox = browser.ScrollElementIntoView("//*[@id=\"sendReminderEmail\"]", clickable: true);
                //Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
                checkbox.Click();

                // sms checkbox
                var slider = browser.ScrollElementIntoView("//*[@id=\"physician\"]", clickable: true);
                //Thread.Sleep(1000);  // can't seem to find the right waits to avoid this
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

        private static void FindFieldAndSendText(ChromeDriver browser, WebDriverWait wait, By by, string value)
        {
            wait.Until(ExpectedConditions.ElementExists(by));
            var field = browser.ScrollElementIntoView(by);
            field.SendKeys(value);
        }
    }
}
