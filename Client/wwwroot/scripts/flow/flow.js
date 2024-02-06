window.ffFlow = {
    active: false,
    csharp: null,
    parts: [],
    elements: [],
    FlowLines: new ffFlowLines(),
    Mouse: new ffFlowMouse(),
    SelectedParts: [],
    SingleOutputConnection: true,
    Vertical: true,
    lblDelete: 'Delete',
    lblNode: 'Node',
    Zoom:100,
    History: new ffFlowHistory(),

    reset: function () {
        ffFlow.active = false;
        ffFlowPart.reset();
        this.FlowLines.reset();
        this.Mouse.reset();
    },

    eleFlowParts: null,
    zoom: function (percent) {
        if (ffFlow.eleFlowParts == null) {
            ffFlow.eleFlowParts = document.querySelector('.flow-parts');
        }
        ffFlow.Zoom = percent;
        ffFlow.eleFlowParts.style.zoom = percent / 100;
    },

    unSelect: function () {
        ffFlow.SelectedParts = [];
        ffFlowPart.unselectAll();
    },

    init: function (container, csharp, parts, elements) {
        ffFlow.csharp = csharp;
        ffFlow.parts = parts;
        ffFlow.elements = elements;
        ffFlow.infobox = null;        
        
        ffFlow.csharp.invokeMethodAsync("Translate", `Labels.Delete`, null).then(result => {
            ffFlow.lblDelete = result;
        });

        ffFlow.csharp.invokeMethodAsync("Translate", `Labels.Node`, null).then(result => {
            ffFlow.lblNode = result;
        });

        if (typeof (container) === 'string') {
            let c = document.getElementById(container);
            if (!c)
                c = document.querySelector(container);
            if (!c) {
                console.warn("Failed to locate container:", container);
                return;
            }
            container = c;
        }
        
        var mc = new Hammer.Manager(container);
        var pinch = new Hammer.Pinch();
        var press = new Hammer.Press({
            time: 1000,
            pointers: 2,
            threshold: 10
        });
        mc.add([pinch, press]);
        mc.on("pinchin", (ev) => {
            ffFlow.zoom(Math.min(100, ffFlow.Zoom + 1));            
        });
        mc.on("pinchout", (ev) => {
            ffFlow.zoom(Math.max(50, ffFlow.Zoom - 1));
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

        container.addEventListener("keydown", (e) => ffFlow.onKeyDown(e), false);
        // container.addEventListener("touchstart", (e) => ffFlow.Mouse.dragStart(e), false);
        // container.addEventListener("touchend", (e) => ffFlow.Mouse.dragEnd(e), false);
        // container.addEventListener("touchmove", (e) => ffFlow.Mouse.drag(e), false);
        container.addEventListener("mousedown", (e) => e.button === 0 && ffFlow.Mouse.dragStart(e), false);
        container.addEventListener("mouseup", (e) => ffFlow.Mouse.dragEnd(e), false);
        container.addEventListener("mousemove", (e) => ffFlow.Mouse.drag(e), false);

        container.addEventListener("mouseup", (e) => ffFlow.FlowLines.ioMouseUp(e), false);
        container.addEventListener("mousemove", (e) => ffFlow.FlowLines.ioMouseMove(e), false);
        container.addEventListener("click", (e) => { ffFlow.unSelect() }, false);
        container.addEventListener("dragover", (e) => { ffFlow.drop(e, false) }, false);
        container.addEventListener("drop", (e) => { ffFlow.drop(e, true) }, false);

        container.addEventListener("contextmenu", (e) => { e.preventDefault(); e.stopPropagation(); ffFlow.contextMenu(e); return false; }, false);


        document.removeEventListener('copy', ffFlow.CopyEventListener);
        document.addEventListener('copy', ffFlow.CopyEventListener);
        document.removeEventListener('paste', ffFlow.PasteEventListener);
        document.addEventListener('paste', ffFlow.PasteEventListener);


        let canvas = document.querySelector('canvas');

        let width = ffFlow.Vertical ? (document.body.clientWidth * 1.5) : window.screen.availWidth
        let height = ffFlow.Vertical ? (document.body.clientHeight * 2) : window.screen.availHeight;

        canvas.height = height;
        canvas.width = width;
        canvas.style.width = canvas.width + 'px';
        canvas.style.height = canvas.height + 'px';

        for (let p of parts) {
            try {
                ffFlowPart.addFlowPart(p);
            } catch (err) {
                if(p != null && p.name)
                    console.error(`Error adding flow part '${p.name}: ${err}`);
                else
                    console.error(`Error adding flow part: ${err}`);
            }
        }

        ffFlow.redrawLines();
    },

    redrawLines: function () {
        ffFlow.FlowLines.redrawLines();
    },
    
    contextMenu: function(event, part){
        if(part){
            event.stopPropagation();
            event.stopImmediatePropagation();
            event.preventDefault();
        }                
        ffFlow.csharp.invokeMethodAsync("OpenContextMenu", {
            x: event.clientX,
            y: event.clientY,
            parts: this.SelectedParts
        });
        return false;
    },

    ioInitConnections: function (connections) {
        ffFlow.reset();
        for (let k in connections) { // iterating keys so use in
            for (let con of connections[k]) { // iterating values so use of
                let id = k + '-output-' + con.output;
                
                let list = ffFlow.FlowLines.ioOutputConnections.get(id);
                if (!list) {
                    ffFlow.FlowLines.ioOutputConnections.set(id, []);
                    list = ffFlow.FlowLines.ioOutputConnections.get(id);
                }
                list.push({ index: con.input, part: con.inputNode });
            }
        }
    },

    /*
     * Called from C# code to insert a new element to the flow
     */
    insertElement: function (uid) {
        ffFlow.drop(null, true, uid);
    },

    drop: function (event, dropping, uid) {
        let xPos = 100, yPos = 100;
        if (event) {
            event.preventDefault();
            if (dropping !== true)
                return;
            let bounds = event.target.getBoundingClientRect();

            xPos = ffFlow.translateCoord(event.clientX) - bounds.left - 20;
            yPos = ffFlow.translateCoord(event.clientY) - bounds.top - 20;
        } else {
        }
        if (!uid)
            uid = ffFlow.Mouse.draggingElementUid;
        ffFlow.addElementActual(uid, xPos, yPos);
    },
    
    addElementActual: function (uid, xPos, yPos) {

        ffFlow.csharp.invokeMethodAsync("AddElement", uid).then(result => {
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

            ffFlow.History.perform(new FlowActionAddNode(part));

            if (element.noEditorOnAdd === true)
                return;

            if (element.model && Object.keys(element.model).length > 0)
            {
                ffFlowPart.editFlowPart(part.uid, true);
            }
        }); 
    },

    translateCoord: function (value, lines) {
        if (lines !== true)
            value = Math.floor(value / 10) * 10;
        let zoom = ffFlow.Zoom / 100;
        if (!zoom || zoom === 1)
            return value;
        return value / zoom;
    },

    getModel: function () {
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

                if (ffFlow.SingleOutputConnection) {
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
    },

    getElement: function (uid) {
        return ffFlow.elements.filter(x => x.uid == uid)[0];
    },


    getPart: function (partUid) {
        return ffFlow.parts.filter(x => x.uid == partUid)[0];
    },

    infobox: null,
    infoboxSpan: null,
    infoSelectedType: '', 
    setInfo: function (message, type) {
        if (!message) {
            if (!ffFlow.infobox)
                return;
            ffFlow.infobox.style.display = 'none';
        } else {
            ffFlow.infoSelectedType = type;
            if (!ffFlow.infobox) {
                let box = document.createElement('div');
                box.classList.add('info-box');

                // remove button
                let remove = document.createElement('span');
                remove.classList.add('fas');
                remove.classList.add('fa-trash');
                remove.style.cursor = 'pointer';
                remove.setAttribute('title', ffFlow.lblDelete);
                remove.addEventListener("click", (e) => {
                    if (ffFlow.infoSelectedType === 'Connection')
                        ffFlow.FlowLines.deleteConnection();
                    else if (ffFlow.infoSelectedType === 'Node') {
                        if (ffFlow.SelectedParts?.length) {
                            for(let p of ffFlow.SelectedParts)
                                ffFlowPart.deleteFlowPart(p.uid);
                        }
                    }
                }, false);
                box.appendChild(remove);


                ffFlow.infoboxSpan = document.createElement('span');
                box.appendChild(ffFlow.infoboxSpan);


                document.getElementById('flow-parts').appendChild(box);
                ffFlow.infobox = box;
            }
            ffFlow.infobox.style.display = '';
            ffFlow.infoboxSpan.innerText = message;
        }
    },

    selectConnection: function (outputNode, output) {
        
        if (!outputNode) {
            ffFlow.setInfo();
            return;
        }
        
        if(this.SelectedParts?.length) {
            console.log('Unselecting parts!');
            this.unSelect();
            this.redrawLines();
            
            // this is un-focuses a node so if the user presses delete, that node is not deleted
            let canvas = document.querySelector('canvas');
            canvas.focus();
        }

        let part = ffFlow.getPart(outputNode);
        if (!part) {
            ffFlow.setInfo();
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
        ffFlow.setInfo(part.OutputLabels[output], 'Connection');
    },

    selectNode: function (part) {
        if (!part) {
            ffFlow.setInfo();
            return;
        }
        ffFlow.SelectedParts = [part];
        
        var ele = document.getElementById(part.uid);
        if(ele)
        {
            ele.classList.remove('selected');
            ele.classList.add('selected');
        }

        if (!part.displayDescription) {
            let element = ffFlow.getElement(part.flowElementUid);
            if (!element)
                return;
            ffFlow.csharp.invokeMethodAsync("Translate", `Flow.Parts.${element.name}.Description`, part.model).then(result => {
                part.displayDescription = ffFlow.lblNode + ': ' + (result === 'Description' || !result ? part.displayName : result);
                ffFlow.setInfo(part.displayDescription, 'Node');
            });
        } else {
            ffFlow.setInfo(part.displayDescription, 'Node');
        }
    },
    setOutputHint(part, output) {
        let element = ffFlow.getElement(part.flowElementUid);
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
            ffFlow.csharp.invokeMethodAsync("Translate", `Flow.Parts.${element.name}.Outputs.${output}`, part.model).then(result => {
                if (!part.OutputLabels) part.OutputLabels = {};
                part.OutputLabels[output] = result;
                let outputNode = document.getElementById(part.uid + '-output-' + output);
                if (outputNode)
                    outputNode.setAttribute('title', result);
            });
        }
    },
    initOutputHints(part) {
        if (!part || !part.outputs)
            return;
        for (let i = 0; i <= part.outputs; i++) {
            ffFlow.setOutputHint(part, i === 0 ? -1 : i);
        }
    },
    
    onKeyDown(event) {
        if (event.code === 'Delete' || event.code === 'Backspace') {
            for(let part of this.SelectedParts || []) {
                ffFlowPart.deleteFlowPart(part.uid);
            }
            event.stopImmediatePropagation();
            event.preventDefault();
        }
    },
    
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
        
        if (ffFlow.SelectedParts.length) {
            let json = JSON.stringify(ffFlow.SelectedParts);
            e.clipboardData.setData('text/plain', json);
            
        }
        e?.preventDefault();
    },

    async PasteEventListener(e, json) {
        let eleFlowParts = document.getElementById('flow-parts');
        if(!eleFlowParts)
            return; // not on flow page, dont consume paste

        if(!json) {
            let active = document.activeElement;
            if (active) {
                let flowParts = active.closest('.flow-parts');
                if (!flowParts)
                    return; // flowparts/canvas does not have focus, do not listen to this event
            }
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
            p.uid = await ffFlow.csharp.invokeMethodAsync("NewGuid");
            // for now we dont copy connections
            p.outputConnections = null;
            p.xPos += 120;
            p.yPos += 80;
            if(p.Name)
                p.Name = "Copy of " + p.Name;
            ffFlow.History.perform(new FlowActionAddNode(p));
        }
    },
        
    contextMenu_Edit: function(part){
        if(!part)
            return;
        ffFlow.setInfo(part.Name, 'Node');
        ffFlowPart.editFlowPart(part.uid);
    },

    contextMenu_EditSubFlow: function(part){
        if(!part)
            return;
        let currentUrl = window.location.href;
        let baseUrl = currentUrl.substring(0, currentUrl.lastIndexOf('/'));
        console.log('part', part);
        let newUrl = baseUrl + '/' + part.flowElementUid.substring(8);
        window.open(newUrl, '_blank');
    },
    
    contextMenu_Copy: function(parts) {
        let json = JSON.stringify(parts);
        navigator.clipboard.writeText(json);
    },
    contextMenu_Paste: function() {
        navigator.clipboard.readText().then(json => {
            ffFlow.PasteEventListener(null, json);            
        });
    },
    contextMenu_Delete: function(parts) {
        for(let part of parts || []) {
            ffFlowPart.deleteFlowPart(part.uid);
        }
    },
    contextMenu_Add: function() {
        let ele = document.getElementById('show-elements')
        if(ele)
            ele.click();
    }
}