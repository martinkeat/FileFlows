/**
 * Creates the InputTextArea instance and returns it
 * @param dotNetObject The calling dotnet object
 * @param uid the UID of the textarea element
 * @param variables the Dictionary<string, object> from C# of available variables to show
 * @returns {InputTextArea} the input text area javascript instance
 */
export function createInputTextArea(dotNetObject, uid, variables)
{
    return new InputTextArea(dotNetObject, uid, variables);
}

/**
 * InputTextArea JavaScript file
 */
export class InputTextArea {
    constructor(dotNetObject, uid, variables) {
        this.dotNetObject = dotNetObject;
        this.uid = uid;
        this.variables = variables;
        this.dropdownIndex = 0;
        this.currentVariable = "";
        
        // Reference to the textarea element
        this.textarea = document.getElementById(uid);

        // Create dropdown element
        this.dropdown = document.createElement("ul");
        this.dropdown.id = "ta-var-dropdown-" + uid;
        this.dropdown.className = "ta-var-dropdown";
        this.dropdown.style.position = "absolute";
        this.dropdown.style.display = "none"; // Initially hidden
        document.body.appendChild(this.dropdown);

        // Event listeners
        this.textarea.addEventListener("keydown", this.handleKeydown.bind(this));
        this.textarea.addEventListener('click', this.handleClick.bind(this))


        // CSS for the dropdown
        const dropdownStyle = document.createElement("style");
        dropdownStyle.textContent = `
            .ta-var-dropdown {
                background: var(--base-lighter);
                border: solid 1px var(--input-background);
                padding: 5px;
                list-style-type: none;
                z-index: 1000;
                max-height: 14rem;
                overflow: auto;
            }
            .ta-var-dropdown li {
                cursor: pointer;
                padding: 3px;
            }
            .ta-var-dropdown li:hover {
                background-color: var(--accent);
            }
            .ta-var-dropdown li.selected {
                background-color: rgba(var(--accent-rgb), 0.7);
            }
        `;
        document.head.appendChild(dropdownStyle);
    }
    
    updateValue(value) {
        this.dotNetObject.invokeMethodAsync("updateValue", value);
    }

    handleClick(event)
    {
        if (this.isDropdownVisible())
        {
            this.removeBrackets();            
        }
    }

    handleKeydown(event) {
        if (event.key === "{") {
            this.showDropdown();
            event.preventDefault();
        } else if (this.isDropdownVisible()) {
            // Handle arrow keys, Enter, and Escape in the dropdown
            if (event.key === "ArrowUp") {
                event.preventDefault();
                this.selectPrevious();
            } else if (event.key === "ArrowDown") {
                event.preventDefault();
                this.selectNext();
            } else if (event.key === "Enter") {
                event.preventDefault();
                this.insertSelectedVariable();
                this.hideDropdown();
            } else if (event.key === "Escape") {
                event.preventDefault();
                this.removeBrackets(); // Remove brackets and hide dropdown
            } else if (event.key === "Backspace") {
                if (this.currentVariable === "") {
                    this.removeBrackets(); // Remove brackets if currentVariable is empty
                } else {
                    this.updateVariableText(this.currentVariable.slice(0, -1)); // Call updateVariableText method
                }
            }else if (event.key.length === 1) {
                // Check if adding the typed key forms a valid variable
                const potentialVariable = this.currentVariable + event.key;
                if (this.isValidVariable(potentialVariable)) {
                    // Update currentVariable and filter variables based on the typed key
                    this.updateVariableText(potentialVariable); // Call updateVariableText method
                    this.selectItem(0);
                }
            }
            event.preventDefault();
        }
    }

    updateVariableText(newText) {
        // Update currentVariable
        this.currentVariable = newText;
        
        this.textarea.value = this.textBeforeCursor + '{' + newText + '}' + this.textAfterCursor;

        // Set cursor position after inserted variable
        const newCursorPosition = this.cursorPosition + newText.length;
        this.textarea.setSelectionRange(newCursorPosition, newCursorPosition);
        this.updateDropdown(newText);
    }
    
    removeBrackets() {
        this.textarea.value = this.textBeforeCursor + this.textAfterCursor;
        const position = Math.max(0, this.cursorPosition - 1);
        this.textarea.setSelectionRange(position, position);
        this.hideDropdown();
    }

