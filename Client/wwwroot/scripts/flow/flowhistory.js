class ffFlowHistory {
    
    constructor(ffFlow)
    {
        this.ffFlow = ffFlow;
        this.history = [];
        this.redoActions = [];        
    }
    
    perform(action){
        console.log('history perform', action);
        this.ffFlow.markDirty();
        // doing new action, anything past the current point we will clear
        this.redoActions = [];
        this.history.push(action);
        action.perform(this.ffFlow);        
    }
    
    redo() {
        if(this.redoActions.length === 0)
            return; // nothing to redo
        let action = this.redoActions.splice(this.redoActions.length - 1, 1)[0];
        console.log('history redo', action);
        this.history.push(action);
        action.perform(this.ffFlow);
    }
    
    undo(){
        if(this.history.length < 1) {
            return;
        }
        let action = this.history.pop();
        console.log('history undo', action);
        this.redoActions.push(action);
        action.undo(this.ffFlow);
    }    
}

class FlowActionMove {
    elementId;
    xPos;
    yPos;
    originalXPos;
    originalYPos;
    
    constructor(element, xPos, yPos, originalXPos, originalYPos) {
        // store the Id of the element, and not the actual element
        // in case the element is deleted then restored
        this.elementId = element.getAttribute('x-uid');
        this.xPos = xPos;
        this.yPos = yPos;
        this.originalXPos = originalXPos;
        this.originalYPos = originalYPos;        
    }
    
    perform(ffFlow) {
        this.moveTo(ffFlow, this.xPos, this.yPos);
    }
    
    undo(ffFlow){
        this.moveTo(ffFlow, this.originalXPos, this.originalYPos);
    }
    
    moveTo(ffFlow, x, y){
        let element = ffFlow.getFlowPart(this.elementId);
        if(!element)
            return;
        element.style.transform = '';
        element.style.left = x + 'px';
        element.style.top = y + 'px'

        ffFlow.redrawLines();
    }
}

class FlowActionDelete {

    html;
    parent;
    uid;
    ioOutputConnections;
    ffFlowPart;
    
    constructor(ffFlow, uid) {
        this.uid = uid;
        let element = ffFlow.getFlowPart(uid);
        this.parent = element.parentNode;
        this.html = element.outerHTML;
        this.ioOutputConnections = ffFlow.FlowLines.ioOutputConnections[this.uid];
        
        for (let i = 0; i < ffFlow.parts.length; i++) {
            if (ffFlow.parts[i].uid === this.uid) {
                this.ffFlowPart = ffFlow.parts[i];
            }
        }
    }

    perform(ffFlow) {
        let div = ffFlow.getFlowPart(this.uid);
        if (div) {
            ffFlow.ffFlowPart.flowPartElements = ffFlow.ffFlowPart.flowPartElements.filter(x => x !== div);
            div.remove();
        }

        ffFlow.FlowLines.ioOutputConnections.delete(this.uid);

        for (let i = 0; i < ffFlow.parts.length; i++) {
            if (ffFlow.parts[i].uid === this.uid) {
                ffFlow.parts.splice(i, 1);
                break;
            }
        }
        
        ffFlow.setInfo();
        ffFlow.redrawLines();
    }

    undo(ffFlow){        
        if(this.ffFlowPart)
            ffFlow.parts.push(this.ffFlowPart);
        
        // create the element again
        let div = document.createElement('div');
        div.innerHTML = this.html;
        let newPart = div.firstChild;
        newPart.classList.remove('selected');
        this.parent.appendChild(newPart);
        div.remove();
        ffFlow.ffFlowPart.flowPartElements.push(newPart);
        ffFlow.ffFlowPart.attachEventListeners({part: this.ffFlowPart, allEvents: true});

        // recreate the connections
        ffFlow.FlowLines.ioOutputConnections[this.uid] = this.ioOutputConnections;

        ffFlow.redrawLines();
    }
}

class FlowActionConnection {

    outputNodeUid;
    previousConnection;
    connection;

    constructor(ffFlow, outputNodeUid, connection) {
        this.outputNodeUid = outputNodeUid;
        this.connection = connection;
        this.previousConnection = ffFlow.FlowLines.ioOutputConnections.get(this.outputNodeUid);
    }

    perform(ffFlow) {
        this.connect(ffFlow, this.connection);
    }

    undo(ffFlow){
        this.connect(ffFlow, this.previousConnection);
    }
    
    connect(ffFlow, connection){
        if(connection)
            ffFlow.FlowLines.ioOutputConnections.set(this.outputNodeUid, connection);
        else
            ffFlow.FlowLines.ioOutputConnections.delete(this.outputNodeUid);
        ffFlow.redrawLines();        
    }
}


class FlowActionAddPart {

    part;
    
    constructor(part) {
        this.part = part;
    }

    perform(ffFlow) {
        ffFlow.ffFlowPart.addFlowPart(this.part);
        ffFlow.parts.push(this.part);
    }

    undo(ffFlow)
    {
        let div = ffFlow.getFlowPart(this.part.uid);
        if (div) {
            ffFlow.ffFlowPart.flowPartElements = ffFlow.ffFlowPart.flowPartElements.filter(x => x !== div);
            div.remove();
        }

        ffFlow.FlowLines.ioOutputConnections.delete(this.part.uid);

        for (let i = 0; i < ffFlow.parts.length; i++) {
            if (ffFlow.parts[i].uid === this.part.uid) {
                ffFlow.parts.splice(i, 1);
                break;
            }
        }

        ffFlow.setInfo();
        ffFlow.redrawLines();
    }
}


class FlowActionReplacePart {

    part;
    replacing;

    constructor(part, replacing) {
        this.part = part;
        this.replacing = replacing;
    }

    perform(ffFlow) {
        this.swap(ffFlow, this.replacing, this.part);
    }

    undo(ffFlow)
    {
        this.swap(ffFlow, this.part, this.replacing);
    }
    
    swap(ffFlow, oldPart, newPart)
    {
        let index = ffFlow.parts.indexOf(oldPart);
        if(index < 0)
            return;
        console.log('newPart', newPart);
        console.log('oldPart', oldPart);
        newPart.uid = oldPart.uid; // so the input connections stay the same
        newPart.xPos = oldPart.xPos;
        newPart.yPos = oldPart.yPos;
        if(oldPart.outputConnections) {
            this.part.outputConnections = [];
            for (let oc of oldPart.outputConnections) {
                if(this.part.outputs > oc.output){
                    this.part.outputConnections.push(oc);
                }
            }
        }

        // have to remove the original before we can add the new one so the correct flow part is found
        let div = ffFlow.getFlowPart(oldPart.uid);
        if (div) {
            ffFlow.ffFlowPart.flowPartElements = ffFlow.ffFlowPart.flowPartElements.filter(x => x !== div);
            div.remove();
        }

        ffFlow.ffFlowPart.addFlowPart(newPart);
        ffFlow.parts[index] = newPart;

        ffFlow.setInfo();
        ffFlow.redrawLines();
    }
}