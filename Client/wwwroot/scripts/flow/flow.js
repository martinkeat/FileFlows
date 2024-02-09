
window.createffFlow = function(csharp, uid) {
    let div = document.createElement('div');
    div.className = 'flow-parts';
    div.setAttribute('id', `flow-parts-${uid}`);
    div.innerHTML = '<canvas width="8000" height="4000" tabindex="1" oncontextmenu="return false"></canvas>';    
    document.querySelector('.flows-tabs-contents').appendChild(div);
    return new ffFlow(csharp, div, uid);
};
class ffFlow 
{
    constructor(csharp, eleFlowParts, uid)
    {
        this.csharp= csharp;
        this.eleFlowParts= eleFlowParts;
        this.uid = uid;
        this.canvas = eleFlowParts.querySelector('canvas');
        this.active= false;
        this.parts= [];
        this.elements= [];
        this.SelectedParts= [];
        this.SingleOutputConnection= true;
        this.Vertical= true;
        this.lblDelete= 'Delete';
        this.lblNode= 'Node';
        this.Zoom=100;
        this.infobox = null;
        this.infoboxSpan = null;
        this.infoSelectedType = '';
        this.FlowLines = new ffFlowLines(this);
        this.ffFlowPart = new ffFlowPart(this);
        this.Mouse = new ffFlowMouse(this);
        this.History= new ffFlowHistory(this);
    }
    
    dispose() {
        this.eleFlowParts.remove();
    }
    
    focusName(){
        setTimeout(() =>{
            let txtName = document.getElementById(`flow-${this.uid}-name`);
            if(txtName)
                txtName.focus();
        }, 50);
    }

    setVisibility(show){
        this.eleFlowParts.classList.remove('show')
        if(show) {
            this.eleFlowParts.classList.add('show');
            this.redrawLines();
        }
    }

    reset() {
        this.active = false;
        this.ffFlowPart.reset();
        this.FlowLines.reset();
        this.Mouse.reset();
    }

    markDirty() {
        this.csharp.invokeMethodAsync("MarkDirty");
    }

    zoom(percent) {
        this.Zoom = percent;
        this.eleFlowParts.style.zoom = percent / 100;
    }

    unSelect() {
        this.SelectedParts = [];
        this.ffFlowPart.unselectAll();
    }
    
    init(parts, elements) {
        this.parts = parts;
        this.elements = elements;
        this.infobox = null;        
        
        this.csharp.invokeMethodAsync("Translate", `Labels.Delete`, null).then(result => {
            this.lblDelete = result;
        });

        this.csharp.invokeMethodAsync("Translate", `Labels.Node`, null).then(result => {
            this.lblNode = result;
        });
        
        console.log('this.eleFlowParts', this.eleFlowParts);
        let mc = new Hammer.Manager(this.eleFlowParts);
        let pinch = new Hammer.Pinch();
        let press = new Hammer.Press({
            time: 1000,
            pointers: 2,
            threshold: 10
        });
        mc.add([pinch, press]);
        mc.on("pinchin", (ev) => {
            this.zoom(Math.min(100, this.Zoom + 1));            
        });
        mc.on("pinchout", (ev) => {
            this.zoom(Math.max(50, this.Zoom - 1));
        });
        mc.on('press', (ev) => {
            ev.preventDefault();
            let eleShowElements = document.getElementById('show-elements');
            if(eleShowElements)
                eleShowElements.click();
        });
        mc.on('touch', (ev) => {
            ev.preventDefault();
        })

        this.eleFlowParts.addEventListener("keydown", (e) => this.onKeyDown(e), false);
        // this.eleFlowParts.addEventListener("touchstart", (e) => this.Mouse.dragStart(e), false);
        // this.eleFlowParts.addEventListener("touchend", (e) => this.Mouse.dragEnd(e), false);
        // this.eleFlowParts.addEventListener("touchmove", (e) => this.Mouse.drag(e), false);
        this.eleFlowParts.addEventListener("mousedown", (e) => e.button === 0 && this.Mouse.dragStart(e), false);
        this.eleFlowParts.addEventListener("mouseup", (e) => this.Mouse.dragEnd(e), false);
        this.eleFlowParts.addEventListener("mousemove", (e) => this.Mouse.drag(e), false);

        this.eleFlowParts.addEventListener("mouseup", (e) => this.FlowLines.ioMouseUp(e), false);
        this.eleFlowParts.addEventListener("mousemove", (e) => this.FlowLines.ioMouseMove(e), false);
        this.eleFlowParts.addEventListener("click", (e) => { this.unSelect() }, false);
        this.eleFlowParts.addEventListener("dragover", (e) => { this.drop(e, false) }, false);
        this.eleFlowParts.addEventListener("drop", (e) => { this.drop(e, true) }, false);

        this.eleFlowParts.addEventListener("contextmenu", (e) => { e.preventDefault(); e.stopPropagation(); this.contextMenu(e); return false; }, false);


        // bind these to this so the `this` in these methods are this object and not document
        this.CopyEventListener = this.CopyEventListener.bind(this);
        this.PasteEventListener = this.PasteEventListener.bind(this);
        
        document.removeEventListener('copy', this.CopyEventListener);
        document.addEventListener('copy', this.CopyEventListener);
        document.removeEventListener('paste', this.PasteEventListener);
        document.addEventListener('paste', this.PasteEventListener);


        let width = this.Vertical ? (document.body.clientWidth * 1.5) : window.screen.availWidth
        let height = this.Vertical ? (document.body.clientHeight * 2) : window.screen.availHeight;

        this.canvas.height = height;
        this.canvas.width = width;
        this.canvas.style.width = this.canvas.width + 'px';
        this.canvas.style.height = this.canvas.height + 'px';

        for (let p of parts) {
            try {
                this.ffFlowPart.addFlowPart(p);
            } catch (err) {
                if(p != null && p.name)
                    console.error(`Error adding flow part '${p.name}: ${err}`);
                else
                    console.error(`Error adding flow part: ${err}`);
            }
        }

        this.redrawLines();
    }
    
