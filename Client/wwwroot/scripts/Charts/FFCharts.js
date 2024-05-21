import { FFChart } from './FFChart.js?v=#VERSION#';
import { Processing } from './Processing.js?v=#VERSION#';
import { LibraryFileTable } from "./LibraryFileTable.js?v=#VERSION#";
import { ProcessingNodes } from './ProcessingNodes.js?v=#VERSION#';
import { TotalsTable } from './TotalsTable.js?v=#VERSION#';
import { Counter } from './Counter.js?v=#VERSION#';

export function initDashboard(uid, Widgets, csharp, isReadOnly){
    if(!Widgets)
        return;
    
    let defaultDashboard = uid === 'bed286d9-68f0-48a8-8c6d-05ec6f81d67c';
    if(defaultDashboard && window.innerWidth <= 850) {
        // rearrange widgets so Runners is first
        Widgets = [...Widgets.filter(x => x.type === 1), 
            ...Widgets.filter(x => x.type !== 105 && x.type !== 1 && x.type !== 3),  
            ...Widgets.filter(x => x.type === 105 || x.type === 3)]        
    }
    disposeAll();
    destroyDashboard();
    if(!document)
        return;
    let dashboard = document.querySelector('.dashboard.grid-stack');
    if(!dashboard)
    {
        dashboard = document.createElement('div');
        dashboard.className = 'dashboard grid-stack';
        let container = document.querySelector('.dashboard-wrapper');
        if(container)
            container.appendChild(dashboard);
    }
    else {
        dashboard.classList.remove('readonly');
        dashboard.textContent = '';
    }
    if (isReadOnly)
        dashboard.classList.add('readonly');

    for(let p of Widgets)
    {
        addWidget(dashboard, p, csharp);
    }
    initDashboardActual(uid, csharp, isReadOnly);
}

export function destroyDashboard()
{
    if(!window.ffGrid)
        return;
    
    try {
        window.ffGrid.destroy();
        delete window.ffGrid;
    }catch(err){
    }
}

export function addWidgets(uid, Widgets, csharp){
    if(!Widgets)
        return;
    let dashboard = document.querySelector('.dashboard.grid-stack');
    let grid = window.ffGrid;
    grid.batchUpdate();
    for(let p of Widgets)
    {
        let div = addWidget(dashboard, p, csharp);
        grid.addWidget(div, { autoPosition: true});
        grid.update(div, { autoPosition: false});
    }
    grid.commit();
}


export function getGridData()
{
    let data = [];
    for(let ele of document.querySelectorAll('.grid-stack-item')){
        let uid = ele.id;
        let x = parseInt(ele.getAttribute('gs-x'), 10);
        let y = parseInt(ele.getAttribute('gs-y'), 10);
        let w = parseInt(ele.getAttribute('gs-w'), 10);
        let h = parseInt(ele.getAttribute('gs-h'), 10);
        data.push({
            Uid:uid, X: x, Y:y, Width:w, Height:h
        });
    }
    return data;
}


export function dispose(uid) {
    let chart = window.FlowCharts[uid];
    if(chart)
        chart.dispose();
}

export function disposeAll(){
    if(!window.FlowCharts)
        return;
    Object.keys(window.FlowCharts).forEach(uid => {
        try {
            window.FlowCharts[uid].dispose();
        }catch(err){
            console.log('err', err);
        }
    });
}

function initDashboardActual(uid, csharp, isReadOnly) {

    if (window.innerWidth < 850)
        return;
    let grid = GridStack.init({
        cellHeight:186,
        handle: '.draghandle',
        disableResize: isReadOnly,
        disableDrag: isReadOnly
    });
    if(!grid)
        return;
    window.ffGrid = grid;

    grid.on('resizestop', (e, el) => {
        window.dashboardElementResized.args = e;
        el.dispatchEvent(window.dashboardElementResized);
        let data = getGridData();
        csharp.invokeMethodAsync("SaveDashboard", uid, data);
    });
    grid.on('dragstop', () => {
        let data = getGridData();
        csharp.invokeMethodAsync("SaveDashboard", uid, data);        
    });
    grid.on('removed', () => {
        setTimeout(() => {                
            let data = getGridData();
            csharp.invokeMethodAsync("SaveDashboard", uid, data);
        }, 500);
    });
}


