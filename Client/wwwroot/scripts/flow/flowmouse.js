class ffFlowMouse {
    
    constructor(ffFlow) {
        this.ffFlow = ffFlow;
        this.dragItem = null;
        this.currentX = 0;
        this.currentY = 0;
        this.initialX = 0;
        this.initialY = 0;
        this.xOffset = 0;
        this.yOffset = 0;
        this.draggingElementUid = null;
        this.canvasSelecting = false;
    }

    reset() {
        this.currentX = 0;
        this.currentY = 0;
        this.initialX = 0;
        this.initialY = 0;
        this.xOffset = 0;
        this.yOffset = 0;
        this.dragItem = null;
        this.draggingElementUid = null;
    }

    dragElementStart(uid) {
        this.draggingElementUid = uid;
        event.dataTransfer.setData("text", uid);
        event.dataTransfer.effectAllowed = "copy";
    }

    dragStart(e) {
        if (e.type === "touchstart") {
            this.initialX = e.touches[0].clientX - this.xOffset;
            this.initialY = e.touches[0].clientY - this.yOffset;
        } else {
            this.initialX = e.clientX;// - this.xOffset;
            this.initialY = e.clientY;// - this.yOffset;
        }

        if (e.target.classList.contains('draggable') === true) {
            var part = this.ffFlow.parts.find(x => x.uid === e.target.parentNode.getAttribute('x-uid'));
            let selected = this.ffFlow.SelectedParts.indexOf(part) >= 0;
            if (selected !== true)
            {
                this.ffFlow.ffFlowPart.unselectAll();
                this.ffFlow.selectNode(part);
            }
            this.currentX = 0;
            this.currentY = 0;
            this.dragItem = e.target.parentNode;
            this.ffFlow.active = true;
            this.canvasSelecting = false;
        }
    }

    dragEnd(e) {
        if (this.ffFlow.active && this.dragItem) {
            this.initialX = this.currentX;
            this.initialY = this.currentY;
            let selectedItems = this.ffFlow.eleFlowParts.querySelectorAll('.flow-part.selected');
            let single = selectedItems.length === 1;
            
            for(let part of selectedItems) {
                part.style.transform = '';
                let originalXPos = parseInt(part.style.left, 10);
                let originalYPos = parseInt(part.style.top, 10);
                let transCurX = this.ffFlow.translateCoord(this.currentX);
                let transCurY = this.ffFlow.translateCoord(this.currentY);
                let partLeft = parseInt(part.style.left, 10);
                let partTop = parseInt(part.style.top, 10);
                let xPos = partLeft + transCurX;
                let yPos = partTop + transCurY;
                if(xPos !== originalXPos || yPos !== originalYPos) 
                {
                    let canIntersect = single && !part.querySelector('.connected') && 
                        part.querySelector('.input-1') && part.querySelector('.output-1');
                    
                    this.ffFlow.History.perform(new FlowActionMove(part, xPos, yPos, originalXPos, originalYPos));

                    if(canIntersect)
                    {
                        // get center of the part div
                        const rectPart = part.getBoundingClientRect();
                        const rectParent = part.parentNode.getBoundingClientRect();
                        const centerX = Math.round(xPos + rectPart.width / 2);
                        const centerY = Math.round(yPos + rectPart.height / 2);
                        let line = this.ffFlow.FlowLines.isOverLine({ xPos: centerX, yPos: centerY, tolerance: 50});
                        let uid = part.getAttribute('x-uid');
                        let p = this.ffFlow.parts.filter(x => x.uid === uid);
                        console.log('p', p);
                        if(line && p)
                            this.ffFlow.History.perform(new FlowActionIntersectLine(p[0], line, true));
                            
                    }
                }
            }
            //this.ffFlow.redrawLines();
        }
        else if(this.canvasSelecting){
            let endX = e.x;
            let endY = e.y;
            let selectedBounds = {
                x: this.ffFlow.translateCoord(Math.min(this.initialX, this.initialX + this.currentX)),
                y: this.ffFlow.translateCoord(Math.min(this.initialY, this.initialY + this.currentY)),
                width: this.ffFlow.translateCoord(Math.abs(this.currentX)),
                height: this.ffFlow.translateCoord(Math.abs(this.currentY))
            };            
            // set this in a timeout, this fixes an issue with the mouse click event clearing our selection
            setTimeout(()=>{
                    
                this.ffFlow.ffFlowPart.unselectAll();   
                // select all nodes in this area
                let selected = [];
                
                if(Math.abs(selectedBounds.width + selectedBounds.height) > 10) {

                    for (let p of this.ffFlow.parts) {
                        let ele = this.ffFlow.getFlowPart(p.uid);
                        if (!ele)
                            continue;
                        let eleBounds = ele.getBoundingClientRect();

                        let inbounds = ((selectedBounds.x + selectedBounds.width) >= eleBounds.left)
                            && (selectedBounds.x <= (eleBounds.left + eleBounds.width))
                            && ((selectedBounds.y + selectedBounds.height) >= eleBounds.top)
                            && (selectedBounds.y <= (eleBounds.top + eleBounds.height));
                        if (inbounds) {
                            selected.push(p);
                            ele.classList.add('selected');
                        }
                    }
                }
                this.ffFlow.SelectedParts = selected;
                this.canvasSelecting = false;
                this.ffFlow.redrawLines();
            });
        }
        this.canvasSelecting = false;
        this.ffFlow.active = false;
    }

    drag(e) {
        if (!this.ffFlow.active && !this.canvasSelecting)
            return;

        e.preventDefault();

        if (e.type === "touchmove") {
            this.currentX = e.touches[0].clientX - this.initialX;
            this.currentY = e.touches[0].clientY - this.initialY;
        } else {
            this.currentX = e.clientX - this.initialX;
            this.currentY = e.clientY - this.initialY;
        }


        this.xOffset = this.currentX;
        this.yOffset = this.currentY;
        if(this.ffFlow.active) 
        {
            for(let part of this.ffFlow.eleFlowParts.querySelectorAll('.flow-part.selected')) 
            {
                this.setTranslate(this.currentX, this.currentY, part);
            }
        }
        this.ffFlow.redrawLines();
    }

    setTranslate(xPos, yPos, el) {
        xPos = this.ffFlow.translateCoord(xPos);
        yPos = this.ffFlow.translateCoord(yPos);
        el.style.transform = "translate3d(" + xPos + "px, " + yPos + "px, 0)";
    }
}