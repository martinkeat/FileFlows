import { FFChart } from './FFChart.js';
export class Processing extends FFChart
{
    recentlyFinished;
    timer;
    existing;
    runners = {};
    isPaused;
    eventListener;

    constructor(uid, args) {        
        super(uid, args);
        console.log('#### PROCESSING!!!');
        this.recentlyFinished = args.flags === 1;
        this.eventListener = (event) => this.onExecutorsUpdated(event);
        document.addEventListener("onClientServiceUpdateExecutors", this.eventListener);
    }
    dispose() {
        super.dispose();
        document.removeEventListener("onClientServiceUpdateExecutors", this.eventListener);
    }

    async fetchData(){
        // this.isPaused = false;
        // let response = await fetch(this.url);
        // if(response.headers.get('x-paused') === '1')
        //     this.isPaused = true;
        // return await response.json();
    }

    /**
     * Called when the websocket receives an update to the executors
     * @param event the event from the websocket
     */
    onExecutorsUpdated(event) {
        let data = event?.detail?.data;
        if(!data)
            return;
        this.createChart(data);
    }

    async getData() {
    }

    /**
     * Creates a chart for the runner
     * @param data the data for teh chart
     */
    createChart(data) {
        // check if the data has changed
        let json = (data ? JSON.stringify(data) : '') + (':' + this.isPaused);
        if(json === this.existing)
            return;
        this.existing = json; // so we dont refresh if we don't have to
        let title = 'FileFlows - Dashboard';
        if(data?.length)
        {
            if(this.hasNoData)
            {
                let chartDiv = document.getElementById(this.chartUid);
                if(chartDiv)
                    chartDiv.textContent = '';
                this.hasNoData = false;
            }
            this.createRunners(data);
            let first = data[0];
            if(first.CurrentPartPercent > 0)
                title = 'FileFlows - ' + first.CurrentPartPercent.toFixed(1) + ' %';
            else
                title = 'FileFlows - ' + first.CurrentPartName;
        }
        else
            this.createNoData();

        document.title = title;

        this.setSize(data?.length);
    }

    /**
     * Sets the size of the widget
     * @param size the size as in how many rows
     */
    setSize(size) {
        let rows = Math.floor((size - 1) / 2) + 1;
        ffGrid.update(this.ele, { h: rows});
    }

    /**
     * Create teh no data element when no runners are running
     */
    createNoData(){
        this.hasNoData = true;
        let chartDiv = document.getElementById(this.chartUid);
        chartDiv.textContent = '';

        let div = document.createElement('div');
        div.className = 'no-data';

        let span = document.createElement('span');
        div.appendChild(span);

        let icon = document.createElement('i');
        span.appendChild(icon);

        let spanText = document.createElement('span');
        span.appendChild(spanText);
        if(this.isPaused){
            icon.className = 'fas fa-pause';
            spanText.innerText = 'Processing is currently paused';
        }else {
            icon.className = 'fas fa-times';
            spanText.innerText = 'No files currently processing';
        }

        chartDiv.appendChild(div);
    }

    /**
     * Create the runners for the data
     * @param data the data from the websocket
     */
    createRunners(data) {
        let running = [];
        let chartDiv = document.getElementById(this.chartUid);
        if(!chartDiv)
            return;
        chartDiv.className = 'processing-runners runners-' + data.length;
        for(let worker of data)
        {
            running.push(worker.uid);
            if(!this.runners[worker.uid]) {
                // new, create it
                this.runners[worker.uid] = new Runner(chartDiv, this.csharp, worker);
            }
            this.runners[worker.uid].update({data: worker, totalRunners: data.length});
        }
        let keys = Object.keys(this.runners);
        for(let i=keys.length; i >= 0; i--){
            let key = keys[i];
            if(!key)
                continue;
            if(running.indexOf(key) < 0){
                this.runners[key].dispose();
                delete this.runners[key];
            }
        }
    }
}

/**
 * A runner in the processing tab
 */
class Runner {
    /**
     * Constructs a new runnner 
     * @param parent the parent element to attach this new runner to
     * @param csharp the csharp instance to call csharp methods
     * @param runner the runner being executed
     */
    constructor(parent, csharp, runner) 
    {
        console.log('creating runner: ' + runner.uid);
        this.uid = runner.uid;
        this.eleChartId = 'runner-' + this.uid + '-chart';
        this.libraryFile = runner.libraryFile;
        this.library = runner.library;
        this.csharp = csharp;
        this.infoTemplate = Handlebars.compile(this.infoTemplateHtml);
        this.createElement(parent);
    }

    infoTemplateHtml = `
<div class="lv w-2 file">
    <span class="l">File</span>
    <span class="v">{{file}}</span>
</div>
<div class="lv node">
    <span class="l">Node</span>
    <span class="v">{{node}}</span>
</div>
<div class="lv library">
    <span class="l">Library</span>
    <span class="v">{{library}}</span>
</div>
<div class="lv step">
    <span class="l">Step</span>
    <span class="v">{{step}}</span>
</div>
<div class="lv time">
    <span class="l">Time</span>
    <span class="v">{{time}}</span>
</div>
`;
    
    /**
     * Log was clicked
     */
    logClicked() {
        this.csharp.invokeMethodAsync("OpenFileViewer", this.libraryFile.uid);       
    }

