import { FFChart } from './FFChart.js';

/**
 * Widget for displaying library files in table
 */
export class LibraryFileTable extends FFChart
{
    lblIncrease = 'Increase';
    lblDecrease = 'Decrease';
    recentlyFinished;
    timer;
    existing;
    hasNoData;
    eventListener;
    // the items in the table
    items = [];

    /**
     * Constructs an instance of the library file table widget
     * @param {string} uid - The UID of the Library File Table instance.
     * @param {object} args - The arguments for the Library File Table instance. 
     */
    constructor(uid, args) {
        super(uid, args);
        this.recentlyFinished = args.flags === 1;
        this.eventListenerName = this.recentlyFinished ? 'FinishProcessing' : 'StartProcessing';
        this.eventListener = (event) => this.handleEvent(event);
        document.addEventListener(`onClientService${this.eventListenerName }`, this.eventListener);
    }

    /**
     * Disposes of the Processing instance.
     */
    dispose() {
        super.dispose();
        document.removeEventListener(`onClientService${this.eventListenerName}`, this.eventListener);
    }

    /**
     * Handles the websocket event 
     * @param {object} event - The event data
     */
    handleEvent(event) {
        this.getData();
    }

    /**
     * Formats the shrinkage text
     * @param {number} original - the original size in bytes 
     * @param {number} final - the final size in bytes
     * @returns {string} the formatted shrinkage tooltip
     */
    formatShrinkage(original, final)
    {
        let diff = Math.abs(original - final);
        return this.formatFileSize(diff) + (original < final ? " " + this.lblIncrease : " " + this.lblDecrease) +
            "\n" + this.formatFileSize(final) + " / " + this.formatFileSize(original);
    }

    /**
     * Fetches the data from the server
     */
    async fetchData(){
        if(this.url.endsWith('recently-finished') !== true)
            return await super.fetchData();
        else {
            let data = await this.csharp.invokeMethodAsync("FetchRecentlyFinished");
            for(var d of data){
                d.When = d.when;
                delete d.when;
                d.RelativePath = d.relativePath;
                delete d.relativePath;
                d.Uid = d.uid;
                delete d.uid;
                d.FinalSize = d.finalSize;
                delete d.finalSize;
                d.OriginalSize = d.originalSize;
                delete d.originalSize;
            }
            return data;
        }
    }

    /**
     * Gets the refresh interval
     * @returns {number} the number of milliseconds to wait before reloading
     */
    getTimerInterval() {
        return document.hasFocus() ? 10000 : 20000;
    }

    /**
     * Gets the data
     */
    async getData() {
        if(this.disposed)
            return;
        super.getData();

        //this.timer = setTimeout(() => this.getData(), this.getTimerInterval());
    }

    /**
     * Create the chart for the data 
     * @param data the data of hte chart
     */
    createChart(data) {
        let json = data ? JSON.stringify(data) : '';
        if(json === this.existing)
            return;
        this.existing = json; // so we dont refresh if we don't have to
        if(data?.length)
            this.createTableData(data);
        else
            this.createNoData();
    }

    /**
     * Create the element to show when there is no data
     */
    createNoData(){
        let chartDiv = document.getElementById(this.chartUid);
        if(chartDiv == null)
            return;
        chartDiv.textContent = '';

        let div = document.createElement('div');
        div.className = 'no-data';

        let span = document.createElement('span');
        div.appendChild(span);

        let icon = document.createElement('i');
        span.appendChild(icon);
        icon.className = 'fas fa-times';

        let spanText = document.createElement('span');
        span.appendChild(spanText);
        spanText.innerText = this.recentlyFinished ? 'No files recently finished' : 'No upcoming files';

        chartDiv.appendChild(div);

    }

    /**
     * Creates the HTML DOM elements for the table
     * @param data - the data
     */
    createTableData(data)
    {
        let table = document.createElement('table');
        let thead = document.createElement('thead');
        thead.style.width = 'calc(100% - 10px)';
        table.appendChild(thead);
        let theadTr = document.createElement('tr');
        thead.appendChild(theadTr);

        let columns = this.recentlyFinished ? ['Name', 'When', 'Size'] : ['Name']

        for(let title of columns){
            let th = document.createElement('th');
            th.innerText = title;
            if(title !== 'Name') {
                let width = title !== 'Size' ? '9rem' : '6rem';
                th.style.width = width;
                th.style.minWidth = width;
                th.style.maxWidth = width;
            }
            th.className = title.toLowerCase();
            theadTr.appendChild(th);
        }

        let tbody = document.createElement('tbody');
        table.appendChild(tbody);
        for(let item of data)
        {
            let tr = document.createElement('tr');
            tbody.appendChild(tr);

            let tdRelativePath = document.createElement('td');
            tdRelativePath.innerText = item.RelativePath;
            tdRelativePath.style.wordBreak = 'break-word';
            tr.appendChild(tdRelativePath);

            if(this.recentlyFinished === false)
                continue;
            // finished
            let fs = item.FinalSize;
            let os = item.OriginalSize;
            let width = (fs / os) * 100;
            let bigger = width > 100;
            if (width > 100)
                width = 100;
            let toolTip = this.formatShrinkage(os, fs);

            let tdWhen = document.createElement('td');
            tdWhen.style.width = '9rem';
            tdWhen.style.minWidth = '9rem';
            tdWhen.style.maxWidth = '9rem';
            tr.appendChild(tdWhen);

            let aWhen = document.createElement('a');
            tdWhen.appendChild(aWhen);
            aWhen.innerText = item.When;
            aWhen.addEventListener('click', (event) => {
                event.preventDefault();
                this.csharp.invokeMethodAsync("OpenFileViewer", item.Uid);
            });

            let tdSize = document.createElement('td');
            tdSize.style.width = '6rem';
            tdSize.style.minWidth = '6rem';
            tdSize.style.maxWidth = '6rem';
            tr.appendChild(tdSize);
            if(fs > 0)
            {
                let divSize = document.createElement('div');
                tdSize.appendChild(divSize);
                divSize.className = 'flow-bar ' + (bigger ? 'grew' : '');
                divSize.setAttribute('title', toolTip);

                let divInner = document.createElement('div');
                divSize.appendChild(divInner);
                divInner.className = 'bar-value';
                divInner.style.width = 'calc(' + width + '% - 2px)';
            }
        }
        let chartDiv = document.getElementById(this.chartUid);
        chartDiv.textContent = '';
        chartDiv.appendChild(table);
    }
}
