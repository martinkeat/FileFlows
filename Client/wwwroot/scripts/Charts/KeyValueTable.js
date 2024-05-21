import { FFChart } from "./FFChart.js?v=#VERSION#";

export class KeyValueTable extends FFChart
{
    timer;
    existing;
    hasNoData;

    constructor(uid, args) {
        super(uid, args);
    }

    getTimerInterval() {
        return document.hasFocus() ? 10000 : 20000;
    }

    async getData() {
        if(this.disposed)
            return;
        super.getData();

        this.timer = setTimeout(() => this.getData(), this.getTimerInterval());
    }

    createChart(data) {
        let json = data ? JSON.stringify(data) : '';
        if(json === this.existing)
            return;
        this.existing = json; // so we dont refresh if we don't have to
        if(data?.length)
            this.createTableData(data);
        else
            this.createNoData('No data.');
    }

    createNoData(message){
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
        if(message && typeof(message) === 'string')
            spanText.innerText = message;
        else
            spanText.innerText = 'No data';

        chartDiv.appendChild(div);

    }

    createTableData(data)
    {
        let chartDiv = document.getElementById(this.chartUid);
        chartDiv.textContent = '';
        let tbody;

        const addRow = (label, value) => {
            let tr = document.createElement("tr");
            tbody.appendChild(tr);
            let tdValue = document.createElement("td");
            tdValue.className = 'label';
            //tdValue.style.textAlign = 'right';
            tdValue.innerText = value;
            tr.appendChild(tdValue);
        };
        let table = document.createElement('table');
        table.style.height = 'unset';
        table.style.width = 'calc(100% - 1rem)';
        tbody = document.createElement('tbody');
        table.appendChild(tbody);
        table.style.margin = '0 0.25rem';
        for(let item of this.data){
            addRow(item.label, item.value);
        }

    }
}
