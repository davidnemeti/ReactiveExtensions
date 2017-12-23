// NOTE that this Javascript example uses an obsolete version of RxJS (through NuGet).
// For an up-to-date version of RxJS (which supports TypeScript as well) you should use npm to get it from here: https://www.npmjs.com/package/@reactivex/rxjs
// See https://github.com/ReactiveX/rxjs for more info.

(function () {
    var searchResults = Rx.Observable.fromEvent($('#inputText'), 'keyup')
        .map(e => e.target.value) // project the text from the input
        .debounce(500) // [1]
        .distinctUntilChanged() // [2]
        .do(item => $('#cancelButton').prop('disabled', false))
        .map(text =>
            Rx.Observable.fromPromise(searchWikipedia(text)) // [3]
                .timeout(3000) // [4]
                .retry(3) // [5]
                .catch(function(ex) {
                    if (ex instanceof Rx.TimeoutError) return Rx.Observable.return(["<< TIMEOUT >>"]); // [6]
                    else return Rx.Observable.return(["<< ERROR >>"]); // [7]
                })
                .amb(Rx.Observable.fromEvent($('#cancelButton'), 'click').map(unit => ["<< CANCEL >>"])) // [9]
        )
        .switch() // [8]
        .do(item => $('#cancelButton').prop('disabled', true));

    searchResults.subscribe(searchResult =>
        $('#searchResult')
            .empty()
            .append($.map(searchResult, searchResultItem => $('<li>').text(searchResultItem)))
    );

    async function searchWikipedia(term) {
        var data = await $.ajax({
            url: 'http://en.wikipedia.org/w/api.php',
            dataType: 'jsonp',
            data: {
                action: 'opensearch',
                format: 'json',
                search: term
            }
        });
        return data[1];
    }
}());
