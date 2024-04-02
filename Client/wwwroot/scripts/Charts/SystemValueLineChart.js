export function newSystemValueLineChart(uid, args){
    if(!window.FlowCharts)
        window.FlowCharts = {};
    window.FlowCharts[uid] = new SystemValueLineChart(uid, args);
}


export function dispose(uid) {
    let chart = window.FlowCharts[uid];
    if(chart)
        chart.dispose();
}


export class SystemValueLineChart{
    uid;
    bottomUid;
    topUid;
    chartBottom;
    sizeData;
    data;
    buckets;
    url;
    lastFetch;
    timer;
    disposed;
    seriesName;
    
    selectedRange = {
        start: null,
        end: null
    };
    
    constructor(uid, args) {
        this.uid = uid;
        
        this.bottomUid = uid + '-bottom';
        this.topUid = uid + '-top';
        this.sizeData = !!args?.sizeData;
        this.url = args.url;
        this.seriesName = args.title;
        
        this.getData();
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
        
        for(let d of data){
            if(typeof(d.x) === 'string')
                d.x = new Date(Date.parse(d.x));
        }
        
        if(this.lastFetch)
            this.data = this.data.concat(data);
        else {
            this.data = data;
        }
        this.lastFetch = this.data[this.data.length -1].x;

        let buckets = this.adjustData(this.data, 100);
        let showBottom = buckets.length !== this.data.length; 
        if(showBottom)
        {            
            if(this.chartBottom)
                this.updateBottom(buckets);
            else
                this.buckets = buckets;
        }else {
            this.selectedRange.start = data[0].x;
            this.selectedRange.end = data[data.length - 1].x;
        }
        
        if(!this.chartTop)
            this.createTop();
        if(!this.chartBottom && showBottom)
            this.createBottom();
        
        
        if(this.timer)
            clearTimeout(this.timer);
        if(!this.disposed)
            this.timer = setTimeout(() => this.getData(), 5000);
    }
    
    updateData(data){
        let animate = false;
        let actualData = [];
        for(let d of data){
            if(typeof(d.time) === 'string')
                d.time = new Date(Date.parse(d.time))
            actualData.push({ x: d.time, y: d.value});
        }
        console.log('updating chart data', actualData);
        // this.chartTop.updateSeries([{
        //     data: actualData
        // }], animate);
        this.chartTop.appendData([{
            data: actualData
        }]);
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
                height: 100,
                type: "area",
                background: 'transparent',
                toolbar: {
                    autoSelected: 'pan',
                    show:false    
                },
                sparkline: {
                    enabled: true
                }
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
                min:0
            },
            markers: {
                colors: ["#00BAEC"],
                strokeColors: "#00BAEC",
                strokeWidth: 3  
            },
            tooltip: {
                x: { 
                    show:true,
                    formatter: (value, opts) => new Date(value).toLocaleTimeString()
                },
                y: {
                    formatter: this.sizeData ?
                        (value, opts) => {
                            if (value === undefined) {
                                return '';
                            }
                            let sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
                            let order = 0;
                            while (value >= 1000 && order < sizes.length - 1) {
                                order++;
                                value = value / 1000;
                            }
                            return value.toFixed(2) + ' ' + sizes[order];
                        }
                        :
                        (value, opts) => {
                            if (value === undefined) {
                                return '';
                            }
                            return value.toFixed(1) + ' %';
                        }
                }
            }
        };

        this.chartTop = new ApexCharts(document.getElementById(this.topUid), options);
        this.chartTop.render();
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
        this.selectedRange.start = brushStart;
        this.selectedRange.end = brushEnd;
        
        var options = {
            chart: {
                height: 30,
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

        this.chartBottom = new ApexCharts(document.getElementById(this.bottomUid), options);
        this.chartBottom.render();
    }
    
    dispose() {
        this.disposed = true;      
        console.log('disposed!!!');
    }
    
    
}