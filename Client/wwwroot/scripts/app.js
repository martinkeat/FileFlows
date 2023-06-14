window.dashboardElementResized = new Event('dashboardElementResized', {});

window.ff = {

    log: function (level, parameters) {

        if (!parameters || parameters.length == 0)
            return;
        let message = parameters[0]
        parameters.splice(0, 1);

        if (level === 0) parameters.length > 0 ? console.error('ERRR: ' + message, parameters) : console.error('ERRR: ' + message);
        else if (level === 1) parameters.length > 0 ? console.warn('WARN: ' + message, parameters) : console.warn('WARN: ' + message);
        else if (level === 2) parameters.length > 0 ? console.info('INFO: ' + message, parameters) : console.info('INFO: ' + message);
        else if (level === 3) parameters.length > 0 ? console.log('DBUG: ' + message, parameters) : console.log('DBUG: ' + message);
        else parameters.length > 0 ? console.error(message, parameters) : console.log(message);
    },
    deviceDimensions: function () {

        return { width: screen.width, height: screen.height };
    },
    disableMovementKeys: function (element) {
        if (typeof (element) === 'string')
            element = document.getElementById(element);
        if (!element)
            return;
        const blocked = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Enter', 'PageUp', 'PageDown', 'Home', 'End'];
        element.addEventListener('keydown', e => {
            if (e.target.getAttribute('data-disable-movement') != 1)
                return;

            if (blocked.indexOf(e.code) >= 0) {
                e.preventDefault();
                return false;
            }
        })
    },
    loadJS: function (url, callback) {
        if (!url)
            return;
        let tag = document.createElement('script');
        tag.src = url;
        tag.onload = () => {
            if (callback)
                callback();
        };
        document.body.appendChild(tag);
    },
    downloadFile: async function (url, filename) {
        try {
            const response = await fetch(url);

            if (!response.ok) {
                Toast.error('File not found.');
                return;
            }

            const anchorElement = document.createElement('a');
            anchorElement.href = '#';

            anchorElement.addEventListener('click', async (event) => {
                event.preventDefault();

                const readableStream = response.body;
                const writableStream = new WritableStream({
                    async write(chunk) {
                        const reader = new FileReader();
                        reader.onload = () => {
                            const dataURL = reader.result;
                            const downloadElement = document.createElement('a');
                            downloadElement.href = dataURL;
                            downloadElement.download = filename ? filename : 'File';
                            downloadElement.click();
                            downloadElement.remove();
                        };

                        reader.readAsDataURL(chunk);
                    }
                });

                await readableStream.pipeTo(writableStream);
            });

            anchorElement.click();
        } catch (error) {
            Toast.error('Error occurred:', error);
        }        
    },
    copyToClipboard: function (text) {
        if (window.clipboardData && window.clipboardData.setData) {
            // Internet Explorer-specific code path to prevent textarea being shown while dialog is visible.
            return window.clipboardData.setData("Text", text);

        }
        else if (document.queryCommandSupported && document.queryCommandSupported("copy")) {
            var textarea = document.createElement("textarea");
            textarea.textContent = text;
            textarea.style.position = "fixed";  // Prevent scrolling to bottom of page in Microsoft Edge.
            document.body.appendChild(textarea);
            textarea.select();
            try {
                return document.execCommand("copy");  // Security exception may be thrown by some browsers.
            }
            catch (ex) {
                console.warn("Copy to clipboard failed.", ex);
                return prompt("Copy to clipboard: Ctrl+C, Enter", text);
            }
            finally {
                document.body.removeChild(textarea);
            }
        }
    },
    scrollTableToTop: function(){
        document.querySelector('.flowtable-body').scrollTop = 0;
    },
    
    
    dashboard: function(uid) {
        let dashboardData = localStorage.getItem('dashboard-' + uid);
        
        if(dashboardData)
        {
            try {
                dashboardData = JSON.parse(dashboardData);
                for (let item of dashboardData) {
                    let ele = document.getElementById(item.id);
                    if (!ele) {
                        console.log('element not found', item, item.id);
                        continue;
                    }
                    ele.setAttribute('gs-x', item.x);
                    ele.setAttribute('gs-y', item.y);
                    ele.setAttribute('gs-w', item.w);
                    ele.setAttribute('gs-h', item.h);
                }
            }catch(err){
                // can throw if the saved data is corrupt, silent fail to defaults
            }
        }

        var grid = GridStack.init({
            cellHeight:170,
            handle: '.draghandle'
        });

        saveGrid = () => {
            let data = [];
            for(let ele of document.querySelectorAll('.grid-stack-item')){
                let id = ele.id;
                let x = parseInt(ele.getAttribute('gs-x'), 10);
                let y = parseInt(ele.getAttribute('gs-y'), 10);
                let w = parseInt(ele.getAttribute('gs-w'), 10);
                let h = parseInt(ele.getAttribute('gs-h'), 10);
                data.push({
                    id:id, x: x, y:y, w:w, h:h
                });                
            }
            localStorage.setItem('dashboard-' + uid, JSON.stringify(data));
        }
        
        grid.on('resizestop', (e, el) => {
            window.dashboardElementResized.args = e;
            el.dispatchEvent(window.dashboardElementResized);
            saveGrid();
        });
    },
    
    nearBottom: function(element){
        let ele = element;
        if(typeof(element) === 'string')
            ele = document.getElementById(element);
        if(!ele)
            ele = document.querySelector(element);
        if(!ele)
            return false;

        const threshold = 100;
        const position = ele.scrollTop + ele.offsetHeight;
        const height = ele.scrollHeight;
        return position > height - threshold;
    },
    scrollToBottom: function(element, notSmooth) {
        let ele = element;
        if(typeof(element) === 'string')
            ele = document.getElementById(element);
        if(!ele)
            ele = document.querySelector(element);
        if(!ele)
            return false;
        if(notSmooth)
            ele.scrollTo({ top: ele.scrollHeight })
        else
            ele.scrollTo({ top: ele.scrollHeight, behavior: 'smooth' })
    },
    codeCaptureSave: function(csharp) {
        window.CodeCaptureListener = (e) => {
            if(e.ctrlKey === false || e.shiftKey || e.altKey || e.code != 'KeyS')
                return;
            e.preventDefault();
            e.stopPropagation();
            setTimeout(() => {                
                csharp.invokeMethodAsync("SaveCode");
            },1);
            return true;
        };
        document.addEventListener("keydown", window.CodeCaptureListener);
    },
    codeUncaptureSave: function(){
        document.removeEventListener("keydown", window.CodeCaptureListener);        
    },
    onEscapeListener: function(csharp) {
        window.CodeCaptureListener = (e) => {
            if(e.ctrlKey || e.shiftKey || e.altKey || e.code != 'Escape')
                return;
            e.preventDefault();
            e.stopPropagation();
            
            hasModal = !!document.querySelector('.flow-modal');
            logPartialViewer = !!document.querySelector('.log-partial-viewer');
            
            setTimeout(() => {
                csharp.invokeMethodAsync("OnEscape", { hasModal: hasModal, hasLogPartialViewer: logPartialViewer });
            },1);
            return true;
        };
        document.addEventListener("keydown", window.CodeCaptureListener);
    },
    attachEventListeners: function(csharp) {
        window.addEventListener('blur', () => {csharp.invokeMethodAsync("EventListener", "WindowBlur")});
        document.addEventListener('click', () => {csharp.invokeMethodAsync("EventListener", "DocumentClick")});        
    },
    resizableEditor: function(uid) {
        let panel = document.getElementById(uid);
        if(!panel)
            return;
        
        if(panel.classList.contains('maximised'))
            return;

        const BORDER_SIZE = 4;

        function resize(e){
            let bWidth = document.body.clientWidth;
            let width = bWidth - e.x;
            panel.style.width = width + "px";
        }

        panel.addEventListener("mousedown", function(e){
            if(e.target !== panel)
                return;
            if (e.offsetX < BORDER_SIZE) {
                document.addEventListener("mousemove", resize, false);
            }
            e.preventDefault();
            e.stopPropagation();
            return false;
        }, false);

        document.addEventListener("mouseup", function(){
            document.removeEventListener("mousemove", resize, false);
        }, false);        
    },
    resetTable: function(uid, tableIdentifier) {
        console.log('reset table');
        localStorage.removeItem(tableIdentifier);        
        this.resizableTable(uid, tableIdentifier, true);
    },
    resizableTable: function(uid, tableIdentifier, reset){
        let div = document.getElementById(uid);
        if(!div)
            return;
        
        let existing = div.querySelectorAll('.resizer');
        for(let item of existing)
            item.remove();

        const getCssClass = function(name) {
            for(let ss of document.styleSheets)
            {
                for(let rule of ss.cssRules)
                {
                    if(rule.selectorText === '.' + name)
                        return rule;
                }
            }
        }
        
        const saveColumnInfo = function()
        {
            if(!tableIdentifier)
                return;
            const cols = div.querySelectorAll('.flowtable-header-row > span:not(.hidden)');
            let widths = [];
            for(let col of cols){
                const styles = window.getComputedStyle(col);
                w = parseInt(styles.width, 10);
                widths.push(w);
            }
            localStorage.setItem(tableIdentifier, JSON.stringify(widths));            
        }
        
        const getClassName = function(col){
            return col.className.match(/col\-[\d]+/)[0]
        } 
                
        const createResizableColumn = function (col, resizer, next) {
            let x = 0;
            let w = 0;
            let nW = 0;

            let colClassName = getClassName(col);
            let nextClassName = getClassName(next);
            let colClass = getCssClass(colClassName);
            let nextClass = getCssClass(nextClassName);
            
            const mouseDownHandler = function (e) {
                x = e.clientX;
                const styles = window.getComputedStyle(col);
                w = parseInt(styles.width, 10);
                const nextStyles = window.getComputedStyle(next);
                nW = parseInt(nextStyles.width, 10);
                document.addEventListener('mousemove', mouseMoveHandler);
                document.addEventListener('mouseup', mouseUpHandler);
                resizer.classList.add('resizing');
            };

            const mouseMoveHandler = function (e) {
                const dx = e.clientX - x;
                colClass.style.width = `${w + dx}px`;
                colClass.style.minWidth = `${w + dx}px`;
                nextClass.style.width = `${nW - dx}px`;
                nextClass.style.minWidth = `${nW - dx}px`;                
            };

            const mouseUpHandler = function () {
                resizer.classList.remove('resizing');
                document.removeEventListener('mousemove', mouseMoveHandler);
                document.removeEventListener('mouseup', mouseUpHandler);
                saveColumnInfo();
            };

            resizer.addEventListener('mousedown', mouseDownHandler);
        }
        
        const cols = div.querySelectorAll('.flowtable-header-row > span:not(.hidden)');
        let savedWidths = [];
        if(tableIdentifier != null){
            let saved = localStorage.getItem(tableIdentifier);
            if(saved){
                savedWidths = JSON.parse(saved);
            }
        }

        for(let i=0;i<cols.length;i++) {
            let col = cols[i];
            if (col.classList.contains('flowtable-select'))
                continue;

            let colClassName = getClassName(col);
            let colClass = getCssClass(colClassName);
            if (colClass) {
                if (reset) 
                {
                    let width = col.getAttribute('data-width');
                    if(width) {
                        colClass.style.width = width;
                        colClass.style.minWidth = width;
                    }
                    else
                    {
                        colClass.style.width = 'unset';
                        colClass.style.minWidth = 'unset';
                    }
                }
                else if (i < savedWidths.length && savedWidths[i] > 0) {
                    colClass.style.width = `${savedWidths[i]}px`;
                    colClass.style.minWidth = `${savedWidths[i]}px`;
                }
            }
            if(i < cols.length - 1) {
                const resizer = document.createElement('div');
                col.style.position = 'relative';
                resizer.classList.add('resizer');
                resizer.style.height = `${div.offsetHeight}px`;
                col.appendChild(resizer);
                createResizableColumn(col, resizer, cols[i + 1]);
            }
        }
    },
    stopSelectPropagation: function(event){
        if(event.ctrlKey === false && event.shiftKey === false)
        {
            event.stopPropagation();
            return false;
        }
    },
    toast: function(type, title, message, timeout){
        switch(type)
        {
            case 'info': Toast.info(title, message, timeout); break;
            case 'warn': Toast.warn(title, message, timeout); break;
            case 'success': Toast.success(title, message, timeout); break;
            case 'error': Toast.error(title, message, timeout); break;
        }
    }
};