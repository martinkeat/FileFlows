export class FFChart {
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
        let response = await ff.doFetch(this.url);
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
