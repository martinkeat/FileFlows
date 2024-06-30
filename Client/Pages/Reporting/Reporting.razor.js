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

    initCharts() {
        console.log('init charts!');
        this.initPieCharts();
        this.initBarCharts()
    }
    
    initPieCharts()
    {
        let pieCharts = document.querySelectorAll('.report-output svg.pie-chart');

        for (let pie of pieCharts) {
            console.log('pie', pie);
            let slices = pie.querySelectorAll('.slice');

            for (let slice of slices) {
                slice.addEventListener('mouseover', () => {
                    // Get tooltip content from data-tooltip attribute
                    let tooltipText = slice.getAttribute('data-title');

                    // Create a tooltip element
                    let tooltip = document.createElement('div');
                    tooltip.classList.add('svg-tooltip');
                    tooltip.textContent = tooltipText;

                    // Position the tooltip relative to the slice
                    let rect = slice.getBoundingClientRect();
                    tooltip.style.left = rect.left + rect.width / 2 + 'px';
                    tooltip.style.top = rect.top + rect.height / 2 + 'px';

                    // Append tooltip to the body or another container
                    document.body.appendChild(tooltip);
                });

                slice.addEventListener('mouseout', () => {
                    // Remove tooltip on mouse out
                    let tooltip = document.querySelector('.svg-tooltip');
                    if (tooltip) {
                        tooltip.parentNode.removeChild(tooltip);
                    }
                });
            }
        }
    }
    initBarCharts()
    {
        let barChartBars = document.querySelectorAll('.report-output rect.bar-chart-bar');

        for (let bar of barChartBars) {
            bar.addEventListener('mouseover', () => {
                // Get tooltip content from data-tooltip attribute
                let tooltipText = bar.getAttribute('data-title');

                // Create a tooltip element
                let tooltip = document.createElement('div');
                tooltip.classList.add('svg-tooltip');
                tooltip.textContent = tooltipText;

                // Position the tooltip relative to the slice
                let rect = bar.getBoundingClientRect();
                tooltip.style.left = rect.left + rect.width / 2 + 'px';
                tooltip.style.top = rect.top + rect.height / 2 + 'px';

                // Append tooltip to the body or another container
                document.body.appendChild(tooltip);
            });

            bar.addEventListener('mouseout', () => {
                // Remove tooltip on mouse out
                let tooltip = document.querySelector('.svg-tooltip');
                if (tooltip) {
                    tooltip.parentNode.removeChild(tooltip);
                }
            });
        }        
    }
}