import { FFChart } from './FFChart.js?v=#VERSION#';

/**
 * Widget for displaying totals in a table
 */
export class TotalsTable extends FFChart
{
    timer;
    existing;
    hasNoData;
    eventListener;
    // the items in the table
    items = [];

    /**
     * Constructs an instance of the totals table widget
     * @param {string} uid - The UID of the widget instance.
     * @param {object} args - The arguments for the widget instance. 
     */
    constructor(uid, args) {
        super(uid, args);
        this.eventListener = (event) => this.handleEvent(event);
        document.addEventListener(`onClientServiceFinishProcessing`, this.eventListener);
    }

    /**
     * Disposes of the Processing instance.
     */
    dispose() {
        super.dispose();
        document.removeEventListener(`onClientServiceFinishProcessing`, this.eventListener);
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
        return document.hasFocus() ? 10000 : 20000;
    }

    /**
     * Gets the data
     */
    async getData() {
        if(this.disposed)
            return;
        super.getData();
    }

    fixData(data) {
        let fixed = [];
        Object.keys(data).forEach(x => {
            fixed.push({Name: x, Value: data[x]});
        });
        return fixed;
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
        if(data && data.length > 0)
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
        spanText.innerText = 'No data';

        chartDiv.appendChild(div);
    }

    /**
     * Creates the HTML DOM elements for the table
     * @param data - the data
     */
    createTableData(data)
    {
        let table = document.createElement('table');
        
        let tbody = document.createElement('tbody');
        table.appendChild(tbody);
        for(let i=0;i<data.length;i++)
        {
            let tr = document.createElement('tr');
            tbody.appendChild(tr);

            let tdLabel = document.createElement('td');
            tr.appendChild(tdLabel);
            tdLabel.innerText = data[i].Name;

            let tdTotal = document.createElement('td');
            tdTotal.style.width = '6rem';
            tdTotal.style.minWidth = '6rem';
            tdTotal.style.maxWidth = '6rem';
            tdTotal.innerText = data[i].Value;
            tr.appendChild(tdTotal);
        }
        let chartDiv = document.getElementById(this.chartUid);
        chartDiv.textContent = '';
        chartDiv.appendChild(table);
    }
}