function addWidget(dashboard, p, csharp){

    let div = document.createElement("div");
    div.setAttribute('id', p.uid);
    div.className = 'grid-stack-item widget chart-type-' + p.type;
    div.setAttribute('gs-w', p.width);
    div.setAttribute('gs-h', p.height);
    div.setAttribute('gs-x', p.x);
    div.setAttribute('gs-y', p.y);
        
    if(p.type === 1)
        div.setAttribute('gs-no-resize', 1);
    
    let inner = document.createElement('div');
    inner.className = 'inner';
    div.appendChild(inner);

    let title = document.createElement('div');
    inner.appendChild(title);
    title.className = 'title draghandle';
    let icon = document.createElement('i');
    title.appendChild(icon);
    icon.className = p.icon;
    let spanTitle = document.createElement('span');
    title.appendChild(spanTitle);
    csharp.invokeMethodAsync("Translate", 'Widgets.' + p.name.replace(/\s/g, '')).then(result => {
        spanTitle.innerText = result;        
    });

    let eleRemove = document.createElement('i');
    title.appendChild(eleRemove);
    eleRemove.className = 'fas fa-trash';
    eleRemove.setAttribute('title', 'Remove');
    eleRemove.addEventListener('click', (event) => {
        event.preventDefault();
        csharp.invokeMethodAsync("RemoveWidget", p.uid).then((success) => {
            if(success)
                window.ffGrid.removeWidget(div);
        });
    });

    let content = document.createElement('div');
    content.className = 'content wt' + p.type;
    inner.appendChild(content);
    if(p.type === 105){
        let top = document.createElement('div');
        top.setAttribute('id', p.uid + '-top');
        top.className = 'top';
        content.appendChild(top);

        let bottom = document.createElement('div');
        bottom.className = 'bottom';
        bottom.setAttribute('id', p.uid + '-bottom');
        content.appendChild(bottom);
    }
    else
    {
        let chart = document.createElement('div');
        chart.setAttribute('id', p.uid + '-chart');
        content.appendChild(chart);
    }
    dashboard.appendChild(div);
    newChart(p.type, p.uid, { url: p.url, flags: p.flags, csharp: csharp});
    return div;
}

function newChart(type, uid, args){
    if(!window.FlowCharts)
        window.FlowCharts = {};
    args.type = type;
    if(type == 'Processing' || type === 1)
        window.FlowCharts[uid] = new Processing(uid, args);
    else if(type == 'LibraryFileTable' || type === 2)
        window.FlowCharts[uid] = new LibraryFileTable(uid, args);
    else if(type == 'ProcessingNodes' || type === 3)
        window.FlowCharts[uid] = new ProcessingNodes(uid, args);
    else if(type == 'BoxPlot' || type === 101)
        window.FlowCharts[uid] = new BoxPlotChart(uid, args);
    else if(type == 'HeatMap' || type === 102)
        window.FlowCharts[uid] = new HeatMapChart(uid, args);
    else if(type == 'PieChart' || type === 103)
        window.FlowCharts[uid] = new PieChartChart(uid, args);
    else if(type == 'TreeMap' || type === 104)
        window.FlowCharts[uid] = new TreeMapChart(uid, args);
    else if(type == 'TimeSeries' || type === 105)
        window.FlowCharts[uid] = new TimeSeriesChart(uid, args);
    else if(type == 'Bar' || type === 106)
        window.FlowCharts[uid] = new BarChart(uid, args);
    else if(type == 'BellCurve' || type === 107)
        window.FlowCharts[uid] = new BellCurve(uid, args);
    else if(type == 'Counter' || type === 108)
        window.FlowCharts[uid] = new Counter(uid, args);
    else if(type == 'TotalsTable' || type === 110)
        window.FlowCharts[uid] = new TotalsTable(uid, args);
    else if(type == 'Nvidia' || type === 121)
        window.FlowCharts[uid] = new NvidiaChart(uid, args);
    else 
        console.log('unknown type: ' + type);
    
}



