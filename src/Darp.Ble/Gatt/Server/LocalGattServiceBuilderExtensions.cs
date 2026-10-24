using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> Adds characteristics by declaration; the declaration decides the properties and, when typed, encodes and decodes </summary>
[SuppressMessage(
    "ApiDesign",
    "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "The overloads differ in the declaration they take, never in their optional parameters"
)]
public static class LocalGattServiceBuilderExtensions
{
    /// <summary> Adds a characteristic with the property <typeparamref name="TProp1"/> </summary>
    /// <param name="service"> The service being built </param>
    /// <param name="uuid"> The UUID of the characteristic </param>
    /// <param name="onWrite"> Handles writes of centrals </param>
    /// <param name="onRead"> Answers reads of centrals </param>
    /// <typeparam name="TProp1"> The property marker </typeparam>
    /// <returns> The characteristic </returns>
    public static LocalCharacteristic<TProp1> AddCharacteristic<TProp1>(
        this LocalGattServiceBuilder service,
        BleUuid uuid,
        OnWriteHandler? onWrite = null,
        OnReadHandler? onRead = null
    )
        where TProp1 : IBleProperty =>
        AddCharacteristic(service, new CharacteristicDeclaration<TProp1>(uuid), onWrite, onRead);

    /// <summary> Adds the characteristic a declaration describes </summary>
    /// <param name="service"> The service being built </param>
    /// <param name="declaration"> The declaration </param>
    /// <param name="onWrite"> Handles writes of centrals </param>
    /// <param name="onRead"> Answers reads of centrals </param>
    /// <typeparam name="TProp1"> The property marker </typeparam>
    /// <returns> The characteristic </returns>
    public static LocalCharacteristic<TProp1> AddCharacteristic<TProp1>(
        this LocalGattServiceBuilder service,
        CharacteristicDeclaration<TProp1> declaration,
        OnWriteHandler? onWrite = null,
        OnReadHandler? onRead = null
    )
        where TProp1 : IBleProperty => new(AddUntyped(service, declaration, onRead, onWrite));

    /// <inheritdoc cref="AddCharacteristic{TProp1}(LocalGattServiceBuilder, CharacteristicDeclaration{TProp1}, OnWriteHandler?, OnReadHandler?)" />
    /// <typeparam name="TProp1"> The first property marker </typeparam>
    /// <typeparam name="TProp2"> The second property marker </typeparam>
    public static LocalCharacteristic<TProp1, TProp2> AddCharacteristic<TProp1, TProp2>(
        this LocalGattServiceBuilder service,
        CharacteristicDeclaration<TProp1, TProp2> declaration,
        OnWriteHandler? onWrite = null,
        OnReadHandler? onRead = null
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty => new(AddUntyped(service, declaration, onRead, onWrite));

    /// <summary> Adds the typed characteristic a declaration describes; handlers get and return decoded values </summary>
    /// <param name="service"> The service being built </param>
    /// <param name="declaration"> The declaration; it decodes writes and encodes reads </param>
    /// <param name="onWrite"> Handles writes of centrals. A value the declaration cannot decode is answered with its <see cref="GattException"/>'s status </param>
    /// <param name="onRead"> Answers reads of centrals </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The property marker </typeparam>
    /// <returns> The characteristic </returns>
    public static TypedLocalCharacteristic<T, TProp1> AddCharacteristic<T, TProp1>(
        this LocalGattServiceBuilder service,
        TypedCharacteristicDeclaration<T, TProp1> declaration,
        OnWriteHandler<T>? onWrite = null,
        OnReadHandler<T>? onRead = null
    )
        where TProp1 : IBleProperty => new(AddTyped(service, declaration, onRead, onWrite), declaration);

    /// <inheritdoc cref="AddCharacteristic{T, TProp1}(LocalGattServiceBuilder, TypedCharacteristicDeclaration{T, TProp1}, OnWriteHandler{T}?, OnReadHandler{T}?)" />
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <typeparam name="TProp1"> The first property marker </typeparam>
    /// <typeparam name="TProp2"> The second property marker </typeparam>
    public static TypedLocalCharacteristic<T, TProp1, TProp2> AddCharacteristic<T, TProp1, TProp2>(
        this LocalGattServiceBuilder service,
        TypedCharacteristicDeclaration<T, TProp1, TProp2> declaration,
        OnWriteHandler<T>? onWrite = null,
        OnReadHandler<T>? onRead = null
    )
        where TProp1 : IBleProperty
        where TProp2 : IBleProperty => new(AddTyped(service, declaration, onRead, onWrite), declaration);

    private static LocalGattCharacteristic AddUntyped(
        LocalGattServiceBuilder service,
        ICharacteristicDeclaration declaration,
        OnReadHandler? onRead,
        OnWriteHandler? onWrite
    )
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(declaration);
        return service.AddCharacteristic(declaration.Uuid, declaration.Properties, onRead, onWrite);
    }

    /// <summary> Wraps typed handlers in untyped ones; decoding and encoding run inside the handler run, with its error handling </summary>
    private static LocalGattCharacteristic AddTyped<T>(
        LocalGattServiceBuilder service,
        ICharacteristicDeclaration declaration,
        OnReadHandler<T>? onRead,
        OnWriteHandler<T>? onWrite
    )
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(declaration);
        var codec = (IGattTypedCharacteristic<T>)declaration;
        OnReadHandler? untypedRead = onRead is null
            ? null
            : async (central, ct) => codec.Encode(await onRead(central, ct).ConfigureAwait(false));
        OnWriteHandler? untypedWrite = onWrite is null
            ? null
            : (central, value, ct) => onWrite(central, codec.Decode(value.Span), ct);
        return service.AddCharacteristic(declaration.Uuid, declaration.Properties, untypedRead, untypedWrite);
    }
}
