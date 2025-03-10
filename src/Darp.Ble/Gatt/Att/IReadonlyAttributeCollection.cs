using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Att;

/// <summary> Represents a collection of attributes </summary>
/// <typeparam name="T"> The type of the attribute </typeparam>
public interface IReadonlyAttributeCollection<T> : IReadOnlyCollection<T>
{
    /// <summary>Determines whether the read-only dictionary contains an element that has the specified key.</summary>
    /// <param name="key">The key to locate.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="key" /> is <see langword="null" />.</exception>
    /// <returns> <see langword="true" /> if the read-only dictionary contains an element that has the specified key; otherwise, <see langword="false" />.</returns>
    bool ContainsAny(BleUuid key);

    /// <summary>Gets the value that is associated with the specified key.</summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="key" /> is <see langword="null" />.</exception>
    /// <returns> <see langword="true" /> if the object that implements the <see cref="IReadonlyAttributeCollection{T}" /> interface contains an element that has the specified key; otherwise, <see langword="false" />.</returns>
    bool TryGet(BleUuid key, [MaybeNullWhen(false)] out T value);

    /// <summary>Gets the element that has the specified key in the read-only dictionary.</summary>
    /// <param name="key">The key to locate.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="key" /> is not found.</exception>
    /// <returns>The element that has the specified key in the read-only dictionary.</returns>
    T this[BleUuid key] { get; }

    /// <summary>Gets an enumerable collection that contains the keys in the read-only dictionary.</summary>
    /// <returns>An enumerable collection that contains the keys in the read-only dictionary.</returns>
    IEnumerable<BleUuid> Uuids { get; }
}
