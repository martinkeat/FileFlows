import { FFChart } from './FFChart.js?v=#VERSION#';

/**
 * Widget for displaying library files in table
 */
export class ProcessingNodes extends FFChart
{
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

        this.lblName = 'Name';
        this.csharp.invokeMethodAsync("Translate", 'Labels.Name').then(result => {
            this.lblName = result;
        });
        this.lblStatus = 'Status';
        this.csharp.invokeMethodAsync("Translate", 'Labels.Status').then(result => {
            this.lblStatus = result;
        });
            
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
     * Gets the refresh interval
     * @returns {number} the number of milliseconds to wait before reloading
     */
    getTimerInterval() {
        return document.hasFocus() ? 20000 : 40000;
    }

    /**
     * Gets the data
     */
    async getData() {
        if(this.disposed)
            return;
        super.getData();
        this.timer = setTimeout(() => this.getData(), this.getTimerInterval());
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
        this.createTableData(data);
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

        let columns = [this.lblName, this.lblStatus]
        let statusWidth ='10rem';

        for(let title of columns){
            let th = document.createElement('th');
            
            th.innerText = title;
            if(title !== this.lblName) {
                th.style.width = statusWidth;
                th.style.minWidth = statusWidth;
                th.style.maxWidth = statusWidth;
            }
            th.className = title.toLowerCase();
            theadTr.appendChild(th);
        }

        let tbody = document.createElement('tbody');
        table.appendChild(tbody);
        for(let item of data)
        {
            let tr = document.createElement('tr');
            if(item.Status === 5)
                tr.style.color = 'var(--orange)';
            else if(item.Status === 4)
                tr.style.color = 'var(--yellow)';
            else if(item.Status === 3)
                tr.style.opacity = '0.6';
            tbody.appendChild(tr);

            let tdName = document.createElement('td');
            tdName.innerText = item.Name;
            tdName.className = 'no-wrap';
            tr.appendChild(tdName);
            
            let tdStatus = document.createElement('td');
            tdStatus.style.width = statusWidth;
            tdStatus.style.minWidth = statusWidth;
            tdStatus.style.maxWidth = statusWidth;
            this.csharp.invokeMethodAsync("Translate", item.StatusText).then(result => {
                tdStatus.innerText = result;
            });
            tr.appendChild(tdStatus);
        }
        let chartDiv = document.getElementById(this.chartUid);
        if(chartDiv != null) {
            chartDiv.textContent = '';
            chartDiv.appendChild(table);
        }
    }
}
