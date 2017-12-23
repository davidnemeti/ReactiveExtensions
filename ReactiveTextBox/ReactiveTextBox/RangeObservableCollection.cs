using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ReactiveTextBox
{
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        public void SetItems(IEnumerable<T> items)
        {
            AddRange(items, clearBefore: true);
        }

        public void AddRange(IEnumerable<T> items)
        {
            AddRange(items, clearBefore: false);
        }

        private void AddRange(IEnumerable<T> items, bool clearBefore)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _suppressNotification = true;
            try
            {
                if (clearBefore)
                {
                    Clear();
                }

                foreach (T item in items)
                {
                    Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
