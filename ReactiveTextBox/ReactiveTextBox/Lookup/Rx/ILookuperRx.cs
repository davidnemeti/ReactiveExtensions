using System;

namespace ReactiveTextBox.Lookup.Rx
{
    public interface ILookuperRx
    {
        IObservable<string[]> Lookup(IObservable<string> texts);
        void Cancel();
    }
}
