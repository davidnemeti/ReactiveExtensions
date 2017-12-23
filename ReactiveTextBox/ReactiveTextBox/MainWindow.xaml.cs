using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ReactiveTextBox.Lookup;

using Lookuper = ReactiveTextBox.Lookup.Rx.LookuperRx10WithCancellationByAmb;  // change this to other lookupers (LookuperRx...) to experiment

namespace ReactiveTextBox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILookuperWpf _lookuper = new Lookuper(new SearchEngine());

        private readonly ObservableCollection<string> _callsLogCollection = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();

            InitializeLookup();

            var callsLogDecorated = Logging.callsLog
                .Timestamp()
                .TimeInterval()
                .Select(item => item.Interval > TimeSpan.FromSeconds(2) ? new[] { "---", ToString(item.Value) }.ToObservable() : Observable.Return(ToString(item.Value)))
                .Concat();

            RawTexts.DumpItems = SubscribeAndCreateForCollection(Logging.rawTextsLog);
            Texts.DumpItems = SubscribeAndCreateForCollection(Logging.textsLog);
            Calls.DumpItems = SubscribeForCollection(callsLogDecorated, _callsLogCollection);
        }

        private ObservableCollection<T> SubscribeAndCreateForCollection<T>(IObservable<T> source) =>
            SubscribeForCollection(source, new ObservableCollection<T>());

        private ObservableCollection<T> SubscribeForCollection<T>(IObservable<T> source, ObservableCollection<T> collection)
        {
            source
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(onNext: item => collection.Insert(0, item));

            return collection;
        }

        private static string ToString<T>(Timestamped<T> timestamped) => $@"{timestamped.Timestamp.TimeOfDay:h\:m\:s}: {timestamped.Value}";

        private void DeleteCallsButton_Click(object sender, RoutedEventArgs e)
        {
            _callsLogCollection.Clear();
        }

        private void InitializeLookup()
        {
            _lookuper.UseTextBox(SearchTextBox);
            _lookuper.UseCancelButton(CancelSearchButton);
            SearchResult.DumpItems = _lookuper.SearchResult;
        }

        private void CancelSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Focus();
        }
    }
}
