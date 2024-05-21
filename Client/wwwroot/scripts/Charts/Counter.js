import { FFChart } from './FFChart.js?v=#VERSION#';

/**
 * Widget for displaying library files in table
 */
export class Counter extends FFChart
{
    timer;
    existing;
    hasNoData;
    value;
    span;

    /**
     * Constructs an instance of the counter widget
     * @param {string} uid - The UID of the widget instance
     * @param {object} args - The arguments for the widget
     * @param {function} formatter - Optional formatter for the value
     */
    constructor(uid, args, formatter) {
        super(uid, args);
        this.createDiv();
        this.formatter = formatter;
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

        if(!this.span)
            this.createDiv();
        let response = await ff.doFetch(this.url);
        this.value = await response.text();
        if(this.formatter) 
            this.span.innerText = this.formatter(this.value);
        else if(/^[\d]+$/.test(this.value))
            this.span.innerText = parseInt(this.value, 10).toLocaleString();
        else
            this.span.innerText = this.value;

        this.timer = setTimeout(() => this.getData(), this.getTimerInterval());
    }

    /**
     * Create the chart for the data 
     * @param data the data of hte chart
     */
    createChart(data) {
        console.log('createChart', data);
        let json = data ? JSON.stringify(data) : '';
        if(json === this.existing)
            return;
        this.existing = json; // so we dont refresh if we don't have to
        this.createDiv(data);
    }

    /**
     * Create the element to show when there is no data
     */
    createDiv(){
        let chartDiv = document.getElementById(this.chartUid);
        if(chartDiv == null)
            return;
        chartDiv.textContent = '';

        this.span = document.createElement('span');
        chartDiv.appendChild(this.span);

    }

}