    isDropdownVisible() {
        return this.dropdown.style.display === "block";
    }

    updateDropdown(partialVariable) {
        const filteredVariables = Object.keys(this.variables).filter(variable => variable.startsWith(partialVariable));
        if (filteredVariables.length > 0) {
            // Update dropdown options
            this.dropdown.innerHTML = ""; // Clear existing options
            filteredVariables.forEach((variable, index) => {
                const listItem = document.createElement("li");
                listItem.textContent = variable;
                listItem.addEventListener("click", () => {
                    this.insertSelectedVariable(variable);
                    this.hideDropdown();
                });
                if (index === this.dropdownIndex) {
                    listItem.classList.add("selected");
                }
                this.dropdown.appendChild(listItem);
            });
        } else {
            this.hideDropdown();
        }
    }
    showDropdown() {
        this.currentVariable = '';
        this.updateDropdown(""); // Call updateDropdown with an empty string to show all variables initially
        this.textarea.setRangeText("{", this.textarea.selectionStart, this.textarea.selectionEnd, "end");
        this.textarea.setRangeText("}", this.textarea.selectionEnd, this.textarea.selectionEnd, "end");
        this.textarea.setSelectionRange(this.textarea.selectionEnd - 1, this.textarea.selectionEnd - 1);
        
        // Cache the text before and after the cursor
        this.cursorPosition = this.textarea.selectionStart;
        this.textBeforeCursor = this.textarea.value.substring(0, this.cursorPosition - 1); // -1 for {
        this.textAfterCursor = this.textarea.value.substring(this.cursorPosition + 1); // +1 for }

        const rect = this.textarea.getBoundingClientRect();
        const lineHeight = parseInt(getComputedStyle(this.textarea).lineHeight);
        const scrollTop = this.textarea.scrollTop; // Get the scrollTop of the textarea

        // Calculate the top position relative to the viewport, accounting for the scroll position
        const linesAboveCursor = this.textarea.value.substring(0, this.cursorPosition).split('\n').length;
        const topPosition = rect.top + window.pageYOffset + lineHeight * linesAboveCursor - scrollTop;

        // Set the dropdown position
        this.dropdown.style.top = topPosition + "px";
        this.dropdown.style.left = rect.left + "px";

        // Set the dropdown width to match the textarea width
        this.dropdown.style.width = rect.width + "px";

        this.dropdown.style.display = "block";
        this.selectItem(0);
    }


    hideDropdown() {
        this.dropdown.style.display = "none";
    }
    
    selectNext() {
        this.selectItem('next')
    }

    selectPrevious() {
        this.selectItem('previous');
    }
    selectItem(direction) {
        const listItems = this.dropdown.getElementsByTagName("li");
        let newIndex;

        if(direction === 0){
            newIndex = 0;
        } else if (direction === "next") {
            newIndex = this.dropdownIndex < listItems.length - 1 ? this.dropdownIndex + 1 : 0;
        } else {
            newIndex = this.dropdownIndex > 0 ? this.dropdownIndex - 1 : listItems.length - 1;
        }

        if(listItems.length > this.dropdownIndex)
            listItems[this.dropdownIndex].classList.remove("selected");
        listItems[newIndex].classList.add("selected");

        // Scroll into view only if the selected item is not fully visible
        const itemRect = listItems[newIndex].getBoundingClientRect();
        if (itemRect.bottom > this.dropdown.clientHeight || itemRect.top < 0) {
            listItems[newIndex].scrollIntoView({ behavior: "smooth", block: "nearest", inline: "nearest" });
        }

        this.dropdownIndex = newIndex;
    }

    insertSelectedVariable() {
        const listItems = this.dropdown.getElementsByTagName("li");
        const selectedVariable = listItems[this.dropdownIndex].textContent;
        const newText = this.textBeforeCursor + '{' + selectedVariable + '}' + this.textAfterCursor;
        this.textarea.value = newText;

        // Set cursor position after inserted variable
        const newCursorPosition = this.textBeforeCursor.length + selectedVariable.length + 2;
        this.textarea.setSelectionRange(newCursorPosition, newCursorPosition);
        this.updateValue(newText);
    }

    isValidVariable(partialVariable) {
        // Check if any variable starts with the given partialVariable
        return Object.keys(this.variables).some(variable => variable.startsWith(partialVariable));
    }
}
