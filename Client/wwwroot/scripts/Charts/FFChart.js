export function initDashboard(uid, Widgets, csharp, isReadOnly){
    if(!Widgets)
        return;
    disposeAll();
    destroyDashboard();
    
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
    intDashboardActual(uid, csharp, isReadOnly);
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

function intDashboardActual(uid, csharp, isReadOnly) {
    let grid = GridStack.init({
        cellHeight:170,
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
    div.className = 'grid-stack-item widget';
    div.setAttribute('gs-w', p.width);
    div.setAttribute('gs-h', p.height);
    div.setAttribute('gs-x', p.x);
    div.setAttribute('gs-y', p.y);
        
    if(p.type === 1)
        div.setAttribute('gs-no-resize', 1);

    let title = document.createElement('div');
    div.appendChild(title);
    title.className = 'title draghandle';
    let icon = document.createElement('i');
    title.appendChild(icon);
    icon.className = p.icon;
    let spanTitle = document.createElement('span');
    title.appendChild(spanTitle);
    spanTitle.innerText = p.name;

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
    div.appendChild(content);
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
    else if(type == 'Nvidia' || type === 121)
        window.FlowCharts[uid] = new NvidiaChart(uid, args);
    else 
        console.log('unknown type: ' + type);
    
}

class FFChart {
    uid;
    chartUid;
    data;
    url;
    seriesName;
    chart;
    chartBottomPad = 18;
    csharp;
    args;


    constructor(uid, args, dontGetData) {
        this.uid = uid;
        this.chartUid = uid + '-chart';
        this.csharp = args.csharp;
        
        this.url = args.url;
        this.seriesName = args.title;

        this.ele = document.getElementById(uid);
        if(!this.ele)
            return;
        this.ele.classList.add('chart-' + args.type);
        this.ele.addEventListener('dashboardElementResized', (event) => this.dashboardElementResized(event));
    
        if(dontGetData !== true)
            this.getData();
    }
    getHeight() {
        let chartDiv = this.ele.querySelector('.content');
        return chartDiv.clientHeight - this.chartBottomPad - 10;
    }

    dashboardElementResized(event) {
        if(!this.chart)
            return;
        
        let height = this.getHeight();
        this.chart.updateOptions({
            chart: {
                height: height
            }
        }, true, false);
    }

    formatFileSize(size, dps) {
        if (size === undefined) {
            return '';
        }

        if(dps === undefined)
            dps = 2;
        let neg = size < 0;
        size = Math.abs(size);
        let sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
        let order = 0;
        while (size >= 1000 && order < sizes.length - 1) {
            order++;
            size = size / 1000;
        }
        if(neg)
            size *= -1;
        return size.toFixed(dps) + ' ' + sizes[order];
    }

    async getData() {
        if(this.disposed)
            return;

        let data = await this.fetchData()
        data = this.fixData(data);

        if(this.hasData(data) === false){
            //document.getElementById(this.uid).style.display = 'none';
            return;
        }
        this.createChart(data);
    }
    
    async fetchData(){
        let response = await fetch(this.url);
        return await response.json();
    }
    
    hasData(data) {
        return data?.length;
    }
    
    fixData(data) {
        return data;
    }
    
    getChartOptions(data) {
        return {};
    }
    
    createChart(data, count){
        let height = this.getHeight();
        if(height < 0)
        {
            if(count > 5)
                return;
            setTimeout(() => this.createChart(data, count + 1), 50);
            return;
        }
        
        let defaultOptions = {
            chart: {              
                background: 'transparent',
                height: height,
                zoom: {
                    enabled: false
                },
                toolbar: {
                    show: false
                }
            },
            theme: {
                mode: 'dark'
            },
            stroke: {
                colors: ['#ffffff']
            },
            grid: {
                borderColor: '#90A4AE33'
            }
        };
        let instanceOptions = this.getChartOptions(data);
        let options = this.mergeDeep(defaultOptions, instanceOptions);
               
        let ele = document.getElementById(this.chartUid);
        if(ele) {
            this.chart = new ApexCharts(ele, options);
            this.chart.render();
        }
    }
    isObject(item) {
        return (item && typeof item === 'object' && !Array.isArray(item));
    }
    mergeDeep(target, ...sources) {
        if (!sources.length) return target;
        const source = sources.shift();

        if (this.isObject(target) && this.isObject(source)) {
            for (const key in source) {
                if (this.isObject(source[key])) {
                    if (!target[key]) Object.assign(target, { [key]: {} });
                    this.mergeDeep(target[key], source[key]);
                } else {
                    Object.assign(target, { [key]: source[key] });
                }
            }
        }

        return this.mergeDeep(target, ...sources);
    }

    dispose() {
        this.disposed = true;
        this.ele.removeEventListener('dashboardElementResized', dashboardElementResized);
    }
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
        data = data.map((x, index) => ({ x: index, y: x.Value}));
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
        if (!data?.length || (data[0].Name && data[0].Value) === false)
            return data;

        //statistic data, convert it
        let newData = {};
        for (let d of data) {
            if (newData[d.Value])
                newData[d.Value] = newData[d.Value] + 1;
            else
                newData[d.Value] = 1;
        }
        let temp = [];
        Object.keys(newData).forEach(x => {
            temp.push({
                label: x,
                value: newData[x]
            })
        });
        temp.sort((a, b) => {
            return b.value - a.value
        });

        data = {
            labels: [],
            series: []
        };
        for(let v of temp)
        {
            data.labels.push(v.label);
            data.series.push(v.value);
        }
        return data;
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
        if (!data?.length || (data[0].Name && data[0].Value) === false)
            return data;

        //statistic data, convert it
        let newData = {};
        for (let d of data) {
            if (d.Value === 'mpeg2video')
                d.Value = 'mpeg2'; // too long
            if (newData[d.Value])
                newData[d.Value] = newData[d.Value] + 1;
            else
                newData[d.Value] = 1;
        }
        data = [];
        Object.keys(newData).forEach(x => {
            data.push({x: x, y: newData[x]});
        });
        return data;
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
            let response = await fetch(`${this.url}?since=${fullDate}T${fullTime}Z`);
            data = await response.json();
        }else {
            let response = await fetch(this.url);
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
                    formatter: (value, opts) => new Date(value).toLocaleTimeString()
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


export class LibraryFileTable extends FFChart
{
    lblIncrease = 'Increase';
    lblDecrease = 'Decrease';
    recentlyFinished;
    timer;
    existing;
    hasNoData;
    
    constructor(uid, args) {
        super(uid, args);
        console.log('args!', args);
        this.recentlyFinished = args.flags === 1;
    }
    
    formatShrinkage(original, final)
    {
        let diff = Math.abs(original - final);
        return this.formatFileSize(diff) + (original < final ? " " + this.lblIncrease : " " + this.lblDecrease) +
        "\n" + this.formatFileSize(final) + " / " + this.formatFileSize(original);
    }
    
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
            console.log('data', data);
            return data;
        }
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
            this.createNoData();
    }

    createNoData(data){
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
        console.log('onExecutorsUpdated', data);
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
        console.log('onExecutorsUpdated.createChart', data.length);
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
            if(!this.runners[worker.Uid]){ 
                this.createRunner(chartDiv, worker);
            }
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
    
    createRunner(chartDiv, runner)
    {
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
        console.log('onExecutorsUpdated.runner', runner)
        this.csharp.invokeMethodAsync("HumanizeStepName", runner.currentPartName).then((step) =>
        {            
            let args = {
                file: runner.libraryFile.name,
                node: runner.nodeName,
                library: runner.library.name,
                step: step,
                time: this.timeDiff( Date.parse(runner.startedAt), Date.now())
            };
            let eleInfo = document.getElementById('runner-' + runner.uid + '-info');
            if(eleInfo)
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


export class NvidiaChart extends FFChart
{
    recentlyFinished;
    timer;
    existing;
    hasNoData;

    constructor(uid, args) {
        super(uid, args);
        console.log('args!', args);
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
            this.createNoData();
    }

    createNoData(data){
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
            
            console.log('gpu', gpu);
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

