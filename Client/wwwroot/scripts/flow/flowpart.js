window.ffFlowPart = {

    flowPartElements: [],

    reset: function () {
        ffFlowPart.flowPartElements = [];
    },

    unselectAll: function () {
        for (let ele of ffFlowPart.flowPartElements) 
            ele.classList.remove('selected');
    },

    focusElement: function (uid) {
        let ele = document.getElementById(uid);
        if (ele)
            ele.focus();
    },

    addFlowPart: function (part) {
        let div = document.createElement('div');
        ffFlowPart.flowPartElements.push(div);
        div.setAttribute('id', part.uid);
        div.style.position = 'absolute';
        let xPos = Math.floor(part.xPos / 10) * 10;
        let yPos = Math.floor(part.yPos / 10) * 10;
        div.style.left = xPos + 'px';
        div.style.top = yPos + 'px';
        div.classList.add('flow-part');
        if (ffFlow.Vertical)
            div.classList.add('vertical');
        if (typeof (part.type) === 'number') {
            if (part.type === 0)
                div.classList.add('Input');
            else if (part.type == 1)
                div.classList.add('Output');
            else if (part.type == 2)
                div.classList.add('Process');
            else if (part.type == 3)
                div.classList.add('Logic');
            else if (part.type == 4)
                div.classList.add('BuildStart');
            else if (part.type == 5)
                div.classList.add('BuildEnd');
            else if (part.type == 6)
                div.classList.add('BuildPart');
            else if (part.type == 7)
                div.classList.add('Failure');
            else if (part.type == 8)
                div.classList.add('Communication');
            else if (part.type == 9)
                div.classList.add('Script');
        }

        var mc = new Hammer.Manager(div);
        mc.add( new Hammer.Tap({ event: 'doubletap', taps: 2 }) );
        mc.on("doubletap", (ev) => {
            ffFlowPart.editFlowPart(part.uid, false);
        });
        div.classList.add('size-' + Math.max(part.inputs, part.outputs));

        div.setAttribute('tabIndex', -1);
        ffFlowPart.attachEventListeners({part: part, div: div});

        if (part.inputs > 0) {
            let divInputs = document.createElement('div');
            divInputs.classList.add('inputs');
            div.appendChild(divInputs);
            for (let i = 1; i <= part.inputs; i++) {
                let divInput = document.createElement('div');
                let divInputInner = document.createElement('div');
                divInput.appendChild(divInputInner);
                divInput.setAttribute('id', part.uid + '-input-' + i);
                divInput.setAttribute('x-input', i);
                divInput.classList.add('input');
                divInput.classList.add('input-' + i);
                divInput.addEventListener("onclick", function (event) {
                    console.log('divInput click ' + i, part);
                });
                divInput.addEventListener("onmousedown", function (event) {
                    console.log('divInput mouse down ' + i, part);
                    ffFlowLines.ioDown(event, true);
                });
                divInput.addEventListener("contextmenu", function (event) {
                    event.preventDefault();
                    return false;
                });

                divInputs.appendChild(divInput);
            }
        }
        let divIconWrapper = document.createElement('div');
        divIconWrapper.classList.add('icon-wrapper');
        div.appendChild(divIconWrapper);
        let spanIcon = document.createElement('span');
        spanIcon.classList.add('icon');
        if (part.icon) {
            for (let picon of part.icon.split(' '))
                spanIcon.classList.add(picon);
        }
        divIconWrapper.appendChild(spanIcon);

        let divName = document.createElement('div');
        divName.classList.add('name');
        div.appendChild(divName);
        ffFlowPart.updateOutputNodes(part.uid, part, div);

        let divDraggable = document.createElement('div');
        divDraggable.classList.add('draggable');
        divDraggable.addEventListener("contextmenu", (e) => { ffFlow.contextMenu(e, part); return false; }, false);

        div.appendChild(divDraggable);

        let flowParts = document.getElementById('flow-parts');
        flowParts.appendChild(div);

        ffFlowPart.setPartName(part);
        ffFlow.initOutputHints(part);
    },
    
    attachEventListeners: function(args){
        let part = args.part;
        let div = args.div;
        let allEvents = args.allEvents;
        if(!div)
            div = document.getElementById(part.uid);

        div.addEventListener("click", function (event) {
            event.stopImmediatePropagation();
            event.preventDefault();
            ffFlowPart.unselectAll();
            div.classList.add('selected');
            ffFlow.SelectedParts = [part];
            ffFlow.selectNode(part);
        });
        div.addEventListener("dblclick", function (event) {
            event.stopImmediatePropagation();
            event.preventDefault();
            ffFlow.setInfo(part.Name, 'Node');
            ffFlowPart.editFlowPart(part.uid);
        });
        div.addEventListener("keydown", function (event) {
            if (event.code === 'Delete' || event.code === 'Backspace') {
                ffFlowPart.deleteFlowPart(part.uid);
                event.stopImmediatePropagation();
                event.preventDefault();
            }
            else if (event.code === 'Enter') {
                ffFlowPart.editFlowPart(part.uid);
                event.stopImmediatePropagation();
                event.preventDefault();
            }
        });
        div.addEventListener("contextmenu", function (event) {
            event.preventDefault();
            return false;
        });
        if(allEvents){
            for(let output of div.querySelectorAll('.output div')){
                ffFlowPart.attachOutputNodeEvents(output);
            }
        }
    },
    
    attachOutputNodeEvents: function(divOutput) {
        divOutput.addEventListener("click", function (event) {            
            event.stopImmediatePropagation();
            event.preventDefault();
        });
        divOutput.addEventListener("mousedown", function (event) {
            event.stopImmediatePropagation();
            event.preventDefault();
            ffFlow.FlowLines.ioDown(event, false);
        });
    },

    setPartName: function (part) {
        try {
            let divName = document.getElementById(part.uid);
            divName = divName.querySelector('.name');
            if (!divName) 
                return;
            let name = part.name;
            if (!name)
                name = part.label;
            if (!name) 
                name = part.flowElementUid.substring(part.flowElementUid.lastIndexOf('.') + 1);

            part.displayName = name;
            divName.innerHTML = name;
        } catch (err) {
            console.error(err);
        }
    },

    deleteFlowPart: function (uid) 
    {
        console.log("deleting flow part");
        ffFlow.History.perform(new FlowActionDelete(uid));
    },

    editFlowPart: function (uid, deleteOnCancel) {
        let part = ffFlow.parts.filter(x => x.uid === uid)[0];
        if (!part)
            return;

        ffFlow.csharp.invokeMethodAsync("Edit", part, deleteOnCancel === true).then(result => {
            console.log('edit result', result);
            if (!result || !result.model) {
                if (deleteOnCancel === true) {
                    ffFlowPart.deleteFlowPart(uid);
                }
                return; // editor was canceled
            }
            if (result.model.Name) {
                part.name = result.model.Name;
                delete result.model.Name;
            } else if (result.model.Name === '') {
                part.name = '';
                let dn = ffFlowPart.getNodeName(part.flowElementUid);
                if (dn) 
                    part.label = dn;
                delete result.model.Name;
            }
            part.model = result.model;

            ffFlowPart.setPartName(part);

            if (result.outputs >= 0) {
                part.outputs = result.outputs;
                // have to update any connections in case they are no long available
                ffFlowPart.updateOutputNodes(part.uid, part);
                ffFlow.redrawLines();
            }
            ffFlow.initOutputHints(part);
            ffFlow.redrawLines();

        });
    },

    getNodeName: function (elementUid) {
        let node = ffFlow.elements.filter(x => x.uid === elementUid)[0];
        if (node)
            return node.displayName;
        return '';
    },


    updateOutputNodes: function (uid, part, div) {
        console.log('updateOutputNodes', part);
        if (!part)
            part = ffFlow.parts.filter(x => x.uid === uid)[0];
        if (!part) {
            console.log('part not found', uid);
            return;
        }
        if (!div) {
            div = document.getElementById(uid);
            if (!div) {
                console.log('div not found', uid);
                return;
            }
        }
        for (let i = 1; i < 100; i++) {
            div.classList.remove('size-' + i);
        }
        div.classList.add('size-' + Math.max(part.outputs, 1));

        let divOutputs = div.querySelector('.outputs');

        if (part.outputs > 0) {
            if (!divOutputs) {
                divOutputs = document.createElement('div');
                div.appendChild(divOutputs);
            }
            else {
                while (divOutputs.hasChildNodes()) {
                    divOutputs.removeChild(divOutputs.firstChild);
                }
            }
            divOutputs.className = 'outputs outputs-' + Math.max(part.outputs, 1);
            for (let i = 1; i <= part.outputs; i++) {
                let divOutput = document.createElement('div');
                let divOutputInner = document.createElement('div');
                divOutput.appendChild(divOutputInner);
                divOutput.setAttribute('id', part.uid + '-output-' + i);
                divOutput.setAttribute('x-output', i);
                divOutput.classList.add('output');
                divOutput.classList.add('output-' + i);
                ffFlowPart.attachOutputNodeEvents(divOutput);
                divOutputs.appendChild(divOutput);
            }
        } else if (divOutputs) {
            divOutputs.remove();
        }

        // delete any connections
        for (let i = part.outputs + 1; i < 100; i++) {
            ffFlow.FlowLines.ioOutputConnections.delete(part.uid + '-output-' + i);
        }
    }

}