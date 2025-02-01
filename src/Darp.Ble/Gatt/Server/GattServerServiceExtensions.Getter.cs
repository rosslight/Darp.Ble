using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Gatt.Server;

public static partial class GattServerServiceExtensions
{
    /// <summary> Tries to get a characteristic that was already discovered </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="characteristicDeclaration"> The characteristic definition </param>
    /// <param name="characteristic"> The resulting characteristic. Null if not present </param>
    /// <typeparam name="TProp1"> The property of the first characteristic </typeparam>
    /// <returns> True, when the characteristic was found; False, otherwise </returns>
    public static bool TryGetCharacteristic<TProp1>(this IGattServerService service,
        CharacteristicDeclaration<TProp1> characteristicDeclaration,
        [NotNullWhen(true)] out GattServerCharacteristic<TProp1>? characteristic)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        foreach (IGattServerCharacteristic serverCharacteristic in service.Characteristics)
        {
            if (serverCharacteristic.Uuid == characteristicDeclaration.Uuid
                && characteristicDeclaration.Properties.HasFlag(serverCharacteristic.Property))
            {
                characteristic = new GattServerCharacteristic<TProp1>(serverCharacteristic);
                return true;
            }
        }
        characteristic = null;
        return false;
    }

    /// <summary> Get a characteristic that was already discovered </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="characteristicDeclaration"> The characteristic definition </param>
    /// <typeparam name="TProp1"> The property of the first characteristic </typeparam>
    /// <returns> The gatt server characteristic </returns>
    /// <exception cref="Exception"> Thrown if no characteristic was found </exception>
    public static GattServerCharacteristic<TProp1> GetCharacteristic<TProp1>(this IGattServerService service,
        CharacteristicDeclaration<TProp1> characteristicDeclaration)
        where TProp1 : IBleProperty
    {
        if (!service.TryGetCharacteristic(characteristicDeclaration, out GattServerCharacteristic<TProp1>? characteristic))
            throw new Exception($"Characteristic {characteristicDeclaration.Uuid} not found");
        return characteristic;
    }

    /// <summary> Tries to get a characteristic that was already discovered </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="characteristicDeclaration"> The characteristic definition </param>
    /// <param name="characteristic"> The resulting characteristic. Null if not present </param>
    /// <typeparam name="TProp1"> The property of the first characteristic </typeparam>
    /// <typeparam name="TProp2"> The property of the first characteristic </typeparam>
    /// <returns> True, when the characteristic was found; False, otherwise </returns>
    public static bool TryGetCharacteristic<TProp1, TProp2>(this IGattServerService service,
        CharacteristicDeclaration<TProp1, TProp2> characteristicDeclaration,
        [NotNullWhen(true)] out GattServerCharacteristic<TProp1, TProp2>? characteristic)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        foreach (IGattServerCharacteristic serverCharacteristic in service.Characteristics)
        {
            if (serverCharacteristic.Uuid == characteristicDeclaration.Uuid
                && characteristicDeclaration.Properties.HasFlag(serverCharacteristic.Property))
            {
                characteristic = new GattServerCharacteristic<TProp1, TProp2>(serverCharacteristic);
                return true;
            }
        }
        characteristic = null;
        return false;
    }

    /// <summary> Get a characteristic that was already discovered </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="characteristicDeclaration"> The characteristic definition </param>
    /// <typeparam name="TProp1"> The property of the first characteristic </typeparam>
    /// <typeparam name="TProp2"> The property of the second characteristic </typeparam>
    /// <returns> The gatt server characteristic </returns>
    /// <exception cref="Exception"> Thrown if no characteristic was found </exception>
    public static GattServerCharacteristic<TProp1, TProp2> GetCharacteristic<TProp1, TProp2>(this IGattServerService service,
        CharacteristicDeclaration<TProp1, TProp2> characteristicDeclaration)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        if (!service.TryGetCharacteristic(characteristicDeclaration, out GattServerCharacteristic<TProp1, TProp2>? characteristic))
            throw new Exception($"Characteristic {characteristicDeclaration.Uuid} not found");
        return characteristic;
    }

    /// <summary> Tries to get a characteristic of a given type that was already discovered </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="characteristicDeclaration"> The characteristic definition </param>
    /// <param name="characteristic"> The resulting characteristic. Null if not present </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The property of the first characteristic </typeparam>
    /// <returns> True, when the characteristic was found; False, otherwise </returns>
    public static bool TryGetCharacteristic<T, TProp1>(this IGattServerService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration,
        [NotNullWhen(true)] out TypedGattServerCharacteristic<T, TProp1>? characteristic)
        where TProp1 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        foreach (IGattServerCharacteristic serverCharacteristic in service.Characteristics)
        {
            if (serverCharacteristic.Uuid == characteristicDeclaration.Uuid
                && characteristicDeclaration.Properties.HasFlag(serverCharacteristic.Property))
            {
                characteristic = new TypedGattServerCharacteristic<T, TProp1>(serverCharacteristic,
                    characteristicDeclaration.ReadValue,
                    characteristicDeclaration.WriteValue);
                return true;
            }
        }
        characteristic = null;
        return false;
    }

    /// <summary> Get a characteristic of a given type that was already discovered </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="characteristicDeclaration"> The characteristic definition </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The property of the first characteristic </typeparam>
    /// <returns> The gatt server characteristic </returns>
    /// <exception cref="Exception"> Thrown if no characteristic was found </exception>
    public static TypedGattServerCharacteristic<T, TProp1> GetCharacteristic<T, TProp1>(this IGattServerService service,
        TypedCharacteristicDeclaration<T, TProp1> characteristicDeclaration)
        where TProp1 : IBleProperty
    {
        if (!service.TryGetCharacteristic(characteristicDeclaration, out TypedGattServerCharacteristic<T, TProp1>? characteristic))
            throw new Exception($"Characteristic {characteristicDeclaration.Uuid} not found");
        return characteristic;
    }

    /// <summary> Tries to get a characteristic of a given type that was already discovered </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="characteristicDeclaration"> The characteristic definition </param>
    /// <param name="characteristic"> The resulting characteristic. Null if not present </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The property of the first characteristic </typeparam>
    /// <typeparam name="TProp2"> The property of the first characteristic </typeparam>
    /// <returns> True, when the characteristic was found; False, otherwise </returns>
    public static bool TryGetCharacteristic<T, TProp1, TProp2>(this IGattServerService service,
        TypedCharacteristicDeclaration<T, TProp1, TProp2> characteristicDeclaration,
        [NotNullWhen(true)] out TypedGattServerCharacteristic<T, TProp1, TProp2>? characteristic)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        foreach (IGattServerCharacteristic serverCharacteristic in service.Characteristics)
        {
            if (serverCharacteristic.Uuid == characteristicDeclaration.Uuid
                && characteristicDeclaration.Properties.HasFlag(serverCharacteristic.Property))
            {
                characteristic = new TypedGattServerCharacteristic<T, TProp1, TProp2>(serverCharacteristic,
                    characteristicDeclaration.ReadValue,
                    characteristicDeclaration.WriteValue);
                return true;
            }
        }
        characteristic = null;
        return false;
    }

    /// <summary> Get a characteristic of a given type that was already discovered </summary>
    /// <param name="service"> The service the characteristic belongs to </param>
    /// <param name="characteristicDeclaration"> The characteristic definition </param>
    /// <typeparam name="T"> The type of the characteristic value </typeparam>
    /// <typeparam name="TProp1"> The property of the first characteristic </typeparam>
    /// <typeparam name="TProp2"> The property of the second characteristic </typeparam>
    /// <returns> The gatt server characteristic </returns>
    /// <exception cref="Exception"> Thrown if no characteristic was found </exception>
    public static TypedGattServerCharacteristic<T, TProp1, TProp2> GetCharacteristic<T, TProp1, TProp2>(this IGattServerService service,
        TypedCharacteristicDeclaration<T, TProp1, TProp2> characteristicDeclaration)
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty
    {
        if (!service.TryGetCharacteristic(characteristicDeclaration, out TypedGattServerCharacteristic<T, TProp1, TProp2>? characteristic))
            throw new Exception($"Characteristic {characteristicDeclaration.Uuid} not found");
        return characteristic;
    }
}