export class BoxPlotChart extends FFChart
{           
    constructor(uid, args) {
        super(uid, args);
    }

    getChartOptions(data){
        return {
            chart: {
                type: 'boxPlot',
            },
            plotOptions: {
                boxPlot: {
                    colors: {
                        upper: '#ff0090',
                        lower: '#84004bd9'
                    }
                }
            },
            series: [{
                data:data
            }]
        };
    }
}



export class HeatMapChart extends FFChart
{
    constructor(uid, args) {
        super(uid, args);
        this.chartBottomPad = 0;
    }

    getChartOptions(data){
        return {
            series: data,
            chart: {
                type: 'heatmap',
            },
            theme: {
                palette: 'palette6'
            },
            dataLabels: {
                enabled: false
            },
            colors: ["#ff0090"],
            plotOptions: {
                heatmap: {
                    shadeIntensity: 0.7,
                    radius: 0,
                    useFillColorAsStroke: true
                }
            },
        };
    }
}

export class BarChart extends FFChart
{
    constructor(uid, args) {
        super(uid, args);
    }

    hasData(data) {
        return !!data?.labels?.length;
    }

    getChartOptions(data)
    {
        return {
            chart: {
                type: 'bar',
                stacked: true,
                stackType: '100%'
            },
            legend: {
                show: false
            },
            tooltip: {
                y: {
                    formatter: (val, opt) => {
                        let r = this.formatFileSize(val);
                        if(opt.seriesIndex === 0 && data.items?.length > opt.dataPointIndex) {
                            r += ` (${data.items[opt.dataPointIndex]} items)`;
                        }
                        return r;
                    }
                }
            },
            plotOptions: {
                bar: {
                    borderRadius: 4,
                    horizontal: true,
                }
            },
            dataLabels: {
                enabled: true,
                formatter: (val, opt) => {
                    let d = data.series[opt.seriesIndex].data[opt.dataPointIndex];
                    return this.formatFileSize(d, 0);                    
                },
            },
            colors: [
                '#02647e',
                'rgba(0,191,232,0.85)',
                'rgba(0,42,52,0.85)'
            ],
            series: data.series,
            xaxis: {
                categories: data.labels,
                axisTicks : {
                    show: false
                },
                labels : {
                    show: false
                },
                axisBorder: {
                    show: false
                },
            },
            yaxis: {
                axisBorder: {
                    show: false
                }
            }
        };
    }
}



export class BellCurve extends FFChart
{
    constructor(uid, args) {
        super(uid, args);
    }

    hasData(data) {
        return !!data?.labels?.length;
    }

    calcMean(data, useY) {
        const sum = data.reduce((a, b) => a + (useY ? b.y : b), 0);
        return sum / data.length;
    }


    hasData(data) {
        return !!data;
    }
    
    fixData(data) {
        let fixed = [];
        Object.keys(data).forEach(x => {
            fixed.push({ x: x, y: data[x]});
        })
        data = fixed;
        
        const mean = this.calcMean(data, true);
        const tmp = data.map(p => Math.pow(p.y - mean, 2));
        const variance = this.calcMean(data.map(p => Math.pow(p.y - mean, 2)));
        const stddev = Math.sqrt(variance);
        const pdf = (x) => {
            const m = stddev * Math.sqrt(2 * Math.PI);
            const e = Math.exp(-Math.pow(x - mean, 2) / (2 * variance));
            return e / m;
        };
        const bell = [];
        const startX = mean - 3.5 * stddev;
        const endX = mean + 3.5 * stddev;
        const step = stddev / 7;
        let x;
        for(x = startX; x <= mean; x += step) {
            bell.push({x, y: pdf(x)});
        }
        for(x = mean + step; x <= endX; x += step) {
            bell.push({x, y: pdf(x)});
        }
        
        return bell;
    }    

