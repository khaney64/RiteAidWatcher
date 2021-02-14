using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace RiteAidWatcher
{
    class RiteAidWatcher
    {
        const int WaitSecondsBetweenSearch = 3;
        const int WaitSecondsBetweenStores = 1;
        const int WaitSecondsBetweenChecks = 5;
        const int MaxStores = 60;

        const string BaseAddress = "https://www.riteaid.com";
        const string FetchStoresTemplate = "/services/ext/v2/stores/getStores?address={0}&attrFilter=PREF-112&fetchMechanismVersion=2&radius=50";
        const string FetchSlotsTemplate = "/services/ext/v2/vaccine/checkSlots?storeNumber={0}";

        private readonly HttpClient HttpClient;
        private readonly List<Alert> Alerts;

        async static Task Main(string[] args)
        {
            var zip = args[0];

            var cookies = new CookieContainer(); // load/save this?

            var handler = new HttpClientHandler() { CookieContainer = cookies };
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };
            await new RiteAidWatcher(client).Watch(zip);
        }

        private RiteAidWatcher(HttpClient client)
        {
            HttpClient = client;
            Alerts = new List<Alert>();
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
                    haveActive |= slot.Slot1 || slot.Slot2;
                    ProcessStoreSlot(store, slot);
                    Thread.Sleep(WaitSecondsBetweenStores * 1000);
                }
                //Console.WriteLine($"{DateTime.Now:s} : sleeping for {WaitSecondsBetweenChecks} seconds");
                CheckAlerts(haveActive);
                Thread.Sleep(WaitSecondsBetweenChecks * 1000);
            } while(true);
        }

        /// <summary>
        /// Build out a list of stores from the given zip code.
        /// It'll get an initial list of stores (10) from main zip code.  Will then continue to get 10 stores from surrounding zips until we hit our max (or maybe run out of zips)
        /// </summary>
        /// <param name="zip"></param>
        /// <returns></returns>
        private async Task<IEnumerable<Store>> BuildStores(string zip)
        {
            var results = new List<Store>();
            var checkedZips = new List<string>();

            var jsonResponse = await FetchStoresForZip(zip);
            var root = JsonConvert.DeserializeObject<StoreRoot>(jsonResponse);
            results.AddRange(root.Data.stores);
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

                    var zipRoot = JsonConvert.DeserializeObject<StoreRoot>(await FetchStoresForZip(store.zipcode));
                    results.AddRange(zipRoot.Data.stores);
                    results = FilterStores(results).ToList();
                    checkedZips.Add(zip);

                    if (results.Count >= MaxStores)
                        break;
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

            return results.OrderBy(s => s.milesFromCenter);
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
            // remove Philadelphia stores
            results.RemoveAll(s => s.city.ToLower() == "philadelphia");

            // remove non PA stores
            results.RemoveAll(s => s.state.ToLower() != "pa");

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
                    Console.WriteLine($"{DateTime.Now:s} : Store {store.Value.StoreNumber} - Start {store.Value.Start.Value:s} End {store.Value.End.Value:s} ({duration:###0} minutes)");
                }
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
                        ActiveStores = new Dictionary<int, AlertData>()
                    };
                    Alerts.Add(activeAlert);
                }

                if (storeAlert == null)
                {
                    storeAlert = new AlertData() { StoreNumber = store.storeNumber, Start = DateTime.Now };
                    activeAlert.ActiveStores.Add(store.storeNumber, storeAlert);
                    Console.WriteLine($"{DateTime.Now:s} : Store {store.storeNumber} ({store.milesFromCenter:0.00} miles) {store.address} {store.city} {store.zipcode} has slots {slot.Slot1} {slot.Slot2}");
                }
                storeAlert.Slot1 = slot.Slot1;
                storeAlert.Slot2 = slot.Slot2;
            }
            else
            {
                // see if this store was active - if so, mark the end date
                if (storeAlert != null)
                {
                    if (storeAlert.End == null)
                    {
                        Console.WriteLine($"{DateTime.Now:s} : Store {store.storeNumber} no longer has slots");
                    }
                    storeAlert.End = DateTime.Now;
                }
            }
        }

        private async Task<Slots> Check(Store store)
        {
            var jsonResponse = await FetchSlotsForStore(store);

            var root = JsonConvert.DeserializeObject<SlotsRoot>(jsonResponse);

            return root.Data.Slots;
        }

        private async Task<string> FetchSlotsForStore(Store store)
        {
            var uri = String.Format(FetchSlotsTemplate, store.storeNumber);
            return await FetchJsonResponse(uri);
        }

        private async Task<string> FetchJsonResponse(string uri)
        {
            var response = await HttpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

    }
}
