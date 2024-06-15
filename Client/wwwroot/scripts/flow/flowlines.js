class ffFlowLines {
    
    constructor(ffFlow) 
    {
        this.ffFlow = ffFlow;
        this.lineWidth = 1;
        this.ioNode = null;
        this.ioSelected = null;
        this.ioContext = null;
        this.ioSourceBounds = null;
        this.ioCanvasBounds = null;
        this.ioOutputConnections = new Map();
        this.ioLines = [];
        this.ioSelectedConnection = null;
        this.accentColor = null;
        this.lineColor = null;
        this.errorColor = null;
        this.errorDash = [2, 2];
        this.ioOffset = 14.5;
        this.initCanvas();
    }

    reset() {

        if (this.ffFlow.eleFlowParts.classList.contains('drawing-line') === true)
            this.ffFlow.eleFlowParts.classList.remove('drawing-line');
        this.ioNode = null;
        this.ioSelected = null;
        this.ioContext = null;
        this.ioSourceBounds = null;
        this.ioCanvasBounds = null;
        this.ioLines = [];
        this.ioOutputConnections = new Map();
        this.ioSelectedConnection = null;
    }

    ioDown(event) {

        if (this.ffFlow.eleFlowParts.classList.contains('drawing-line') === false)
            this.ffFlow.eleFlowParts.classList.add('drawing-line');
        let outputNode = event.target;
        while(outputNode.parentNode != null && /output\s/.test(outputNode.className) === false)
            outputNode = outputNode.parentNode;
        
        if(!outputNode || !outputNode.tagName)
            return;
        
        this.ioNode = outputNode;
        this.ioSelected = outputNode;

        this.ioCanvasBounds = this.ffFlow.canvas.getBoundingClientRect();
        let srcBounds = this.ioNode.getBoundingClientRect();
        let srcX = (srcBounds.left - this.ioCanvasBounds.left);
        let srcY = (srcBounds.top - this.ioCanvasBounds.top);
        let isError = /output--1/.test(this.ioNode.className);
        if(isError)
        {
            srcX += 10;
            srcY += 15;
        }else {
            srcX += 15;
        }
        this.ioSourceBounds = { left: srcX, top: srcY, isError: isError };

        if (this.selectedOutput != null) {
        }
        else {
            // start drawing line
        }
    };

    ioMouseMove(event) {
        if (!this.ioNode)
            return;

        let destX = this.ffFlow.translateCoord(event.clientX, true) - this.ioCanvasBounds.left;
        let destY = this.ffFlow.translateCoord(event.clientY, true) - this.ioCanvasBounds.top;
        this.redrawLines();
        let overInput = !!this.ffFlow.eleFlowParts.querySelector('.flow-part:hover');

        this.drawLineToPoint({ 
            srcX: this.ioSourceBounds.left, srcY:this.ioSourceBounds.top, isError:this.ioSourceBounds.isError,
            destX, destY, color: overInput ? '#ff0090' : null });
    };


    ioMouseUp(event) {
        if (!this.ioNode)
            return;
        
        let suitable = false;
        let target = event.target;
        while(target.parentNode != null && suitable === false) {
            suitable = target?.classList?.contains('flow-part') === true && target?.classList?.contains('has-input') === true;
            if(!suitable)
                target = target.parentNode;
        }
        
        if(suitable) // get the input for the target
            target = target.querySelector('.input');                    
        
        if (suitable && target) {
            let input = this.isInput ? this.ioNode : target;
            let output = this.isInput ? target : this.ioNode;
            let outputId = output.getAttribute('x-uid');

            if (input.classList.contains('connected') === false)
                input.classList.add('connected');
            if (output.classList.contains('connected') === false)
                output.classList.add('connected');

            let connections = this.ioOutputConnections.get(outputId);
            if (!connections) {
                this.ioOutputConnections.set(outputId, []);
                connections = this.ioOutputConnections.get(outputId);
            }
            let index = parseInt(input.getAttribute('x-input'), 10);

            let part = input.parentNode.parentNode.getAttribute('x-uid');
            let existing = connections.filter(x => x.index == index && x.part == part);
            if (!existing || existing.length === 0) {

                if (this.ffFlow.SingleOutputConnection) {
                    connections = [{ index: index, part: part }];
                    this.ffFlow.History.perform(new FlowActionConnection(this.ffFlow, outputId, connections));
                }
                else
                    connections.push({ index: index, part: part });
            }
        }

        if (this.ffFlow.eleFlowParts.classList.contains('drawing-line') === true)
            this.ffFlow.eleFlowParts.classList.remove('drawing-line');

        this.ioNode = null;
        this.ioSelected = null;
        this.redrawLines();
    };

    redrawLines() {
        this.ffFlow.selectConnection();
        this.ioLines = [];
        let outputs = this.ffFlow.eleFlowParts.querySelectorAll('.flow-part .output');
        for (let o of this.ffFlow.eleFlowParts.querySelectorAll('.flow-part .output, .flow-part .input'))
            o.classList.remove('connected');
        let canvas = this.ffFlow.canvas;
        if (!this.ioContext) {
            this.ioContext = canvas.getContext('2d');
        }
        this.ioContext.clearRect(0, 0, canvas.width, canvas.height);
        this.redrawGrid(canvas);

        for (let output of outputs) {
            let outputUid = output.getAttribute('x-uid');
            let connections = this.ioOutputConnections.get(outputUid);
            if (!connections)
                continue;
            for (let input of connections) {
                let inputEle = this.ffFlow.getFlowPartInput(input.part, input.index);
                if (!inputEle)
                    continue;
                this.drawLine(inputEle, output, input);
            }
        }
        this.drawDottedSelection();
    };
    
    drawDottedSelection(context){
        if(this.ffFlow.Mouse.canvasSelecting !== true)
            return;
        let canvas = this.ffFlow.canvas;
        let canvasBounds = canvas.getBoundingClientRect();
        
        let x1 = this.ffFlow.Mouse.initialX;
        let y1 = this.ffFlow.Mouse.initialY;
        let x2 = this.ffFlow.Mouse.currentX;
        let y2 = this.ffFlow.Mouse.currentY;
        
        x1 = this.ffFlow.translateCoord(x1, true);
        x2 = this.ffFlow.translateCoord(x2, true);
        y1 = this.ffFlow.translateCoord(y1, true);
        y2 = this.ffFlow.translateCoord(y2, true);
        
        x1 -= canvasBounds.left;
        y1 -= canvasBounds.top;
        this.ioContext.strokeStyle = this.accentColor;
        this.ioContext.setLineDash([2]);
        this.ioContext.strokeRect(x1, y1, x2, y2);
        this.ioContext.setLineDash([0]);
    }

    drawLine(input, output, connection) {
        if (!input || !output)
            return;

        let src = output;
        let dest = input;
        if (output.classList.contains('connected') === false)
            output.classList.add('connected');
        if (input.classList.contains('connected') === false)
            input.classList.add('connected');
        let srcBounds = src.getBoundingClientRect();
        let destBounds = dest.getBoundingClientRect();

        let canvas = this.ffFlow.canvas;
        let canvasBounds = canvas.getBoundingClientRect();
        let srcX = (srcBounds.left - canvasBounds.left) + this.ioOffset;
        let srcY = (srcBounds.top - canvasBounds.top) + this.ioOffset;
        let destX = (destBounds.left - canvasBounds.left) + this.ioOffset;
        let destY = (destBounds.top - canvasBounds.top) + this.ioOffset;

        // eliminates any pixel off issues now the grid snaps to 10px
        srcX = Math.round(srcX / 10) * 10;
        srcY = Math.round(srcY / 10) * 10;
        destX = Math.round(destX / 10) * 10;
        destY = Math.round(destY / 10) * 10;
        
        let isError = /output--1/.test(output.className); 
        if(isError){
            srcX -= 10;
        }else{
            srcY -= 10;
        }
        
        
        this.drawLineToPoint({ srcX, srcY, destX, destY, output, connection, isError: isError });
    };

    drawLineToPoint({ srcX, srcY, destX, destY, output, connection, color, isError }) {
        if (!this.ioContext) {
            this.ioContext = this.ffFlow.canvas.getContext('2d');
        }

        const context = this.ioContext;

        const path = new Path2D();
        
        path.moveTo(srcX, srcY);
        let linePoints = [[srcX, srcY]];
        let addLinePoint = (_x, _y) => {
            linePoints.push([_x,_y]);
            path.lineTo(_x,_y);
        };
        if(isError ) {
            addLinePoint(srcX + 40, srcY);
            srcX += 40;            
        }

        if (srcY < destY - 20) {
            // draw stepped line
            let mid = destY + (srcY - destY) / 2;
            addLinePoint(srcX, mid);
            addLinePoint(destX, mid);
            addLinePoint(destX, destY);
        }
        else {
            let diff = 0;
            if(isError === false) {
                diff = 20;
                addLinePoint(srcX, srcY + 20);
            }
            let midX = (destX - srcX) / 2
            addLinePoint(srcX + midX, srcY + diff);
            addLinePoint(srcX + midX, destY - 20);
            addLinePoint(destX, destY - 20);
            addLinePoint(destX, destY);
        }

        context.lineWidth = this.lineWidth;
        context.strokeStyle = color || isError ? this.errorColor : this.lineColor;

        if(isError) {
            context.setLineDash(this.errorDash);
            context.stroke(path);
            context.setLineDash([]);
        }else{
            context.stroke(path);
        }

        this.ioLines.push({ path: path, output: output, connection: connection, linePoints: linePoints });

    };

    initCanvas() {
        this.accentColor = this.colorFromCssClass('--accent');
        this.lineColor = this.colorFromCssClass('--color-darkest');
        this.errorColor = '#ff6060'; //this.colorFromCssClass('--errr');
        
        if(this.ffFlow.readOnly)
            return; // dont attach events in read only mode

        // Listen for mouse moves
        let self = this;
        this.ffFlow.canvas.addEventListener('mousedown',  (event) => {
            let ctx = self.ioContext;
            self.ioSelectedConnection = null;
            let clearNode = true;
            let selectedLine = null;
            for (let line of self.ioLines) {
                // Check whether point is inside ellipse's stroke
                if (!selectedLine && self.isClickNearLine(ctx, line.linePoints, event)) 
                    // ctx.isPointInStroke(line.path, event.offsetX, event.offsetY)) {
                { 
                    selectedLine = line;
                    self.ioSelectedConnection = line;
                    let output = line.output.parentNode.parentNode.getAttribute('x-uid');
                    let outputNode = line.output.getAttribute('x-output');
                    this.ffFlow.selectConnection(output, outputNode);
                    clearNode = false;
                    event.stopImmediatePropagation();
                    event.stopPropagation();
                    event.preventDefault();
                }
                else {
                    let isError = /--1$/.test(line.output.id);
                    ctx.strokeStyle = isError ? self.errorColor : self.lineColor;
                    if(isError) {
                        ctx.setLineDash(self.errorDash);
                        ctx.stroke(line.path);
                        ctx.setLineDash([]);
                    }else{
                        ctx.stroke(line.path);                        
                    }
                }
            }
            this.ffFlow.redrawLines();
            if (selectedLine) {
                ctx.strokeStyle = self.accentColor;
                ctx.stroke(selectedLine.path);
            }
            if (clearNode)
                this.ffFlow.selectConnection();
        });
    }

    /**
     * Checks if a mouse event is near a line
     * @param event the mouse event
     * @returns {*|null} the line its over or null
     */
    isOverLine(event) {
        const rect = this.ffFlow.canvas.getBoundingClientRect();
        const xPos = event.xPos ?? Math.round(event.clientX - rect.left);
        const yPos = event.yPos ?? Math.round(event.clientY - rect.top);
        let tolerance = event.tolerance || 30;
        for (let line of this.ioLines) {
            if(this.isClickNearLine(this.ioContext, line.linePoints, { offsetX: xPos, offsetY: yPos}, tolerance)) {
                return line;
            }
        }        
        return null;
    }

    isClickNearLine(ctx, line, event, tolerance= 5) {
        const x = event.offsetX / (this.ffFlow.Zoom / 100);
        const y = event.offsetY / (this.ffFlow.Zoom / 100);

        // Iterate through each pair of points (line segment):
        for (let i = 0; i < line.length - 1; i++) {
            const [x1, y1] = line[i];
            const [x2, y2] = line[i + 1];

            // Calculate the distance from the point to the segment:
            const dx = x2 - x1;
            const dy = y2 - y1;
            const lengthSquared = dx * dx + dy * dy;
            const t = Math.max(0, Math.min(1, ((x - x1) * dx + (y - y1) * dy) / lengthSquared));
            const nearestX = x1 + t * dx;
            const nearestY = y1 + t * dy;
            const distanceSquared = (x - nearestX) * (x - nearestX) + (y - nearestY) * (y - nearestY);

            // Check if the squared distance is within the tolerance squared:
            if (distanceSquared <= tolerance * tolerance) {
                return true; // Click is near this segment of the line
            }
        }

        return false; // Click is not near any segment of the line
    }
    deleteConnection() {
        if (!this.ioSelectedConnection)
            return;

        this.ffFlow.selectConnection();

        let selected = this.ioSelectedConnection;
        let outputNodeUid = selected.output.getAttribute('x-uid');
        
        this.ffFlow.History.perform(new FlowActionConnection(this.ffFlow, outputNodeUid));
    }


    colorFromCssClass(variable) {
        let tmp = document.createElement("div"), color;
        tmp.style.cssText = "position:fixed;left:-100px;top:-100px;width:1px;height:1px";
        if (variable.startsWith('--'))
            tmp.style.color = "var(" + variable + ")";
        else
            tmp.style.color = variable;
        document.body.appendChild(tmp);  // required in some browsers
        color = getComputedStyle(tmp).getPropertyValue("color");
        document.body.removeChild(tmp);
        return color;
    }

    redrawGrid(canvas) {
        const ctx = this.ioContext;
        const gridSize = 10; // Spacing between grid lines


        // Draw vertical grid lines
        for (let x = 0; x <= canvas.width; x += gridSize) {
            ctx.beginPath();
            ctx.moveTo(x, 0);
            ctx.lineTo(x, canvas.height);
            ctx.strokeStyle = x % 50 === 0 ? '#282828' : '#202020'; // Grid line color
            ctx.stroke();
        }

        // Draw horizontal grid lines
        for (let y = 0; y <= canvas.height; y += gridSize) {
            ctx.beginPath();
            ctx.moveTo(0, y);
            ctx.lineTo(canvas.width, y);
            ctx.strokeStyle = y % 50 === 0 ? '#282828' : '#202020'; // Grid line color
            ctx.stroke();
        }
    }
}