    getChartOptions(data)
    {
        var options = {
            chart: {
                type: 'area',
                background: 'transparent',
                sparkline: {
                    enabled: true
                }
            },
            dataLabels: {
                enabled: false
            },
            series: [
                {
                    name: 'Series 1',
                    data: data
                }
            ],
            theme: {
                mode: 'dark',
                palette: 'palette3'
            },
            tooltip: {
                y: {
                    formatter: (value, x) => {
                        return;
                    }
                },
                x: {
                    formatter: (value, x) => {
                        return value.toFixed(0);
                    }
                }
            },
            grid: {
                padding: {
                    top: 0,
                    right:0,
                    bottom: 0,
                    left:0,
                },
                show:false
            },
            stroke: {
                curve: 'straight',
                width: 3,
                colors: ['#33b2df']
            },
            fill: {
                type: "gradient",
                gradient: {
                    OpacityFrom: 0.55,
                    opacityTo: 0
                }
            },
            markers: {
                colors: ["#00BAEC"],
                strokeColors: "#00BAEC",
                strokeWidth: 3
            },
            yaxis: {
                show: false                
            },
            xaxis: {
                show: false,
                axisTicks : {
                    show: false
                },
                labels : {
                    show: false
                }
            }
        };
        return options;
    }
}

export class PieChartChart extends FFChart
{
    constructor(uid, args) {
        super(uid, args);
    }

    hasData(data) {
        return !!data?.series?.length;
    }

    fixData(data) {
        if(!data || !Object.keys(data).length)
            return [];

        let results = [];
        Object.keys(data).forEach(x => {
            results.push({
                Name: x,
                Value: data[x]
            })
        });

        results.sort((a, b) => {
            return b.Value - a.Value;
        });
        
        let series = {
            labels: [],
            series: []
        };
        for(let v of results)
        {
            series.labels.push(v.Name);
            series.series.push(v.Value);
        }
        return series;
    }


    getChartOptions(data)
    {
        return {
            chart: {
                type: 'donut',
            },
            theme: {
                monochrome: {
                    enabled: true,
                    color:'#02647e'
                }
            },
            stroke:{
                colors:['#33b2df']
            },
            colors: [
                // #33b2df , common blue
                '#33b2df',
                'rgba(51,223,85,0.65)',
                '#84004bd9',

                'var(--blue)',
                'var(--indigo)',
                'var(--cyan)',
                'var(--orange)',
                'var(--green)',
                'var(--teal)',
                'var(--teal)',
                'var(--yellow)',
                'var(--error)',
            ],
            series: data.series,
            labels: data.labels
        };
    }
}

export class TreeMapChart extends FFChart 
{
    constructor(uid, args) {
        super(uid, args);
    }

    fixData(data) {
        if(!data || !Object.keys(data).length)
            return [];
        
        let results = [];
        Object.keys(data).forEach(x => {
            let name = x;
            if (name=== 'mpeg2video')
                name = 'mpeg2'; // too long
            results.push({
                x: name,
                y: data[x]
            })
        });
        
        return results;
    }

    getChartOptions(data)
    {
        return {
            chart: {
                type: 'treemap',
            },
            colors: ['#33b2df'],
            stroke:{
                colors:['#33b2df']
            },
            grid: {
                borderColor: '#90A4AE33'
            },
            series: [{
                data:data
            }]
        };
    }
}



export class TimeSeriesChart extends FFChart
{
    bottomUid;
    topUid;
    chartBottom;
    sizeData;
    countData;
    data;
    buckets;
    url;
    lastFetch;
    timer;
    maxValue;

    selectedRange = {
        start: null,
        end: null
    };

    constructor(uid, args) {
        super(uid, args, true);
        
        let options = args.flags !== null ? args.flags : this.ele.getAttribute('x-options');
        if(!options)
            options = '0';

        this.bottomUid = uid + '-bottom';
        this.topUid = uid + '-top';
        this.sizeData = options.toString() === '1';
        this.countData  = options.toString() === '2';
        this.url = args.url;
        
        this.getData();
    }
    
    getTopHeight(){
        let height = this.getHeight();
        return height - this.getBottomHeight();
    }
    
