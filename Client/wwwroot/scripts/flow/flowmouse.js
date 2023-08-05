class ffFlowMouse {
    dragItem = null;
    currentX = 0;
    currentY = 0;
    initialX = 0;
    initialY = 0;
    xOffset = 0;
    yOffset = 0;
    draggingElementUid = null;
    canvasSelecting = false;

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

    dragElementStart(event) {
        this.draggingElementUid = event.target.id;
        event.dataTransfer.setData("text", event.target.id);
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
            var part = ffFlow.parts.find(x => x.uid === e.target.parentNode.id);
            let selected = ffFlow.SelectedParts.indexOf(part) >= 0;
            if (selected !== true)
            {
                ffFlowPart.unselectAll();
                ffFlow.selectNode(part);
            }
            this.currentX = 0;
            this.currentY = 0;
            this.dragItem = e.target.parentNode;
            ffFlow.active = true;
            this.canvasSelecting = false;
        }
        else if(e.target.tagName == 'CANVAS'){
            this.canvasSelecting = true;
        }else {
            this.canvasSelecting = false;
        }
    }

    dragEnd(e) {
        if (ffFlow.active && this.dragItem) {
            this.initialX = this.currentX;
            this.initialY = this.currentY;
            for(let part of document.querySelectorAll('.flow-part.selected')) {
                part.style.transform = '';
                let originalXPos = parseInt(part.style.left, 10);
                let originalYPos = parseInt(part.style.top, 10);
                let transCurX = ffFlow.translateCoord(this.currentX);
                let transCurY = ffFlow.translateCoord(this.currentY);
                let partLeft = parseInt(part.style.left, 10);
                let partTop = parseInt(part.style.top, 10);
                let xPos = partLeft + transCurX;
                let yPos = partTop + transCurY;
                if(xPos != originalXPos || yPos != originalYPos)
                    ffFlow.History.perform(new FlowActionMove(part, xPos, yPos, originalXPos, originalYPos));
            }
            //ffFlow.redrawLines();
        }
        else if(this.canvasSelecting){
            let endX = e.x;
            let endY = e.y;
            let selectedBounds = {
                x: ffFlow.translateCoord(Math.min(this.initialX, this.initialX + this.currentX)),
                y: ffFlow.translateCoord(Math.min(this.initialY, this.initialY + this.currentY)),
                width: ffFlow.translateCoord(Math.abs(this.currentX)),
                height: ffFlow.translateCoord(Math.abs(this.currentY))
            };            
            // set this in a timeout, this fixes an issue with the mouse click event clearing our selection
            setTimeout(()=>{
                    
                ffFlowPart.unselectAll();   
                // select all nodes in this area
                let selected = [];
                
                if(Math.abs(selectedBounds.width + selectedBounds.height) > 10) {

                    for (let p of window.ffFlow.parts) {
                        var ele = document.getElementById(p.uid);
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
                window.ffFlow.SelectedParts = selected;
                this.canvasSelecting = false;
                ffFlow.redrawLines();
            });
        }
        this.canvasSelecting = false;
        ffFlow.active = false;
    }

    drag(e) {
        if (ffFlow.active || this.canvasSelecting) {

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
            if(ffFlow.active) 
            {
                for(let part of document.querySelectorAll('.flow-part.selected')) 
                {
                    this.setTranslate(this.currentX, this.currentY, part);
                }
            }
            ffFlow.redrawLines();
        }
    }

    setTranslate(xPos, yPos, el) {
        xPos = ffFlow.translateCoord(xPos);
        yPos = ffFlow.translateCoord(yPos);
        el.style.transform = "translate3d(" + xPos + "px, " + yPos + "px, 0)";
    }
}