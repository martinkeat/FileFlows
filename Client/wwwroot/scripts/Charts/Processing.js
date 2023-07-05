import { FFChart } from './FFChart.js';
export class Processing extends FFChart
{
    recentlyFinished;
    timer;
    existing;
    runners = {};
    infoTemplate;
    isPaused;
    eventListener;

    constructor(uid, args) {
        super(uid, args);
        console.log('#### PROCESSING!!!');
        this.recentlyFinished = args.flags === 1;
        this.infoTemplate = Handlebars.compile(this.infoTemplateHtml);
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

    onExecutorsUpdated(event) {
        let data = event?.detail?.data;
        if(!data)
            return;
        this.createChart(data);
    }

    async getData() {
        // if(this.timer)
        //     clearTimeout(this.timer);
        //
        // if(this.disposed)
        //     return;
        // super.getData();
        //
        // this.timer = setTimeout(() => this.getData(), document.hasFocus() ? 5000 : 10000);
    }

    createChart(data) {
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

    setSize(size) {
        let rows = Math.floor((size - 1) / 2) + 1;
        ffGrid.update(this.ele, { h: rows});
    }

    createNoData(data){
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

    createRunners(data) {
        let running = [];
        let chartDiv = document.getElementById(this.chartUid);
        if(!chartDiv)
            return;
        chartDiv.className = 'processing-runners runners-' + data.length;
        for(let worker of data){
            running.push(worker.Uid);
            this.updateRunner(worker);
            try {
                this.createOrUpdateRadialBar(worker);
            }catch(err){}
        }
        let keys = Object.keys(this.runners);
        for(let i=keys.length; i >= 0; i--){
            let key = keys[i];
            if(!key)
                continue;
            if(running.indexOf(key) < 0){
                let eleRemove = document.getElementById('runner-' + key)
                if(eleRemove)
                    eleRemove.remove();
                delete this.runners[key];
            }
        }
    }

    createRunner(runner)
    {
        let chartDiv = document.getElementById(this.chartUid);
        let div = document.createElement('div');
        div.className = 'runner';
        div.id = 'runner-' + runner.Uid;


        let eleChart = document.createElement('div');
        div.appendChild(eleChart);
        eleChart.id = 'runner-' + runner.Uid + '-chart';
        eleChart.className = 'chart chart-' + runner.Uid;

        let eleInfo = document.createElement('div');
        eleInfo.id = 'runner-' + runner.Uid + '-info';
        div.appendChild(eleInfo);
        eleInfo.className = 'info';
        this.runners[runner.Uid] = {
            Uid: runner.Uid
        };
        chartDiv.appendChild(div);

        let buttons = document.createElement('div');
        div.appendChild(buttons);
        buttons.className = 'buttons';

        let btnLog = document.createElement('button');
        btnLog.className = 'btn btn-log';
        btnLog.innerText = 'Info';
        btnLog.addEventListener('click', () => {
            //this.csharp.invokeMethodAsync("OpenLog", runner.LibraryFile.Uid, runner.LibraryFile.Name);
            this.csharp.invokeMethodAsync("OpenFileViewer", runner.LibraryFile.Uid);
        });
        buttons.appendChild(btnLog);

        let btnCancel = document.createElement('button');
        btnCancel.className = 'btn btn-cancel';
        btnCancel.innerText = 'Cancel';
        btnCancel.addEventListener('click', () => {
            this.csharp.invokeMethodAsync("CancelRunner", runner.Uid, runner.LibraryFile.Uid, runner.LibraryFile.Name).then(() =>{
                try {
                    this.getData();
                }catch(err){}
            });
        });
        buttons.appendChild(btnCancel);
        return div;
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

    updateRunner(runner)
    {
        this.csharp.invokeMethodAsync("HumanizeStepName", runner.CurrentPartName).then((step) =>
        {
            let args = {
                file: runner.LibraryFile?.Name,
                node: runner.NodeName,
                library: runner.Library?.Name,
                step: step,
                time: this.timeDiff( Date.parse(runner.StartedAt), Date.now())
            };
            let eleInfo = document.getElementById('runner-' + runner.uid + '-info');
            if(!eleInfo){
                eleInfo = this.createRunner(runner).querySelector('.info');
            }
            eleInfo.innerHTML = this.infoTemplate(args);
        });
    }

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

    createOrUpdateRadialBar(runner){
        let chartUid = `runner-${runner.Uid}-chart`;
        let overall = runner.TotalParts == 0 ? 100 : (runner.CurrentPart / runner.TotalParts) * 100;
        let options = {
            chart: {
                id: chartUid,
                height: this.runners.length > 3 ? '200px' : '190px',
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
                            label: runner.CurrentPartPercent ? (runner.CurrentPartPercent.toFixed(1) + ' %') : 'Overall',
                            fontSize: '0.8rem',
                            formatter: function (val) {
                                return parseFloat(overall).toFixed(1) + ' %';
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
        if (runner.CurrentPartPercent > 0) {
            options.series.push(runner.CurrentPartPercent);
            options.labels.push('Current');
        }

        let updated = false;

        let eleChart = document.getElementById(`runner-${runner.Uid}-chart`);

        if (eleChart.querySelector('.apexcharts-canvas')) {
            try {
                ApexCharts.exec(chartUid, 'updateOptions', options, false, false);
                updated = true;
            } catch (err) { }
        }

        if (updated === false && eleChart) {
            new ApexCharts(eleChart, options).render();
        }
    }
}

