using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Examples.Unix;

internal sealed class AdvGenerator : IObservable<AdvGenerator.DataExt>, IDisposable
{
    internal sealed record DataExt(BleAddress Address, TxPowerLevel TxPower, Rssi Rssi, AdvertisingData Data);

    private readonly ObservableCollection<IObserver<DataExt>> m_observers = new();
    private readonly System.Timers.Timer m_timer = new(200);

    public AdvGenerator()
    {
        m_observers.CollectionChanged += (sender, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (m_observers.Count == 1)
                    {
                        // Start timer when first observer is added
                        m_timer.Start();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (m_observers.Count == 0)
                    {
                        // Stop timer when last observer is removed
                        m_timer.Stop();
                    }
                    break;
            }
        };

        Random random = new();
        m_timer.Elapsed += (sender, e) =>
        {
            int nRandom = random.Next(10, 21);

            var data = new DataExt(
                Address: new BleAddress((UInt48)(ulong)nRandom),
                TxPower: (TxPowerLevel) (nRandom * 3),
                Rssi: (Rssi) (nRandom * -2),
                Data: AdvertisingData.From(BitConverter.GetBytes(nRandom).Reverse().ToArray()));

            foreach (IObserver<DataExt> observer in m_observers)
            {
                observer.OnNext(data);
            }
        };
    }

    public IDisposable Subscribe(IObserver<DataExt> observer)
    {
        if (!m_observers.Contains(observer))
            m_observers.Add(observer);

        return Disposable.Create(this, self => self.m_observers.Remove(observer));
    }

    public void Stop()
    {
        // For iterating use shallow copy of m_observers since m_observers is changed while iterating.
        // OnCompleted unsubscribes observer removing it from m_observers.
        foreach (IObserver<DataExt> observer in m_observers.ToList())
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