    redrawLines() {
        this.FlowLines.redrawLines();
    }
    
    contextMenu(event, part){
        if(part){
            event.stopPropagation();
            event.stopImmediatePropagation();
            event.preventDefault();
        }                
        this.csharp.invokeMethodAsync("OpenContextMenu", {
            x: event.clientX,
            y: event.clientY,
            parts: this.SelectedParts
        });
        return false;
    }

    ioInitConnections(connections) {
        this.reset();
        for (let k in connections) { // iterating keys so use in
            for (let con of connections[k]) { // iterating values so use of
                let id = k + '-output-' + con.output;
                
                let list = this.FlowLines.ioOutputConnections.get(id);
                if (!list) {
                    this.FlowLines.ioOutputConnections.set(id, []);
                    list = this.FlowLines.ioOutputConnections.get(id);
                }
                list.push({ index: con.input, part: con.inputNode });
            }
        }
    }

    /*
     * Called from C# code to insert a new element to the flow
     */
    insertElement(uid) {
        console.log('insertElement', uid);
        this.drop(null, true, uid);
    }

    drop(event, dropping, uid) {
        let xPos = 100, yPos = 100;
        if (event) {
            event.preventDefault();
            if (dropping !== true)
                return;
            let bounds = event.target.getBoundingClientRect();

            xPos = this.translateCoord(event.clientX) - bounds.left - 20;
            yPos = this.translateCoord(event.clientY) - bounds.top - 20;
        } else {
        }
        if (!uid) {
            console.log('this.Mouse.draggingElementUid', this.Mouse.draggingElementUid);
            uid = this.Mouse.draggingElementUid;
        }
        console.log('drop', uid);
        this.addElementActual(uid, xPos, yPos);
    }
    
    addElementActual(uid, xPos, yPos) {

        this.csharp.invokeMethodAsync("AddElement", uid).then(result => {
            if(!result)
                return; // can happen if adding a obsolete node and user declines it
            let element = result.element;
            if (!element) {
                console.warn('element was null');
                return;
            }
            let part = {
                name: '', // new part, dont set a name
                label: element.name,
                flowElementUid: element.uid,
                type: element.type,
                xPos: xPos - 30,
                yPos: yPos,
                inputs: element.model.Inputs ? element.model.Inputs : element.inputs,
                outputs: element.model.Outputs ? element.model.Outputs : element.outputs,
                uid: result.uid,
                icon: element.icon,
                model: element.model
            };

            if (part.model?.outputs)
                part.Outputs = part.model?.outputs;

            this.History.perform(new FlowActionAddNode(part));

            if (element.noEditorOnAdd === true)
                return;

            if (element.model && Object.keys(element.model).length > 0)
            {
                this.ffFlowPart.editFlowPart(part.uid, true);
            }
        }); 
    }

    translateCoord(value, lines) {
        if (lines !== true)
            value = Math.floor(value / 10) * 10;
        let zoom = this.Zoom / 100;
        if (!zoom || zoom === 1)
            return value;
        return value / zoom;
    }

