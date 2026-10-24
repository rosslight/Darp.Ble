using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> Discovery of characteristics by declaration </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "ApiDesign",
    "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "The overloads differ in the characteristic or declaration they take, never in their optional parameters"
)]
public static class RemoteGattServiceExtensions
{
    /// <summary> Discovers the characteristic a declaration describes </summary>
    /// <param name="service"> The service </param>
    /// <param name="declaration"> The declaration </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <typeparam name="TProp1"> The property marker </typeparam>
    /// <returns> The first characteristic with the declared UUID </returns>
    /// <exception cref="GattException"> The service has no such characteristic (NotFound), or it lacks a declared property (PropertyMismatch) </exception>
    public static async Task<RemoteCharacteristic<TProp1>> DiscoverCharacteristicAsync<TProp1>(
        this RemoteGattService service,
        CharacteristicDeclaration<TProp1> declaration,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty =>
        new(await DiscoverAsync(service, declaration, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc cref="DiscoverCharacteristicAsync{TProp1}(RemoteGattService, CharacteristicDeclaration{TProp1}, CancellationToken)" />
    /// <typeparam name="TProp1"> The first property marker </typeparam>
    /// <typeparam name="TProp2"> The second property marker </typeparam>
    public static async Task<RemoteCharacteristic<TProp1, TProp2>> DiscoverCharacteristicAsync<TProp1, TProp2>(
        this RemoteGattService service,
        CharacteristicDeclaration<TProp1, TProp2> declaration,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty =>
        new(await DiscoverAsync(service, declaration, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc cref="DiscoverCharacteristicAsync{TProp1}(RemoteGattService, CharacteristicDeclaration{TProp1}, CancellationToken)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The property marker </typeparam>
    public static async Task<TypedRemoteCharacteristic<T, TProp1>> DiscoverCharacteristicAsync<T, TProp1>(
        this RemoteGattService service,
        TypedCharacteristicDeclaration<T, TProp1> declaration,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty =>
        new(await DiscoverAsync(service, declaration, cancellationToken).ConfigureAwait(false), declaration);

    /// <inheritdoc cref="DiscoverCharacteristicAsync{TProp1}(RemoteGattService, CharacteristicDeclaration{TProp1}, CancellationToken)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The first property marker </typeparam>
    /// <typeparam name="TProp2"> The second property marker </typeparam>
    public static async Task<TypedRemoteCharacteristic<T, TProp1, TProp2>> DiscoverCharacteristicAsync<
        T,
        TProp1,
        TProp2
    >(
        this RemoteGattService service,
        TypedCharacteristicDeclaration<T, TProp1, TProp2> declaration,
        CancellationToken cancellationToken = default
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty =>
        new(await DiscoverAsync(service, declaration, cancellationToken).ConfigureAwait(false), declaration);

    /// <summary> The characteristic a declaration describes among discovered ones, or null when there is none </summary>
    /// <exception cref="GattException"> It exists but lacks a declared property (PropertyMismatch) </exception>
    internal static RemoteGattCharacteristic? Match(
        IReadOnlyList<RemoteGattCharacteristic> characteristics,
        ICharacteristicDeclaration declaration
    )
    {
        RemoteGattCharacteristic? characteristic = characteristics.FirstOrDefault(c => c.Uuid == declaration.Uuid);
        if (characteristic is null)
            return null;
        GattProperty missing = declaration.Properties & ~characteristic.Properties;
        if (missing is not GattProperty.None)
        {
            throw new GattException(
                GattErrorKind.PropertyMismatch,
                $"The characteristic {declaration.Uuid} does not support {missing}"
            );
        }
        return characteristic;
    }

    /// <summary> The characteristic a declaration describes among discovered ones </summary>
    /// <exception cref="GattException"> There is none (NotFound), or it lacks a declared property (PropertyMismatch) </exception>
    internal static RemoteGattCharacteristic Require(
        RemoteGattService service,
        IReadOnlyList<RemoteGattCharacteristic> characteristics,
        ICharacteristicDeclaration declaration
    ) =>
        Match(characteristics, declaration)
        ?? throw new GattException(
            GattErrorKind.NotFound,
            $"The service {service.Uuid} has no characteristic {declaration.Uuid}"
        );

    private static async Task<RemoteGattCharacteristic> DiscoverAsync(
        RemoteGattService service,
        ICharacteristicDeclaration declaration,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(declaration);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await service
            .DiscoverCharacteristicsAsync(cancellationToken)
            .ConfigureAwait(false);
        return Require(service, characteristics, declaration);
    }
}
