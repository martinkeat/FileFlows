/**
 * Creates the FlowColorPicker instance and returns it
 * @param dotNetObject The calling dotnet object
 * @param uid the UID of the textarea element
 * @param initialValue the initial color value
 * @returns {FlowColorPicker} the FlowColorPicker instance
 */
export function createFlowColorPicker(dotNetObject, uid, initialValue)
{
    return new FlowColorPicker(dotNetObject, uid, initialValue);
}

class FlowColorPicker
{
    visible = false;
    input;
    baseR = 0; baseG = 0; baseB = 0;
    // Define the new gradient colors
    gradients = [
        [255, 0, 0],   // Red
        [255, 255, 0], // Yellow
        [0, 255, 0],   // Green
        [0, 255, 255], // Cyan
        [0, 0, 255],   // Blue
        [255, 0, 255], // Magenta
        [255, 0, 0]    // Red (repeated for smooth transition)
    ];

    constructor(dotNetObject, uid, initialValue) {
        this.dotNetObject = dotNetObject;
        let picker = document.getElementById(uid + '-picker');
        this.picker = picker;
        this.input = document.getElementById(uid + '-input');
        this.input.addEventListener('keyup', (e) => {
            this.manualEntry();
        });
        this.colorPickerRgb = picker.querySelector('.color-picker-rgb');
        this.preview = document.getElementById(uid +'-preview');
        this.preview.addEventListener('click', (e) => {
            this.visible = !this.visible;
            picker.className = 'color-picker ' + (this.visible ? 'visible' : 'hidden');
            e.stopPropagation();
            this.dotNetObject.invokeMethodAsync("pickerOpened", this.visible, this.currentValue);
        });

        for(let psw of picker.querySelectorAll('.color-palette-sw div')){
            psw.addEventListener('click', (e) => {
                let hex = psw.getAttribute('data-color');
                if(!/#[a-fA-F0-9]{6}/.test(hex)) {
                    let pswColor = hex.split(',');
                    let pswR = parseInt(pswColor[0].trim());
                    let pswG = parseInt(pswColor[1].trim());
                    let pswB = parseInt(pswColor[2].trim());
                    hex = this.rgbToString(pswR, pswG, pswB);
                }
                this.updateValue(hex.toUpperCase());
                this.close();
            });
        }
        this.initSlider(picker);
        this.initArea(picker);
        this.manualEntry();
    }

    initArea(picker) {
        let crosshair = picker.querySelector('.crosshair');

        document.addEventListener('click', (event) => {
            if(!this.visible)return;
            const withinBoundaries = event.composedPath().includes(picker);
            if(!withinBoundaries) {
                this.close();
            }
        });
        let area = picker.querySelector('.color-picker-rgb');
        area.addEventListener('click', (event) => {
            crosshair.style.left = (event.offsetX + 5.5) + 'px';
            crosshair.style.top = (event.offsetY + 6) + 'px';
            this.mainPickerClicked(event);
        });

        let mouseMoveEvent = (event) => {
            crosshair.style.left = (event.offsetX + 5.5) + 'px';
            crosshair.style.top = (event.offsetY + 6) + 'px';
            this.mainPickerClicked(event);
        }
        area.addEventListener('mousedown', () => {
            area.addEventListener('mousemove', mouseMoveEvent);
        });
        document.addEventListener('mouseup', (event) => {
            area.removeEventListener('mousemove', mouseMoveEvent)
        });
    }

    initSlider(picker) {
        let sliderIndicator = picker.querySelector('.color-slider-bar-indicator');
        let colorSlider = picker.querySelector('.color-slider');
        colorSlider.addEventListener('click', (event) => {
            sliderIndicator.style.top = (event.offsetY - 3) + 'px';
            this.moveSlider(event);
        });
        let mouseMoveEvent = (event) => {
            sliderIndicator.style.top = (event.offsetY - 3) + 'px';
            this.moveSlider(event);
        }
        colorSlider.addEventListener('mousedown', () => {
            colorSlider.addEventListener('mousemove', mouseMoveEvent);
        });
        document.addEventListener('mouseup', (event) => {
            colorSlider.removeEventListener('mousemove', mouseMoveEvent)
        });
    }

    close() {
        this.visible = false;
        this.picker.className = 'color-picker ' + (this.visible ? 'visible' : 'hidden');
        this.dotNetObject.invokeMethodAsync("pickerOpened", this.visible, this.currentValue);
    }

    moveSlider(e)
    {
        let percent = e.offsetY / 150.3;
        percent = Math.max(0, Math.min(1, percent));

        let gradientIndex = Math.floor(percent * (this.gradients.length - 1));

        let c1r = this.gradients[gradientIndex][0];
        let c1g = this.gradients[gradientIndex][1];
        let c1b = this.gradients[gradientIndex][2];

        let c2r, c2g, c2b;
        if (gradientIndex < this.gradients.length - 1) {
            c2r = this.gradients[gradientIndex + 1][0];
            c2g = this.gradients[gradientIndex + 1][1];
            c2b = this.gradients[gradientIndex + 1][2];
        } else {
            c2r = this.gradients[gradientIndex][0];
            c2g = this.gradients[gradientIndex][1];
            c2b = this.gradients[gradientIndex][2];
        }

        let start = (gradientIndex / (this.gradients.length - 1)) * 100;
        let end = ((gradientIndex + 1) / (this.gradients.length - 1)) * 100;

        let shifted = (percent * 100) - start;
        let newP = shifted / (end - start);

        this.baseR = this.adjustPercent(c1r, c2r, newP);
        this.baseG = this.adjustPercent(c1g, c2g, newP);
        this.baseB = this.adjustPercent(c1b, c2b, newP);

        this.BaseColor = this.rgbToString(this.baseR, this.baseG, this.baseB);
        this.colorPickerRgb.style.backgroundColor = this.BaseColor;

        if(!e.dontCalculateColor)
            this.calculateColor();
    }

