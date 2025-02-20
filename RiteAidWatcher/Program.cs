﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using RiteAidChecker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace RiteAidWatcher
{
    class RiteAidWatcher
    {
        private static IConfigurationRoot Configuration;
        private static IServiceProvider provider;

        const int WaitSecondsBetweenSearch = 1;
        const int WaitSecondsBetweenStores = 1;
        const int WaitSecondsBetweenChecks = 5;
        const int MaxStores = 60;
        const int MaxBrowsers = 2;

        const string BaseAddress = "https://www.riteaid.com";
        const string FetchStoresTemplate = "/services/ext/v2/stores/getStores?address={0}&attrFilter=PREF-112&fetchMechanismVersion=2&radius=50";
        const string FetchSlotsTemplate = "/services/ext/v2/vaccine/checkSlots?storeNumber={0}";

        private readonly List<Alert> Alerts;
        private readonly Notifier Notifier;
        private readonly bool Filter;
        private readonly int MaxMiles = 0;
        private readonly bool BrowserCheck;
        private readonly BrowserCache browserCache;
        private readonly RiteAidData riteAidData;

        async static Task Main(string[] args)
        {
            var datafile = args[0];

            var config = JsonConvert.DeserializeObject<RiteAidConfig>(await File.ReadAllTextAsync(args[0]));

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddUserSecrets<NotifierConfiguration>();
            Configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();
            services.AddHttpClient("riteaid", c =>
            {
                c.BaseAddress = new Uri(BaseAddress);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                c.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true
                };
            });
            services.AddSingleton(new NotifierConfiguration(Configuration));

            provider = services.BuildServiceProvider();

            try
            {
                await new RiteAidWatcher(config).Watch(config.Data.Zip);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected fatal exception : {e.Message}");
                Console.WriteLine(e.StackTrace);
            }
        }

        private RiteAidWatcher(RiteAidConfig riteAidConfig)
        {
            Alerts = new List<Alert>();
            var configuration = provider.GetService<NotifierConfiguration>();
            Notifier = new Notifier(configuration);
            Filter = riteAidConfig.Filter;
            MaxMiles = riteAidConfig.MaxMiles;
            BrowserCheck = riteAidConfig.BrowserCheck;
            riteAidData = riteAidConfig.Data;
            DumpStateRules(riteAidData);
            if (BrowserCheck)
            {
                browserCache = new BrowserCache(MaxBrowsers, riteAidData, Checker.Initializer, Checker.Resetter);
                browserCache.Preload();
            }
        }

        private void DumpStateRules(RiteAidData data)
        {
            var jsonResponse = FetchRules().GetAwaiter().GetResult();
            var rules = JsonConvert.DeserializeObject<RulesRoot>(jsonResponse);
            var found = false;
            foreach (var conditions in rules.Conditions.Any)
            {
                if (conditions.All.Exists(c => c.Fact == "stateCode" && (string)c.Value == data.StateCode))
                {
                    found = true;
                    foreach (var condition in conditions.All)
                    {
                        Console.WriteLine($"{condition.Fact} {condition.Operator} {condition.Value}");
                    }
                }
            }

            if (!found)
            {
                Console.WriteLine($"No rules found for state {data.StateCode} - may not be able to find slots");
            }
        }

        private async Task Watch(string zip)
        {
            //var stores = (await FetchStoreData(zips)).ToList();
            var stores = (await BuildStores(zip)).ToList();
            Console.WriteLine($"Found {stores.Count} stores from zip code {zip}");
            foreach (var store in stores)
            {
                Console.WriteLine($"Watching store {store.storeNumber} ({store.milesFromCenter:0.00} miles) {store.address} {store.city} {store.zipcode}");
            }

            Console.WriteLine($"{DateTime.Now:s} {WaitSecondsBetweenStores} second delay between stores, {WaitSecondsBetweenChecks} second delay between checks");

            do
            {
                //Console.WriteLine($"{DateTime.Now:s} : checking ({WaitSecondsBetweenStores} second delay between stores)");
                var haveActive = false;
                foreach (var store in stores)
                {
                    var slot = await Check(store);
                    if (slot != null)
                    {
                        haveActive |= slot.Slot1 || slot.Slot2;
                        ProcessStoreSlot(store, slot);
                    }
                    Thread.Sleep(WaitSecondsBetweenStores * 1000);
                }
                //Console.WriteLine($"{DateTime.Now:s} : sleeping for {WaitSecondsBetweenChecks} seconds");
                CheckAlerts(haveActive);
                Thread.Sleep(WaitSecondsBetweenChecks * 1000);
            } while (true);
        }

        /// <summary>
        /// Build out a list of stores from the given zip code.
        /// It'll get an initial list of stores (10) from main zip code.  Will then continue to get 10 stores from surrounding zips until we hit our max (or maybe run out of zips)
        /// </summary>
        /// <param name="zip"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Store>> BuildStores(string zip)
        {
            Console.WriteLine($"{DateTime.Now:s} : Building store list from {zip}");
            var results = new List<Store>();
            var checkedZips = new List<string>();

            var jsonResponse = await FetchStoresForZip(zip);
            var centerStore = JsonConvert.DeserializeObject<StoreRoot>(jsonResponse);
            results.AddRange(centerStore.Data.stores);
            results = FilterStores(results).ToList();
            checkedZips.Add(zip);

            var haveUnchecked = results.Exists(s => !checkedZips.Contains(s.zipcode));
            while (results.Count < MaxStores && haveUnchecked)
            {
                for (var s = 0; s < results.Count; s++)
                {
                    var store = results[s];
                    if (checkedZips.Contains(store.zipcode))
                        continue;

                    Thread.Sleep(250);
                    //Thread.Sleep(WaitSecondsBetweenSearch * 1000);
                    var zipStore = JsonConvert.DeserializeObject<StoreRoot>(await FetchStoresForZip(store.zipcode));
                    if (zipStore.Data == null)
                    {
                        Console.WriteLine($"zip {store.zipcode} returned no data - skipping");
                        continue;
                    }
                    results.AddRange(zipStore.Data.stores);
                    results = FilterStores(results).ToList();
                    checkedZips.Add(zip);

                    haveUnchecked = results.Exists(s => !checkedZips.Contains(s.zipcode));
                    if (results.Count >= MaxStores * 3 || !haveUnchecked)
                    {
                        Console.WriteLine($"Stopping at {results.Count} stores, have unchecked is {haveUnchecked}");
                        break;
                    }
                }
            }

            foreach (var store in results)
            {
                if (store.zipcode == zip)
                {
                    store.milesFromCenter = 0.0;
                }
                else
                {
                    store.milesFromCenter = CalculateDistance(results[0], store);
                }
            }

            return results.FindAll(s => s.milesFromCenter < MaxMiles).OrderBy(s => s.milesFromCenter).Take(MaxStores);
        }

        private double CalculateDistance(Store centerStore, Store store)
        {
            var d1 = centerStore.latitude * (Math.PI / 180.0);
            var num1 = centerStore.longitude * (Math.PI / 180.0);
            var d2 = store.latitude * (Math.PI / 180.0);
            var num2 = store.longitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
            var meters = 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));

            return meters * 0.000621371;
        }

        private IEnumerable<Store> FilterStores(List<Store> results)
        {
            if (Filter)
            {
                // remove Philadelphia stores
                results.RemoveAll(s => s.city.ToLower() == "philadelphia");

                // remove non PA stores
                results.RemoveAll(s => s.state.ToLower() != "pa");
            }

            // there may be overlap by zip, return unique list of stores
            return results.GroupBy(s => s.storeNumber).Select(s => s.First());
        }

        private async Task<string> FetchStoresForZip(string zip)
        {
            var uri = String.Format(FetchStoresTemplate, zip);
            return await FetchJsonResponse(uri);
        }

        private void CheckAlerts(bool haveActive)
        {
            var activeAlert = Alerts.Find(a => a.AlertStatus != AlertStatusType.Complete);
            if (activeAlert == null)
            {
                return;
            }

            if (activeAlert.AlertStatus == AlertStatusType.Active && haveActive == false)
            {
                Console.WriteLine($"{DateTime.Now:s} : Ending alert - {activeAlert.ActiveStores.Keys.Count} active stores");
                foreach (var store in activeAlert.ActiveStores)
                {
                    var duration = (store.Value.End.Value - store.Value.Start.Value).TotalMinutes;
                    Console.WriteLine($"{DateTime.Now:s} : Ending store {store.Value.StoreNumber} - Started {store.Value.Start.Value:s} Ended {store.Value.End.Value:s} ({duration:###0} minutes)");
                }
                Console.WriteLine($"---------------------------------------------------------------");
                // send an email or some notification here
                activeAlert.AlertStatus = AlertStatusType.Complete;
            }

            if (activeAlert.AlertStatus == AlertStatusType.New)
            {
                Console.WriteLine($"{DateTime.Now:s} : Starting alert - {activeAlert.ActiveStores.Keys.Count} active stores");
                // send an email or some notification here
                activeAlert.AlertStatus = AlertStatusType.Active;
            }

        }

        private void ProcessStoreSlot(Store store, Slots slot)
        {
            var activeAlert = Alerts.Find(a => a.AlertStatus == AlertStatusType.Active || a.AlertStatus == AlertStatusType.New);
            AlertData storeAlert = null;
            activeAlert?.ActiveStores.TryGetValue(store.storeNumber, out storeAlert);
            if (slot.Slot1 || slot.Slot2)
            {
                // see if there is an active alert, if so we'll add to that, otherwise create a new one
                if (activeAlert == null)
                {
                    activeAlert = new Alert()
                    {
                        AlertStatus = AlertStatusType.New,
                        ActiveStores = new Dictionary<int, AlertData>(),
                    };
                    Alerts.Add(activeAlert);
                }

                var hasSlots = true;
                if (storeAlert == null)
                {
                    storeAlert = new AlertData()
                    {
                        StoreNumber = store.storeNumber,
                        ZipCode = store.zipcode,
                        Slot1 = slot.Slot1,
                        Slot2 = slot.Slot2,
                        Start = DateTime.Now,
                        Status = StoreStatusType.New
                    };
                    activeAlert.ActiveStores.Add(store.storeNumber, storeAlert);
                }

                var checkInfo = "";
                if (BrowserCheck && (storeAlert.Status == StoreStatusType.New || storeAlert.Status == StoreStatusType.Retry))
                {
                    //Console.Beep(600, 200);
                    var checkStatus = CheckStore(store);
                    hasSlots = checkStatus.slots;
                    checkInfo = checkStatus.info;
                    storeAlert.Browser = checkStatus.browser;
                    storeAlert.LastCheck = DateTime.Now;
                    storeAlert.Tries++;
                }

                if (hasSlots && storeAlert.Status != StoreStatusType.Wait)
                {
                    Console.Beep(600, 800);
                    Console.WriteLine(
                        $"{DateTime.Now:s} : Store {store.storeNumber} ({store.milesFromCenter:0.00} miles) {store.address} {store.city} {store.zipcode} has slots {slot.Slot1} {slot.Slot2} ({storeAlert.Status})");
                    storeAlert.Status = StoreStatusType.Hold;
                }
                else
                {
                    // try 3 times then wait a bit before retrying
                    if (storeAlert.Tries > 3)
                    {
                        storeAlert.Status = StoreStatusType.Wait;
                    }
                    else
                    {
                        storeAlert.Status = StoreStatusType.Retry;
                    }

                    if (storeAlert.Status == StoreStatusType.Wait)
                    {
                        Console.WriteLine($"{DateTime.Now:s} : Store {store.storeNumber} ({store.milesFromCenter:0.00} miles) {store.zipcode} reported slots - waiting");
                        if (DateTime.Now.Subtract(storeAlert.LastCheck.Value).Minutes > 3)
                        {
                            storeAlert.Tries = 0;
                            storeAlert.Status = StoreStatusType.Retry;
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            $"{DateTime.Now:s} : Store {store.storeNumber} ({store.milesFromCenter:0.00} miles) {store.zipcode} reported slots but none found ({checkInfo}) ({storeAlert.Tries} tries)");
                    }
                }
            }
            else
            {
                if (storeAlert != null)
                {
                    // if we were holding, see if it's long enough to remove it
                    if (storeAlert.Status == StoreStatusType.Hold && DateTime.Now.Subtract(storeAlert.LastCheck.Value).Minutes > 10)
                    {
                        storeAlert.Status = StoreStatusType.Wait;
                        browserCache.Release(storeAlert.Browser);
                    }

                    var removed = false;
                    // see if this store was active - if so, mark the end date (but not if Hold status)
                    if (storeAlert.End == null && storeAlert.Status != StoreStatusType.Hold)
                    {
                        removed = true;
                        try
                        {
                            browserCache.Push(storeAlert.Browser);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (removed)
                    {
                        storeAlert.End = DateTime.Now;
                        storeAlert.Browser = null;
                        Console.WriteLine($"{DateTime.Now:s} : Store {store.storeNumber} no longer has slots");
                    }

                    var activeStores = activeAlert.ActiveStores.Values.ToList().FindAll(a => a.End == null);
                    if (removed && activeStores.Count > 0)
                    {
                        foreach (var activeStore in activeStores)
                        {
                            Console.WriteLine(
                                $"{DateTime.Now:s} : Store {activeStore.StoreNumber} zip {activeStore.ZipCode} still has active slots ({activeStore.Status})");
                        }
                    }
                }
            }
        }

        private async Task<Slots> Check(Store store)
        {
            var jsonResponse = await FetchSlotsForStore(store);

            var root = JsonConvert.DeserializeObject<SlotsRoot>(jsonResponse);

            if (root?.Data?.Slots == null)
            {
                Console.WriteLine($"{DateTime.Now:s} : Bad data returned for store {store.storeNumber} {jsonResponse}");
            }

            return root?.Data?.Slots;
        }

        private async Task<string> FetchSlotsForStore(Store store)
        {
            var uri = String.Format(FetchSlotsTemplate, store.storeNumber);
            return await FetchJsonResponse(uri);
        }

        private async Task<string> FetchRules()
        {
            var uri = "/content/dam/riteaid-web/covid-19/rule-engine.json";
            return await FetchJsonResponse(uri);
        }

        private async Task<string> FetchJsonResponse(string uri)
        {
            var httpFactory = provider.GetService<IHttpClientFactory>();
            var httpClient = httpFactory.CreateClient("riteaid");
            var response = await httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private (bool slots, ChromeDriver browser, string info) CheckStore(Store store)
        {
            Console.WriteLine($"{DateTime.Now:s} : Checking store {store.storeNumber} ({store.milesFromCenter:0.00} miles) {store.address} {store.city} {store.zipcode} with browser");

            var browser = browserCache.Pop();
            if (browser == null)
            {
                Console.WriteLine($"{DateTime.Now:s} : No browsers remaining to be able to check");
                return (true, null, "");
            }

            try
            {
                var slots = Checker.Check(store.zipcode, store.storeNumber.ToString(), riteAidData, browser);
                if (!slots.haveSlots)
                {
                    browserCache.Push(browser);
                    return (false, null, slots.info );
                }
                else
                {
                    browserCache.Hold(browser);
                    return (true, browser, slots.info);
                }
            }
            catch (Exception e)
            {
                Console.Beep(200, 500); // debug
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
                browserCache.Push(browser);
                return (false, null, "exception");
            }
        }
    }
}
