using System.Reactive.Subjects;

namespace ReactiveTextBox
{
    public static class Logging
    {
        public static readonly Subject<string> callsLog = new Subject<string>();
        public static readonly Subject<string> rawTextsLog = new Subject<string>();
        public static readonly Subject<string> textsLog = new Subject<string>();
    }
}