    getBottomHeight(){
        let height = this.getHeight();
        return height > 200 ? 30 : 18;
    }

    async getData() {
        if(this.disposed)
            return;

        let data;
        if(this.lastFetch) {
            let time = new Date(this.lastFetch.getTime() + 1000);
            let fullDate = time.getFullYear() + '-' + ((time.getMonth() + 1).toString()).padStart(2, '0') + '-' + (time.getDate().toString()).padStart(2, '0')
            let fullTime = time.getHours().toString().padStart(2, '0') + ':' + time.getMinutes().toString().padStart(2, '0') + ':' + time.getSeconds().toString().padStart(2, '0')
                + '.' + time.getMilliseconds().toString().padEnd(3, '0');
            let response = await ff.doFetch(`${this.url}?since=${fullDate}T${fullTime}Z`);
            data = await response.json();
        }else {
            let response = await ff.doFetch(this.url);
            data = await response.json();
        }

        let max = 0;
        for(let d of data){
            if(typeof(d.x) === 'string')
                d.x = new Date(Date.parse(d.x));
            if(d.y === 0)
                d.y = 0.001; // just show it appears
            if(d.y > max)
                max = d.y;
        }
        this.maxValue = max;

        if(this.lastFetch)
            this.data = this.data.concat(data);
        else {
            this.data = data;
        }
        
        
        if(this.data.length > 0) 
        {
            this.lastFetch = this.data[this.data.length - 1].x;

            let buckets = this.adjustData(this.data, 100);
            let showBottom = buckets.length !== this.data.length;
            if (showBottom) {
                if (this.chartBottom)
                    this.updateBottom(buckets);
                else
                    this.buckets = buckets;
            } else {
                this.selectedRange.start = data[0].x;
                this.selectedRange.end = data[data.length - 1].x;
            }

            if (!this.chartTop)
                this.createTop();
            if (!this.chartBottom && showBottom)
                this.createBottom();

        }

        if(this.timer)
            clearTimeout(this.timer);
        if(!this.disposed)
            this.timer = setTimeout(() => this.getData(), 10000);
    }

    adjustData(data, desiredItems){
        let min = data[0].x;
        let max = data[data.length - 1].x;

        let timeDiff = (max - min) / 60000;
        let minutes = 0;
        if(timeDiff < 5)
            minutes = 0;
        else if(timeDiff < desiredItems)
            minutes = 1;
        else
            minutes = Math.floor(timeDiff / desiredItems);

        if(minutes === 0)
            return data;

        const ms = 1000 * 60 * minutes;

        // update the summary graph
        let buckets = [];
        let bucketDict = {};
        for(let d of data) {
            let dt = new Date(Date.parse(d.x));
            let thirtyMins = new Date(Math.floor(dt.getTime() / ms) * ms);
            if(bucketDict[thirtyMins] == null) {
                bucketDict[thirtyMins] = {x: thirtyMins, y: d.y, t: d.y, c: 1};
                buckets.push(bucketDict[thirtyMins]);
            }
            else {
                let b = bucketDict[thirtyMins];
                b.t += d.y;
                ++b.c;
                b.y = b.t / b.c;
            }
        }
        return buckets;
    }

    updateBottom(buckets)
    {
        let oldEnd = this.buckets[this.buckets.length - 1].x;
        let newEnd = buckets[buckets.length - 1].x;

        let diff = newEnd.getTime() - oldEnd.getTime();
        this.buckets = buckets;

        this.chartBottom.updateSeries([{
            name: this.seriesName,
            data: this.buckets
        }]);

        this.selectedRange.start = new Date(this.selectedRange.start.getTime() + diff);
        this.selectedRange.end  = new Date(this.selectedRange.end.getTime() + diff);

        this.chartBottom.updateOptions(
            {
                chart: {
                    selection: {

                        xaxis: {
                            min: this.selectedRange.start.getTime(),
                            max: this.selectedRange.end.getTime()
                        }
                    }
                }
            }
        );
    }