    getModel() {
        let connections = this.FlowLines.ioOutputConnections;
        
        // remove existing error Connections 
        this.parts.forEach(x => x.errorConnection = null);

        let connectionUids = [];
        for (let [outputPart, con] of connections) {
            connectionUids.push(outputPart);
            let partId = outputPart.substring(0, outputPart.indexOf('-output'));
            let outputStr = /[\-]{1,2}[\d]+$/.exec(outputPart);
            if(!outputStr)
                continue;
            outputStr = outputStr[0].substring(1);
            let output = parseInt(outputStr, 10);
            let part = this.parts.filter(x => x.uid === partId)[0];
            if (!part) {
                console.warn('unable to find part: ', partId);
                continue;
            }
            for (let inputCon of con) {
                let input = inputCon.index;
                let toPart = inputCon.part;
                if (!part.outputConnections)
                    part.outputConnections = [];

                if (this.SingleOutputConnection) {
                    // remove any duplicates from the output
                    part.outputConnections = part.outputConnections.filter(x => x.output != output);
                }                
                
                if(output === -1)
                {
                    part.errorConnection = 
                    {
                        input: input,
                        output: output,
                        inputNode: toPart
                    };
                }
                else {
                    part.outputConnections.push(
                    {
                        input: input,
                        output: output,
                        inputNode: toPart
                    });
                }
            }
        }
        // remove any no longer existing connections
        for (let part of this.parts) {
            if (!part.outputConnections)
                continue;
            for (let i = part.outputConnections.length - 1; i >= 0;i--) {
                let po = part.outputConnections[i];
                let outUid = part.uid + '-output-' + po.output;
                if (connectionUids.indexOf(outUid) < 0) {
                    // need to remove it
                    part.outputConnections.splice(i, 1);
                }
            }
        }

        // update the part positions
        for (let p of this.parts) {
            var div = document.getElementById(p.uid);
            if (!div)
                continue;
            p.xPos = parseInt(div.style.left, 10);
            p.yPos = parseInt(div.style.top, 10);
        }

        return this.parts;
    }

    getElement(uid) {
        return this.elements.filter(x => x.uid == uid)[0];
    }

    getPart(partUid) {
        return this.parts.filter(x => x.uid == partUid)[0];
    };

    setInfo(message, type) {
        if (!message) {
            if (!this.infobox)
                return;
            this.infobox.style.display = 'none';
        } else {
            this.infoSelectedType = type;
            if (!this.infobox) {
                let box = document.createElement('div');
                box.classList.add('info-box');

                // remove button
                let remove = document.createElement('span');
                remove.classList.add('fas');
                remove.classList.add('fa-trash');
                remove.style.cursor = 'pointer';
                remove.setAttribute('title', this.lblDelete);
                remove.addEventListener("click", (e) => {
                    if (this.infoSelectedType === 'Connection')
                        this.FlowLines.deleteConnection();
                    else if (this.infoSelectedType === 'Node') {
                        if (this.SelectedParts?.length) {
                            for(let p of this.SelectedParts)
                                this.ffFlowPart.deleteFlowPart(p.uid);
                        }
                    }
                }, false);
                box.appendChild(remove);


                this.infoboxSpan = document.createElement('span');
                box.appendChild(this.infoboxSpan);


                this.eleFlowParts.appendChild(box);
                this.infobox = box;
            }
            this.infobox.style.display = '';
            this.infoboxSpan.innerText = message;
        }
    }

    selectConnection(outputNode, output) {
        
        if (!outputNode) {
            this.setInfo();
            return;
        }
        
        if(this.SelectedParts?.length) {
            this.unSelect();
            this.redrawLines();
            
            // this is un-focuses a node so if the user presses delete, that node is not deleted
            this.canvas.focus();
        }

        let part = this.getPart(outputNode);
        if (!part) {
            this.setInfo();
            return;
        }

        if (!part.OutputLabels) {
            console.log('output labels null');
            return;
        }
        if (part.OutputLabels.length <= output) {
            console.log('output labels length less than output', output, part.OutputLabels);
            return;
        }
        this.setInfo(part.OutputLabels[output], 'Connection');
    }

    selectNode(part) {
        if (!part) {
            this.setInfo();
            return;
        }
        this.SelectedParts = [part];
        
        var ele = document.getElementById(part.uid);
        if(ele)
        {
            ele.classList.remove('selected');
            ele.classList.add('selected');
        }

        if (!part.displayDescription) {
            let element = this.getElement(part.flowElementUid);
            if (!element)
                return;
            this.csharp.invokeMethodAsync("Translate", `Flow.Parts.${element.name}.Description`, part.model).then(result => {
                //part.displayDescription = this.lblNode + ': ' + (result === 'Description' || !result ? part.displayName : result);
                part.displayDescription = (result === 'Description' || !result ? part.displayName : result);
                this.setInfo(part.displayDescription, 'Node');
            });
        } else {
            this.setInfo(part.displayDescription, 'Node');
        }
    }
    