    /**
     * Cancel was clicked
     */
    async cancelClicked() {
        this.csharp.invokeMethodAsync("CancelRunner", this.uid, this.libraryFile.uid, this.libraryFile.name);
    }

    /**
     * Updates the runner
     * @param data the runner data
     * @param totalRunners the total runners
     */
    update({data, totalRunners}) {
        this.updateInfo(data);
        this.createOrUpdateRadialBar({totalParts: data.totalParts, currentPart: data.currentPart, 
            currentPartPercent: data.currentPartPercent, totalRunners: totalRunners});        
    }

    /**
     * Updates the runner info 
     * @param runner the runnner
     */
    async updateInfo(runner)
    {
        let step = await this.csharp.invokeMethodAsync("HumanizeStepName", runner.currentPartName);
        let args = {
            file: runner.libraryFile?.name,
            node: runner.nodeName,
            library: runner.library?.name,
            step: step,
            time: this.timeDiff( Date.parse(runner.startedAt), Date.now())
        };
        console.log('runner', runner);
        console.log('args', args);
        this.eleInfo.innerHTML = this.infoTemplate(args);
    }    
    
    /**
     * Creates the element for this runner
     * @param parent the parent to attach this to
     */
    createElement(parent) {
        this.element  = document.createElement('div');
        this.element .className = 'runner';
        this.element .id = 'runner-' + this.uid;
        parent.appendChild(this.element);

        this.eleChart = document.createElement('div');
        this.element .appendChild(this.eleChart);
        this.eleChart.id = this.eleChartId;
        this.eleChart.className = 'chart chart-' + this.uid;

        this.eleInfo = document.createElement('div');
        this.eleInfo.id = 'runner-' + this.uid + '-info';
        this.element .appendChild(this.eleInfo);
        this.eleInfo.className = 'info';

        let buttons = document.createElement('div');
        this.element .appendChild(buttons);
        buttons.className = 'buttons';

        let btnLog = document.createElement('button');
        btnLog.className = 'btn btn-log';
        btnLog.innerText = 'Info';
        btnLog.addEventListener('click', () => {
            this.logClicked();
        });
        buttons.appendChild(btnLog);

        let btnCancel = document.createElement('button');
        btnCancel.className = 'btn btn-cancel';
        btnCancel.innerText = 'Cancel';
        btnCancel.addEventListener('click', () => {
            this.cancelClicked();
        });
        buttons.appendChild(btnCancel);
    }

    /**
     * Creates or updates the radial bar chart for this runner 
     * @param totalParts the total parts in the runner
     * @param currentPart the current part in the runner 
     * @param currentPartPercent the current part percent
     * @param totalRunners the total runners currently running
     */
    createOrUpdateRadialBar({
        totalParts, 
        currentPart,
        currentPartPercent,
        totalRunners
    })
    {
        let overall = totalParts === 0 ? 100 : (currentPart / totalParts) * 100;
        let options = {
            chart: {
                id: this.eleChartId,
                height: totalRunners > 3 ? '200px' : '190px',
                type: "radialBar",
                foreColor: 'var(--color)',
            },
            plotOptions: {
                radialBar: {
                    hollow: {
                        margin: 5,
                        size: '48%',
                        background: 'transparent',
                    },
                    track: {
                        background: '#333',
                    },
                    startAngle: -135,
                    endAngle: 135,
                    stroke: {
                        lineCap: 'round'
                    },
                    dataLabels: {
                        total: {
                            show: true,
                            label: currentPartPercent ? (currentPartPercent.toFixed(1) + ' %') : 'Overall',
                            fontSize: '0.8rem',
                            formatter: function (val) {
                                return parseFloat(''+overall).toFixed(1) + ' %';
                            }
                        },
                        value: {
                            show: true,
                            fontSize: '0.7rem',
                            formatter: function (val) {
                                return +(parseFloat(val).toFixed(1)) + ' %';
                            }
                        }

                    }
                }
            },
            colors: [
                '#2b8fb3',
                '#c30471',
            ],
            series: [overall],
            labels: ['Overall']
        };
        if (currentPartPercent > 0) {
            options.series.push(currentPartPercent);
            options.labels.push('Current');
        }

        let updated = false;

        if (this.eleChart.querySelector('.apexcharts-canvas')) {
            try {
                ApexCharts.exec(this.eleChartId, 'updateOptions', options, false, false);
                updated = true;
            } catch (err) { }
        }

        if (updated === false && this.eleChart) {
            new ApexCharts(this.eleChart, options).render();
        }
    }

    /**
     * Gets the time difference  between two dates
     * @param start the start date
     * @param end the end date
     * @returns {string} the time difference as a string
     */
    timeDiff(start, end)
    {
        let diff = (end - start) / 1000;
        let hours = Math.floor(diff / 3600);
        diff -= (hours * 3600);
        let minutes = Math.floor(diff / 60);
        diff -= (minutes * 60);
        let seconds = Math.floor(diff);

        return hours.toString().padStart(2, '0') + ':' + minutes.toString().padStart(2, '0') + ':' + seconds.toString().padStart(2, '0')
    }

    /**
     * disposes of this runner
     */
    dispose() {
        this.element.remove();
    }
}