    calculateColor()
    {
        let wPercent = this.PointerX / 211.2;
        let r = this.adjustPercent(255, this.baseR, wPercent);
        let g = this.adjustPercent(255, this.baseG, wPercent);
        let b = this.adjustPercent(255, this.baseB, wPercent);
        if(isNaN(r) || isNaN(g) || isNaN(b))
            return;

        let bPercent = this.PointerY / 147.1;
        r = this.adjustPercent(r,0, bPercent);
        g = this.adjustPercent(g,0, bPercent);
        b = this.adjustPercent(b,0, bPercent);

        this.updateValue(this.rgbToString(r, g, b))
    };

    rgbToString(r, g, b) {
        return ("#" + this.decimalToHex(r, 2) + this.decimalToHex(g, 2) + this.decimalToHex(b, 2)).toUpperCase();
    }

    updateValue(value){
        this.currentValue = value;
        this.preview.style.backgroundColor = value;
        this.dotNetObject.invokeMethodAsync("updateValue", value);
    }

    adjustPercent(zeroC, hundredC, percent)
    {
        return Math.min(255, Math.max(0,Math.round(zeroC + percent * (hundredC - zeroC))));
    }

    mainPickerClicked(e)
    {
        this.PointerX = e.offsetX;
        this.PointerY = e.offsetY;
        this.calculateColor();
    }

    decimalToHex(d, padding)
    {
        let hex = Number(d).toString(16);
        padding = typeof (padding) === "undefined" || padding === null ? padding = 2 : padding;

        while (hex.length < padding) {
            hex = "0" + hex;
        }

        return hex;
    }

    manualEntry() {
        let v = this.input.value;
        if (!v) return;
        if (!/^#[a-fA-F0-9]{6}$/.test(v)) return; // not a valid color

        // Update the color picker with the entered color
        this.updateValue(v);

        // Make the entered color pure bright to find it in the wheel
        let r = parseInt(v.substring(1, 3), 16); // Extracting red value
        let g = parseInt(v.substring(3, 5), 16); // Extracting green value
        let b = parseInt(v.substring(5, 7), 16); // Extracting blue value

        // Adjust the color to its pure bright form
        r = this.adjustPercent(255, r, 1);
        g = this.adjustPercent(255, g, 1);
        b = this.adjustPercent(255, b, 1);
        r = this.adjustPercent(0, r, 1);
        g = this.adjustPercent(0, g, 1);
        b = this.adjustPercent(0, b, 1);


        // Calculate the position of the slider based on the color
        let selectedColorIndex = 0;
        let minDistance = Infinity;
        for (let i = 0; i < this.gradients.length; i++) {
            let [gr, gg, gb] = this.gradients[i];
            let distance = Math.abs(r - gr) + Math.abs(g - gg) + Math.abs(b - gb);
            if (distance < minDistance) {
                minDistance = distance;
                selectedColorIndex = i;
            }
        }

        // Calculate the position of the slider based on the selected color index
        let sliderPosition = (selectedColorIndex / (this.gradients.length - 1)) * 150.3; // Adjust as needed
        
        this.moveSlider({ offsetY: sliderPosition, dontCalculateColor: true});
        this.currentValue = v;
        
        // Update the position of the slider
        let sliderIndicator = this.picker.querySelector('.color-slider-bar-indicator');
        sliderIndicator.style.top = sliderPosition + 'px';


        // Calculate the position of the crosshair based on the green and blue values
        let crosshairX = (g / 255) * 211.2; // 211.2 is the width of the color area
        let crosshairY = (b / 255) * 147.1; // 147.1 is the height of the color area

        // Update the position of the crosshair
        let crosshair = this.picker.querySelector('.crosshair');
        crosshair.style.left = (crosshairX + 5.5) + 'px'; // 5.5 is half the width of the crosshair
        crosshair.style.top = (crosshairY + 6) + 'px'; // 6 is half the height of the crosshair

    }

    // Convert RGB to Lab
    rgbToLab(r, g, b) {
        // Convert RGB to XYZ
        let x = 0.4124564 * r + 0.3575761 * g + 0.1804375 * b;
        let y = 0.2126729 * r + 0.7151522 * g + 0.072175 * b;
        let z = 0.0193339 * r + 0.119192 * g + 0.9503041 * b;
    
        // Normalize XYZ
        x /= 95.047;
        y /= 100;
        z /= 108.883;
    
        // Convert XYZ to Lab
        x = x > 0.008856 ? Math.pow(x, 1 / 3) : (7.787 * x) + (16 / 116);
        y = y > 0.008856 ? Math.pow(y, 1 / 3) : (7.787 * y) + (16 / 116);
        z = z > 0.008856 ? Math.pow(z, 1 / 3) : (7.787 * z) + (16 / 116);
    
        let L = (116 * y) - 16;
        let a = 500 * (x - y);
        let b2 = 200 * (y - z);
    
        return [L, a, b2];
    }

    rgbToHsl(r, g, b) {
        r /= 255, g /= 255, b /= 255;
        let max = Math.max(r, g, b), min = Math.min(r, g, b);
        let h, s, l = (max + min) / 2;

        if (max === min) {
            h = s = 0; // achromatic
        } else {
            let d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            switch (max) {
                case r: h = (g - b) / d + (g < b ? 6 : 0); break;
                case g: h = (b - r) / d + 2; break;
                case b: h = (r - g) / d + 4; break;
            }
            h /= 6;
        }

        return [h, s, l];
    }
}