using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Examples.Unix.Mockup;

internal sealed class BMAdvGenerator : IObservable<(BleAddress Address, AdvertisingData Data)>, IDisposable
{
    private readonly ObservableCollection<IObserver<(BleAddress Address, AdvertisingData Data)>> m_observers = new();
    private readonly System.Timers.Timer m_timer = new(200);

    public BMAdvGenerator()
    {
        m_observers.CollectionChanged += (sender, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (m_observers.Count == 1)
                    {
                        // Wenn der erste Observer eingetragen wird, Timer starten.
                        m_timer.Start();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (m_observers.Count == 0)
                    {
                        // Wenn der letzte Observer ausgetragen wird, Timer stoppen.
                        m_timer.Stop();
                    }
                    break;
            }
        };

        Random random = new();
        m_timer.Elapsed += (sender, e) =>
        {
            int nRandom = random.Next(10, 21);

            (BleAddress Address, AdvertisingData Data) tuple = new()
            {
                Address = new BleAddress((UInt48)(ulong)nRandom),
                Data = CreateAdvertisementData(nRandom),
            };

            foreach (var observer in m_observers)
            {
                observer.OnNext(tuple);
            }
        };
    }

    private static AdvertisingData CreateAdvertisementData(int nRandom)
    {
        switch (nRandom)
        {
            case 10:
                // Apple Inc - Beacon
                return AdvertisingData.From(Convert.FromHexString("1AFF4C00021550765CB7D9EA4E2199A4FA879613A492CB1AD302CE"));

            case 11:
                // Apple Inc - NearbyInfo
                return AdvertisingData.From(Convert.FromHexString("02011A020A0C0BFF4C001006111E54C734B7"));

            case 12:
                // Apple Inc - NearbyInfo
                return AdvertisingData.From(Convert.FromHexString("02011A020A0C0BFF4C001006091AD0C23F46"));

            case 13:
                // Würth - AdvertisementMode "M-Cube"
                return AdvertisingData.From(Convert.FromHexString("02010603024CFD17FF3D09F2A25C100E43543FD53262635A263C7FEAA90600"));

            case 14:
                // Würth - AdvertisementMode "M-Cube"
                return AdvertisingData.From(Convert.FromHexString("02010603024CFD17FF3D09F2A25C100E43543F952454CD80646C77EAA9B405"));

            case 15:
                // Würth - AdvertisementMode "Tracker"
                return AdvertisingData.From(Convert.FromHexString("02010617FF3D09718A5FADAE270111194A0000000000000000000003024CFD"));

            case 16:
                // Würth - AdvertisementMode "Tracker"
                return AdvertisingData.From(Convert.FromHexString("02010603024CFD13FF3D09812E4DD14C02CE3E1500000000A4800000000000"));

            case 17:
                // Microsoft
                return AdvertisingData.From(Convert.FromHexString("1EFF060001092002FDEDABAF77D53586B144EBEF969BB9D5FAA575EC6EEA4F"));

            case 18:
                // SamsungElectronicsCoLtd
                return AdvertisingData.From(Convert.FromHexString("1EFF75000112766F6E204572696B0000000000000000000000000000000000"));

            case 19:
                // WURTH Smart Battery; ohne ManufacturerSpecificData
                return AdvertisingData.From(Convert.FromHexString("051228002003020A001409575552544820536D6172742042617474657279"));

            case 20:
                // ThreeDiJoyCorporation
                return AdvertisingData.From(Convert.FromHexString("02010617FF54004C9D5FADAE27011119550000000000000000000003024CFD"));
        }

        return AdvertisingData.Empty;
    }


    public IDisposable Subscribe(IObserver<(BleAddress Address, AdvertisingData Data)> observer)
    {
        if (!m_observers.Contains(observer))
            m_observers.Add(observer);

        return Disposable.Create(this, state => state.m_observers.Remove(observer));
    }

    public void Stop()
    {
        // Verwende (Shallow-)Kopie von m_observers, da m_observers während der Iteration verändert wird.
        // OnCompleted führt hier zum Unsubscribe des jeweiligen Observer, was ihn aus m_observers entfernt.
        foreach (var observer in m_observers.ToList())
        {
            observer.OnCompleted();
        }
    }

    public void Dispose()
    {
        Stop();
        m_timer.Stop();
        m_timer.Dispose();
    }

}
