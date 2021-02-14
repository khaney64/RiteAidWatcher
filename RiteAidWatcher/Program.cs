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

        const string BaseAddress = "https://www.riteaid.com";
        const string FetchStoresTemplate = "/services/ext/v2/stores/getStores?address={0}&attrFilter=PREF-112&fetchMechanismVersion=2&radius=50";
        const string FetchSlotsTemplate = "/services/ext/v2/vaccine/checkSlots?storeNumber={0}";

        private readonly HttpClient HttpClient;
        private readonly List<Alert> Alerts;

        async static Task Main(string[] args)
        {
            var zips = new List<string>(args[0].Split(","));

            var cookies = new CookieContainer(); // load/save this?

            var handler = new HttpClientHandler() { CookieContainer = cookies };
            var client = new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };
            await new RiteAidWatcher(client).Watch(zips);
        }

        private RiteAidWatcher(HttpClient client)
        {
            HttpClient = client;
            Alerts = new List<Alert>();
        }


        private async Task Watch(List<string> zips)
        {
            var stores = (await FetchStoreData(zips)).ToList();
            Console.WriteLine($"Found {stores.Count} stores");
            foreach (var store in stores)
            {
                Console.WriteLine($"Watching store {store.storeNumber} {store.address} {store.city} {store.zipcode}");
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

        private async Task<IEnumerable<Store>> FetchStoreData(List<string> zips)
        {
            var results = new List<Store>();

            foreach (var zip in zips)
            {
                Console.WriteLine($"Searching zip {zip}");
                var jsonResponse = await FetchStoresForZip(zip);
                var root = JsonConvert.DeserializeObject<StoreRoot>(jsonResponse);

                results.AddRange(root.Data.stores);
                Thread.Sleep(WaitSecondsBetweenSearch * 1000);
            }

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
                    var duration = (store.Value.End.Value - store.Value.End.Value).TotalMinutes;
                    Console.WriteLine($"{DateTime.Now:s} : Store {store.Value.StoreNumber} - Start {store.Value.Start.Value:s} End {store.Value.End.Value:s} ({duration} minutes)");
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
                    Console.WriteLine($"{DateTime.Now:s} : Store {store.storeNumber} {store.address} {store.city} {store.zipcode} has slots {slot.Slot1} {slot.Slot2}");
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
