using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Att;

/// <summary> The implementation of an attribute collection </summary>
/// <typeparam name="T"> The type of the attribute </typeparam>
public sealed class AttributeCollection<T>(Func<T, BleUuid> uuidFunc) : IReadonlyAttributeCollection<T>
{
    private readonly Func<T, BleUuid> _uuidFunc = uuidFunc;
    private readonly List<T> _attributes = [];

    /// <inheritdoc cref="List{T}.GetEnumerator" />
    public List<T>.Enumerator GetEnumerator() => _attributes.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
    public int Count => _attributes.Count;

    /// <inheritdoc cref="ICollection{T}.Add" />
    public void Add(T item) => _attributes.Add(item);

    /// <inheritdoc />
    public bool ContainsAny(BleUuid key)
    {
        T? attribute = _attributes.Find(x => _uuidFunc(x) == key);
        return attribute is not null;
    }

    /// <inheritdoc />
    public bool TryGet(BleUuid key, [MaybeNullWhen(false)] out T value)
    {
        value = _attributes.Find(x => _uuidFunc(x) == key);
        return value is not null;
    }

    /// <inheritdoc />
    public T this[BleUuid key]
    {
        get
        {
            if (!TryGet(key, out T? value))
                throw new KeyNotFoundException();
            return value;
        }
    }

    /// <inheritdoc />
    public IEnumerable<BleUuid> Uuids => _attributes.Select(x => _uuidFunc(x));
}