    createTop(){
        let data = this.adjustData(this.data, 500);
        var options = {
            chart: {
                id: this.topUid,
                height: this.getTopHeight(),
                type: "area",
                background: 'transparent',
                toolbar: {
                    autoSelected: 'pan',
                    show:false
                },
                sparkline: {
                    enabled: true
                },
                animations: {
                    enabled: false
                },
            },
            theme: {
                mode: 'dark',
                palette: 'palette3'
            },
            dataLabels: {
                enabled: false
            },
            series: [
                {
                    name: this.seriesName,
                    data: data
                }
            ],
            grid: {
                padding: {
                    top: 0,
                    right:0,
                    bottom: 0,
                    left:0,
                },
                show:false
            },
            stroke: {
                curve: 'smooth',
                width: 1
            },
            fill: {
                type: "gradient",
                gradient: {
                    OpacityFrom: 0.55,
                    opacityTo: 0
                }
            },
            markers: {
                colors: ["#00BAEC"],
                strokeColors: "#00BAEC",
                strokeWidth: 3
            },
            xaxis: {
                type:'datetime',
                axisTicks : {
                    show: false
                },
                labels : {
                    show: false
                }
            },
            yaxis: {
                show: false,
                min:0,
                max: this.maxValue === 0.001 ? 1 : this.maxValue
            },
            tooltip: {
                x: {
                    show:true,
                    formatter: (value, opts) =>
                    {
                        const utcDate = new Date(value);
                        const localDate = new Date(utcDate.getTime() - utcDate.getTimezoneOffset() * 60000);
                        return localDate.toLocaleTimeString();
                    }
                },
                y: {
                    title: {
                        formatter: function (seriesName) {
                            return '';
                        }
                    },
                    formatter: this.sizeData ?
                        (value, opts) => {
                            return this.formatFileSize(value);
                        }
                        :
                        (value, opts) => {
                            if (value === undefined) {
                                return '';
                            }
                            if(this.countData)
                                return value === 0.001 ? '0' : value.toString();
                            return value.toFixed(1) + ' %';
                        }
                }
            }
        };
        
        let eleTop = document.getElementById(this.topUid);
        if(eleTop) {
            this.chartTop = new ApexCharts(eleTop, options);
            this.chartTop.render();
        }
    }

    updateTopTimeout;

    updateTopSelection(minDate, maxDate, dontWait)
    {
        this.selectedRange.start = minDate;
        this.selectedRange.end = maxDate;
        let doIt = () => {
            let min = minDate.getTime();
            let max = maxDate.getTime();
            let rangeData = this.data.filter(x => {
                let xTime = x.x.getTime();
                return xTime >= min && xTime <= max;
            });
            let data = this.adjustData(rangeData, 500);

            this.chartTop.updateSeries([{
                name: this.seriesName,
                data: data
            }]);
        };

        if(dontWait)
            doIt();
        if(this.updateTopTimeout)
            clearTimeout(this.updateTopTimeout);
        this.updateTopTimeout = setTimeout(() => doIt(), 250);
    }


