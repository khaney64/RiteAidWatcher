# RiteAidWatcher
Program to watch stores in specific zip code for open slots for covid vaccines.

The code will take the zip code and build out a list of stores from that zip code, and expand out from those zip codes up to a list of up to 60 stores (hard coded for now).

It checks for open slots at each of those stores at a reasonable rate.


usage:
  RiteAidWatcher datafile

where
- datafile is a json formatted file with the following fields
```
{
  "Data": {
    "FirstName": "John",
    "LastName": "User",
    "BirthDate": "01/02/1934",
    "StreetAddress": "123 Main Street",
    "City": "Springfield",
    "StateCode": "PA",
    "StateName": "Pennsylvania",
    "Zip": "19064",
    "MobilePhone": "(610) 555-1212",
    "EmailAddress": "john.user@mailserver.com",
    "Occupation": "ChildcareWorker",
    "Condition": "Cancer",
    "OtherConditions": "cancer",
    "Sex" : "Male",
    "Hispanic": "NotHispanicOrLatino",
    "Race": "White"
  },
  "MaxMiles": 50,
  "Filter": true,
  "BrowserCheck": true
}
```
## Data
The Data section defines data points to fill in forms during the process.
The following are required to get past the covid-qualifier page:
- BirthDate
- City
- StateName
- Zip
- Occupation
- Condition

The next few are necessary for the medical questionaire:
- Sex
- Hispanic
- Race

### Occupation
Currently supports
- Childcare Worker
- None of the Above

The code can easily be updated to add others as needed, just update the enum, and the Format method in extensions.cs.

### Condition
Currently supports 
- Cancer
- Diabetes
- Obesity
- Weakened Immune System

The code can easily be updated to add other Occupations or Conditions as needed, just update the enum, and the Format method in extensions.cs.

### Sex
- Female
- Male
- DeclineToAnswer

### Hispanic
- HispanicorLatino
- NotHispanicorLatino
- UnknownEthnicity

### Race
- AmericanIndianorAlaskaNative
- Asian
- BlackorAfricanAmerican
- NativeHawaiianorOtherPacificIslander
- White

## Qualification and rules
Occupation, Condtion and Age are critical in getting past the qualifier page, and the rules seems to change every so often.  As I write this RiteAid is only qualifying teachers in PA.  

The rules are defined [here](https://www.riteaid.com/content/dam/riteaid-web/covid-19/rule-engine.json) and the code will dump out any rules it finds for the given StateCode.  If you're having trouble getting past the qualifier page, you are likely running up against the rules.

## Options
- MaxMiles - how far are you willing to go from the home zip code?
- Filter - specific to PA right now, if true will exclude Philadelphia stores
- BrowserCheck - if true, will do additional checks driving Chrome browser via [ChromeDriver](https://chromedriver.chromium.org/downloads)
**NOTE** that the chromedriver.exe must be in your current path.
If Browsercheck is true, the other Data elements will be used on other pages if it finds a slot and can get far enough.

The current BrowserCheck code can get as far as the last page.  My plan is *not* to automatically submit the last page, the user should confirm and do that.
It occassionally will time out or fail on some of the intermediate final pages for various reasons, seems to depend on system load, code probably not waiting long enough, etc.
Any failures (or success up to the last page) will stop and leave the browser where it got to, and you should be able to continue manually.  If the program is still running/checking (i.e. not stopped in the debugger) the browser will stay up as long as there are still slots detected, otherwise it'll reset the browser back to the find stores page if slots are no longer available.
A slot found will wait at which ever page it gets to (up to the last), and the browser will be marked as "hold" status for up to 10 minutes (if slots are not found after 10 minutes, it'l reload the browser to the find stores page.
The code currently starts up a BrowserCache of 2 browsers - the only real reason to have more than one is if there happen to be multiple hits at once, but for most cases that's probabl not necessary (and it often leads to a false start where one browser comes up successfully but the other doesn't, which requires a restart. 

If you run without BrowserCheck enabled, the code will just scan the (up to) 60 stores in range and look for slots, and report if any are found.  You'd likely run this side by side with an open browswer already past the [qualification page](https://www.riteaid.com/pharmacy/covid-qualifier) waiting for a zip code to search.  When the code finds a zip with a slot, it'll beep and print out the store and zip code information (it also does this with BrowserCheck enabled).  The browser check option will weed out a lot of the false hits their api often reports, avoiding you the trouble/frustration of checking only to have it tell you no slots are available.
