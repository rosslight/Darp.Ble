using System.Diagnostics.CodeAnalysis;
using System.Text;

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
                && characteristicDeclaration.Properties.HasFlag(serverCharacteristic.Properties))
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
                && characteristicDeclaration.Properties.HasFlag(serverCharacteristic.Properties))
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
                && characteristicDeclaration.Properties.HasFlag(serverCharacteristic.Properties))
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
                && characteristicDeclaration.Properties.HasFlag(serverCharacteristic.Properties))
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

    /// <summary> Tries to get a descriptor that was already discovered </summary>
    /// <param name="characteristic"> The characteristic the descriptor belongs to </param>
    /// <param name="descriptorDeclaration"> The descriptor definition </param>
    /// <param name="descriptor"> The descriptor if found </param>
    /// <returns> The gatt server characteristic </returns>
    public static bool TryGetDescriptor(this IGattServerCharacteristic characteristic,
        DescriptorDeclaration descriptorDeclaration,
        [NotNullWhen(true)] out IGattServerDescriptor? descriptor)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        ArgumentNullException.ThrowIfNull(descriptorDeclaration);
        return characteristic.Descriptors.TryGetValue(descriptorDeclaration.Uuid, out descriptor);
    }

    /// <summary> Get a descriptor that was already discovered </summary>
    /// <param name="characteristic"> The characteristic the descriptor belongs to </param>
    /// <param name="descriptorDeclaration"> The descriptor definition </param>
    /// <returns> The gatt server characteristic </returns>
    public static IGattServerDescriptor GetDescriptor(this IGattServerCharacteristic characteristic,
        DescriptorDeclaration descriptorDeclaration)
    {
        if (!characteristic.TryGetDescriptor(descriptorDeclaration, out IGattServerDescriptor? descriptor))
            throw new Exception($"Descriptor {descriptorDeclaration.Uuid} not found");
        return descriptor;
    }

    /// <summary> Read the user description descriptor </summary>
    /// <param name="characteristic"> The characteristic the descriptor belongs to </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> The user description </returns>
    public static async Task<string> ReadUserDescriptionAsync(this IGattServerCharacteristic characteristic,
        CancellationToken cancellationToken = default)
    {
        IGattServerDescriptor descriptor = characteristic.GetDescriptor(DescriptorDeclaration.CharacteristicUserDescription);
        byte[] bytes = await descriptor.ReadAsync(cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }
}