namespace Hyperbee.XS.Interactive.Tests;

public static class ObservableExtensions
{
    public static SubscribedList<T> ToSubscribedList<T>( this IObservable<T> source )
    {
        return new SubscribedList<T>( source );
    }
}