    setOutputHint(part, output) {
        let element = this.getElement(part.flowElementUid);
        if (!element) {
            console.error("Failed to find element: " + part.flowElementUid);
            return;
        }
        if(output === -1){
            let outputNode = document.getElementById(part.uid + '-output-' + output);
            if (outputNode)
                outputNode.setAttribute('title', 'FAILED');
            return;
        }
        if(part.flowElementUid.startsWith('Script:') || part.flowElementUid.startsWith('SubFlow:'))
        {
            part.OutputLabels = {};
            for(let i=0; i<element.outputLabels.length;i++)
            {
                part.OutputLabels[(i + 1)] = 'Output ' + (i + 1) + ': ' + element.outputLabels[i];
                let outputNode = document.getElementById(part.uid + '-output-' + (i + 1));
                if (outputNode)
                    outputNode.setAttribute('title', part.OutputLabels[(i + 1)]);                 
            }
        }
        else 
        {
            this.csharp.invokeMethodAsync("Translate", `Flow.Parts.${element.name}.Outputs.${output}`, part.model).then(result => {
                if (!part.OutputLabels) part.OutputLabels = {};
                part.OutputLabels[output] = result;
                let outputNode = document.getElementById(part.uid + '-output-' + output);
                if (outputNode)
                    outputNode.setAttribute('title', result);
            });
        }
    }
    
    initOutputHints(part) {
        if (!part || !part.outputs)
            return;
        for (let i = 0; i <= part.outputs; i++) {
            this.setOutputHint(part, i === 0 ? -1 : i);
        }
    }
    
    onKeyDown(event) {
        if (event.code === 'Delete' || event.code === 'Backspace') {
            for(let part of this.SelectedParts || []) {
                this.ffFlowPart.deleteFlowPart(part.uid);
            }
            event.stopImmediatePropagation();
            event.preventDefault();
        }
    }
    
    CopyEventListener(e){
        let eleFlowParts = document.getElementById('flow-parts');
        if(!eleFlowParts)
            return; // not on flow page, dont consume copy

        let active = document.activeElement;
        if(active) {
            let flowParts = active.closest('.flow-parts');
            if (!flowParts)
                return; // flowparts/canvas does not have focus, do not listen to this event
        }
        if (this.SelectedParts?.length) {
            let json = JSON.stringify(this.SelectedParts);
            e.clipboardData.setData('text/plain', json);
            
        }
        e?.preventDefault();
    }

    async PasteEventListener(e, json) {
        if(!json) {
            json = (e?.clipboardData || window.clipboardData)?.getData('text');
        }
        if(!json)
            return;
        e?.preventDefault();
        let parts = [];
        try {
            parts = JSON.parse(json);
        }catch(err) { return; }
        for(let p of parts){
            if(!p.uid)
                return; // not a valid item pasted in
            p.uid = await this.csharp.invokeMethodAsync("NewGuid");
            // for now we dont copy connections
            p.outputConnections = null;
            p.xPos += 120;
            p.yPos += 80;
            if(p.Name)
                p.Name = "Copy of " + p.Name;
            this.History.perform(new FlowActionAddNode(p));
        }
    }
        
    contextMenu_Edit(part){
        if(!part)
            return;        
        this.setInfo(part.Name, 'Node');
        this.ffFlowPart.editFlowPart(part.uid);
    }

    contextMenu_EditSubFlow(part){
        if(!part)
            return;
        let currentUrl = window.location.href;
        let baseUrl = currentUrl.substring(0, currentUrl.lastIndexOf('/'));
        console.log('part', part);
        let newUrl = baseUrl + '/' + part.flowElementUid.substring(8);
        window.open(newUrl, '_blank');
    }
    
    contextMenu_Copy(parts) {
        let json = JSON.stringify(parts);
        navigator.clipboard.writeText(json);
    }
    contextMenu_Paste() {
        navigator.clipboard.readText().then(json => {
            this.PasteEventListener(null, json);            
        });
    }
    contextMenu_Delete(parts) {
        for(let part of parts || []) {
            this.ffFlowPart.deleteFlowPart(part.uid);
        }
    }
    contextMenu_Add() {
        let ele = document.getElementById('show-elements')
        if(ele)
            ele.click();
    }
}