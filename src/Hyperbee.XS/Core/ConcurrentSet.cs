using System.Collections;
using System.Collections.Concurrent;

namespace Hyperbee.XS.Core;

internal interface IConcurrentSet<T> : ICollection<T>
{
    bool IsEmpty { get; }
    bool TryAdd( T item );
    bool TryRemove( T item );
}
internal class ConcurrentSet<T> : IConcurrentSet<T>
{
    private readonly ConcurrentDictionary<T, int> _dictionary;

    public ConcurrentSet()
    {
        _dictionary = new ConcurrentDictionary<T, int>();
    }

    public ConcurrentSet( IEnumerable<T> collection )
    {
        if ( collection == null )
            throw new ArgumentNullException( nameof( collection ) );

        _dictionary = new ConcurrentDictionary<T, int>( ToKeyValuePairs( collection ) );
    }

    public ConcurrentSet( IEqualityComparer<T> comparer )
    {
        if ( comparer == null )
            throw new ArgumentNullException( nameof( comparer ) );

        _dictionary = new ConcurrentDictionary<T, int>( comparer );
    }

    public ConcurrentSet( IEnumerable<T> collection, IEqualityComparer<T> comparer )
    {
        if ( collection == null )
            throw new ArgumentNullException( nameof( collection ) );

        if ( comparer == null )
            throw new ArgumentNullException( nameof( comparer ) );

        _dictionary = new ConcurrentDictionary<T, int>( ToKeyValuePairs( collection ), comparer );
    }

    private static IEnumerable<KeyValuePair<T, int>> ToKeyValuePairs( IEnumerable<T> collection )
        => collection.Distinct().Select( key => new KeyValuePair<T, int>( key, KeyHash( key ) ) );

    private static int KeyHash( T item ) => item?.GetHashCode() ?? 0;

    public void Add( T item ) => _dictionary.AddOrUpdate( item, KeyHash( item ), ( k, _ ) => KeyHash( k ) );

    public void Clear() => _dictionary.Clear();

    public bool Contains( T item ) => item != null && _dictionary.ContainsKey( item );

    public void CopyTo( T[] array, int arrayIndex )
    {
        if ( array == null || arrayIndex >= array.Length )
            throw new ArgumentOutOfRangeException( nameof( arrayIndex ) );

        if ( _dictionary.IsEmpty )
            return;

        foreach ( var key in _dictionary.Keys )
        {
            array[arrayIndex++] = key;

            if ( arrayIndex >= array.Length )
                break;
        }
    }

    public int Count => _dictionary.Count;

    public bool IsEmpty => _dictionary.IsEmpty;
    public bool IsReadOnly => false;

    public bool Remove( T item ) => _dictionary.Remove( item, out var _ );

    public bool TryAdd( T item ) => _dictionary.TryAdd( item, item.GetHashCode() );
    public bool TryRemove( T item ) => _dictionary.TryRemove( item, out _ );

    public IEnumerator<T> GetEnumerator() => _dictionary.Keys.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
