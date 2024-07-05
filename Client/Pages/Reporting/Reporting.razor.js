/**
 * Creates the Reporting instance and returns it
 * @param dotNetObject The calling dotnet object
 * @returns {InputCode} the Reporting instance
 */
export function createReporting(dotNetObject)
{
    return new Reporting(dotNetObject);
}

export class Reporting {
    
    constructor() {
        this.COLORS = [
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
            'var(--yellow)',
            'var(--error)',
        ];
    }
    initCharts() {
        this.initPieCharts();
        this.initBarCharts();
        this.initLineCharts();
        this.initTreeMaps();
    }
    initPieCharts()
    {
        let hidden = document.querySelectorAll('.report-output .report-pie-chart-data');
        for(let hPid of hidden)
        {
            let ele = document.createElement('div');
            hPid.insertAdjacentElement('afterend', ele);
            
            let data = JSON.parse(hPid.value);
            let series = [];
            let labels = [];
            Object.keys(data).forEach((key) => {
                series.push(data[key]);
                labels.push(key);                
            });
            
            let options = {
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
                colors: this.COLORS,
                series: series,
                labels: labels
            };
            
            this.createChart(ele, options)
        }
    }
    
    formatValue(value, formatter, yAxis)
    {
        if(!formatter) {
            if(typeof(value) === 'number') {
                // Check if the value is an integer
                if (Number.isInteger(value)) {
                    return value;
                }
                // Round the value to one decimal place
                return Math.round(value * 10) / 10;
            }
        }
        if(formatter.toLowerCase() === 'filesize')
            return this.formatBytes(value, yAxis ? 0 : 2);
        return value;
    }
    
    formatBytes(size, decimalPlaces) {
        const sizes = ["B", "KB", "MB", "GB", "TB"];
        let order = 0;
        let num = size;

        while (num >= 1000 && order < sizes.length - 1) {
            order++;
            num /= 1000;
        }

        if(decimalPlaces === undefined)
            decimalPlaces = 2;

        return `${num.toFixed(decimalPlaces)} ${sizes[order]}`;
    }
    initBarCharts()
    {
        let hidden = document.querySelectorAll('.report-output .report-bar-chart-data');
        for(let hPid of hidden)
        {
            let ele = document.createElement('div');
            hPid.insertAdjacentElement('afterend', ele);

            let args = JSON.parse(hPid.value);
            let data = args.data;
            let series = [];
            Object.keys(data).forEach((key) => {
                series.push({ x: key, y: data[key]});
            });

            let options= {
                chart: {
                    type: 'bar'
                },
                tooltip: {
                    y: {
                        title: {
                            formatter: function(seriesName) {
                                return '';
                            }
                        },
                        formatter: (value) => {
                            return value ? this.formatValue(value, args.yAxisFormatter) : '0';
                        }
                    }
                },
                dataLabels: {
                    enabled: false
                },
                yaxis: {
                    labels : {
                        formatter: (value) => {
                            return value ? this.formatValue(value, args.yAxisFormatter, true) : '';
                        }
                    }
                },
                colors: this.COLORS,
                series: [{ data: series }]
            };

            this.createChart(ele, options)
        }
    }

    initLineCharts()
    {
        let hidden = document.querySelectorAll('.report-output .report-line-chart-data');
        for(let hPid of hidden)
        {
            let ele = document.createElement('div');
            ele.classList.add('report-chart');
            hPid.insertAdjacentElement('afterend', ele);

            let parameters = JSON.parse(hPid.value);
            let args = parameters.data;
            let dates = false;
            for(let i= 0;i<args.labels.length;i++)
            {
                let label = args.labels[i];
                if(/20[\d]{2}\-/.test(label)) {
                    let utcDate = new Date(label); // Parse the UTC date string into a Date object
                    let timezoneOffset = utcDate.getTimezoneOffset(); // Get local timezone offset in minutes                     
                    // Adjust date by adding the local timezone offset
                    utcDate.setMinutes(utcDate.getMinutes() - timezoneOffset);
                    args.labels[i] = utcDate;
                    dates = true;
                }                
            }
            if(dates)
            {
                for(let s of args.series){
                    for(let i=0;i<s.data.length;i++)
                    {
                        s.data[i] = { x: args.labels[i].getTime(), y: s.data[i]};
                    }
                }
            }

            let options= {
                series: args.series,//[{ data: series}],
                chart: {
                    type: 'area',
                    stacked: false,
                    zoom: {
                        type: 'x',
                        enabled: true,
                        autoScaleYaxis: true
                    },
                    toolbar: {
                        show: true,
                        autoSelected: 'zoom',
                        tools: {
                            download: false,
                            selection: false,
                            zoom: true,
                            zoomin: false,
                            zoomout: false,
                            pan: true
                        },
                    }
                },
                xaxis: {
                    categories: args.labels
                },
                yaxis: {
                    labels : {
                        formatter: (value) => {
                            return value ? this.formatValue(value, args.yAxisFormatter, true) : '';
                        }
                    }
                },
                tooltip: {
                    y: {
                        title: {
                            formatter: function(seriesName) {
                                return args.series.length === 1 ? '' : seriesName;
                            }
                        },
                        formatter: (value) => {
                            return value ? this.formatValue(value, args.yAxisFormatter) : '0';
                        }
                    }                    
                },
                dataLabels: {
                    enabled: false,
                },
                stroke: {
                    width: 2,
                    curve: 'smooth',
                    colors: this.COLORS
                },
                colors: this.COLORS,
                fill: {
                    type: 'gradient',
                    gradient: {
                        shadeIntensity: 1,
                        inverseColors: false,
                        opacityFrom: 0.5,
                        opacityTo: 0,
                        //stops: [0, 90, 100]
                    },
                },
                markers: {
                    size: 0
                }
            };

            if(dates) {
                options.xaxis.type = 'datetime';
                delete options.xaxis.categories;
            }
            
            console.log('chart options', JSON.parse(JSON.stringify(options)));

            this.createChart(ele, options)
        }
    }

    initTreeMaps()
    {
        let hidden = document.querySelectorAll('.report-output .report-tree-map-data');
        for(let hPid of hidden)
        {
            let ele = document.createElement('div');
            ele.classList.add('apex-tree-map');
            hPid.insertAdjacentElement('afterend', ele);

            let data = JSON.parse(hPid.value);

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
            
            // Define the threshold for the maximum number of individual entries
            const threshold = 10;

            // Sort the data in descending order
            results.sort((a, b) => b.y - a.y);

            // Separate the top entries and the rest
            let topEntries = results.slice(0, threshold);
            let otherEntries = results.slice(threshold);

            // Sum the values of the smaller entries
            let otherValue = otherEntries.reduce((sum, entry) => sum + entry.y, 0);

            // Add the "Other" category
            if (otherValue > 0) {
                topEntries.push({ x: 'Other', y: otherValue });
            }

            let options = {
                chart: {
                    type: 'treemap'
                },
                dataLabels: {
                    format:  "truncate"
                },
                colors: ['#33b2df'],
                stroke:{
                    colors:['#33b2df']
                },
                grid: {
                    borderColor: '#90A4AE33'
                },
                series: [{
                    data:topEntries
                }]
            };

            this.createChart(ele, options)
        }
    }
    
    
    createChart(ele, options){
        ele.style.margin = '0';
        let defaultOptions = {
            chart: {
                background: 'transparent',
                height: 220,
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
        let completeOptions = this.mergeDeep(defaultOptions, options);
        this.chart = new ApexCharts(ele, completeOptions);
        this.chart.render();
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
}