    createBottom(){
        let d = [] ;
        let yMax = 0;

        let brushEnd = this.buckets[this.buckets.length - 1].x;
        let brushStart = new Date(brushEnd.getTime() - 5 * 60000); // -5 minutes
        if(this.buckets[0].x > brushStart)
            brushStart = this.buckets[0].x;
        for(let b of this.buckets) {
            d.push({x: b.x, y: (b.y.toFixed(1) + ' %')});
            if(b.y > yMax)
                yMax = b.y;
        }
        if(yMax === 0.001)
            yMax = 1;
        this.selectedRange.start = brushStart;
        this.selectedRange.end = brushEnd;

        var options = {
            chart: {
                height: this.getBottomHeight(),
                id: this.bottomUid,
                type: 'bar',
                background: 'transparent',
                toolbar: {
                    show:false
                },
                sparkline: {
                    enabled: true
                },
                animations: {
                    enabled: false
                },
                brush: {
                    target: this.topUid,
                    enabled: true
                },
                yaxis: {
                    min:0
                },
                selection: {
                    enabled: true,
                    fill: {
                        color: "#fff",
                        opacity: 0.4
                    },
                    xaxis: {
                        min: brushStart.getTime(),
                        max: brushEnd.getTime()
                    }
                },
                events: {
                    selection: (context, xy) => {
                        this.updateTopSelection(new Date(xy.xaxis.min), new Date(xy.xaxis.max));
                    }
                }
            },
            markers: {
                size: 0
            },
            dataLabels: {
                enabled: false
            },
            theme: {
                mode: 'dark',
                palette: 'palette3'
            },
            grid: {
                padding: {
                    top: 0,
                    right:0,
                    bottom: 0,
                    left:0,
                },
                show:false
            },
            series: [
                {
                    name: this.seriesName,
                    data: d
                }
            ],
            colors: [
                'var(--accent)'
            ],
            stroke: {
                width:2
            },
            xaxis: {
                type:'datetime',
                axisTicks : {
                    show: false
                },
                labels : {
                    show: false
                }
            },
            yaxis: {
                min:0,
                max: yMax,
                show: false
            }
        };

        let ele = document.getElementById(this.bottomUid);
        if(ele) {
            this.chartBottom = new ApexCharts(ele, options);
            this.chartBottom.render();
        }
    }


    dashboardElementResized(event) {
        let height = this.getTopHeight();

        this.chartTop.updateOptions({
            chart: {
                height: height
            }
        }, true, false);
    }

}


export class NvidiaChart extends FFChart
{
    recentlyFinished;
    timer;
    existing;
    hasNoData;

    constructor(uid, args) {
        super(uid, args);
        this.recentlyFinished = args.flags === 1;
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
            this.createNoData('No encoders currently in use');
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
            spanText.innerText = this.recentlyFinished ? 'No files recently finished' : 'No upcoming files';

        chartDiv.appendChild(div);

    }

    createTableData(data)
    {
        let chartDiv = document.getElementById(this.chartUid);
        chartDiv.textContent = '';
        let tbody;
        
        const addRow = (label, value, icon) => {
            let tr = document.createElement("tr");
            tbody.appendChild(tr);
            let tdLabel = document.createElement("td");
            tdLabel.className = 'label';
            tdLabel.style.width = '10rem';
            if(icon){
                tdLabel.innerHTML = `<i class="${icon}" style="width: 1.5rem;text-align: center;"></i> ${label}`;
            }else {
                tdLabel.innerText = label;
            }
            tr.appendChild(tdLabel);

            if(value === 'COLSPAN') {
                tdLabel.colSpan = 2;
                tdLabel.style.fontWeight = 600;
                tdLabel.style.textTransform = 'uppercase';
                return;
            }
            let tdValue = document.createElement("td");
            tdValue.className = 'label';
            tdValue.style.textAlign = 'right';
            tdValue.innerText = value;
            tr.appendChild(tdValue);
        };
        let count = 0;
        for(let gpu of data) {
            let table = document.createElement('table');
            table.style.height = 'unset';
            table.style.width = 'calc(100% - 1rem)';
            tbody = document.createElement('tbody');
            table.appendChild(tbody);
            table.style.margin = '0 0.25rem';
            if(count > 0) {
                table.style.borderTop = 'solid 2px var(--border-color)';
                table.style.marginTop = '1rem';
            }
            
            addRow(gpu.Name, 'COLSPAN');
            addRow('Temperature', gpu.GpuTemperature + ' °C',  
                gpu.GpuTemperature < 40 ? 'fas fa-thermometer-quarter' : 
                gpu.GpuTemperature < 75 ? 'fas fa-thermometer-half' : 
                'fas fa-thermometer-full'
            );
            addRow('Memory', Math.round((gpu.MemoryUsedMib / gpu.MemoryTotalMib) * 100) + ' %', 'fas fa-memory');
            addRow('Fan Speed', gpu.FanSpeedPercent  + ' %', 'fas fa-fan');
            addRow('Processes', (gpu.Processes?.length?.toString() || '0'), 'fas fa-running');
            chartDiv.appendChild(table);
            ++count;
        }
        
    }
}

