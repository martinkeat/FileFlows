/**
 * Creates the NavMenu instance and returns it
 * @returns {NavMenu} the navmenu itenace
 */
export function createNavMenu()
{
    return new NavMenu();
}

/**
 * NavMenu JavaScript file
 */
export class NavMenu {
    
    /**
     * Constructs the NavMenu instance
     */
    constructor()
    {
        this.ul = document.getElementById('ul-nav-menu');
        if(this.ul) {
            this.resizeMenu = this.resizeMenu.bind(this); // Bind resizeMenu to the class instance
            //new ResizeObserver(this.resizeMenu).observe(this.ul)

            window.addEventListener('resize', this.resizeMenu);
        }

        // Get the computed style of the root element
        const htmlComputedStyle = window.getComputedStyle(document.documentElement);
        const rootFontSize = htmlComputedStyle.getPropertyValue('font-size');
        this.rem  = parseFloat(rootFontSize);
        this.styleSheet = document.createElement('style');
        document.head.appendChild(this.styleSheet);
    }

    setCSS(css) {
        this.styleSheet.innerText = '';
        this.styleSheet.innerText = '@media (min-width:850px) { \n' + css + '\n}';
    }

    menuSet(groups, totalItems, collapsed)
    {
        this.groups = groups;
        this.totalItems = totalItems;
        this.collapsed = collapsed;
        this.resizeMenu();
    }
    
    /**
     * Resizes the menu 
     */
    resizeMenu(){
        if(!this.groups || !this.totalItems)
            return;
                
        let idealGroupHeight = this.collapsed ? 0 : 2.75;
        let idealItemHeight = this.collapsed ? 3 : 2.5;
        
        let maxHeight = this.ul.clientHeight;
        
        let height = idealGroupHeight * this.groups * this.rem + idealItemHeight * this.totalItems * this.rem;
        if(height <= maxHeight)
        {
            this.setHeights(idealGroupHeight, idealItemHeight);
            return;
        }
        
        let percent = (height - maxHeight) / maxHeight * 100;
        let groupHeight = this.collapsed ? 0 : percent < 25 ? 2 : 0;
        
        let itemHeight = idealItemHeight + 0.1;
        let count = 0;
        while(height > maxHeight && ++count < 100){
            itemHeight -= 0.05;
            height = (groupHeight * this.groups * this.rem) + (itemHeight * this.totalItems * this.rem);
        }
        this.setHeights(groupHeight, itemHeight);
    }
    
    setHeights(groupHeight, itemHeight){
        let css = '';
        if(!groupHeight)
            css += '.nav-menu-group { display: none !important; }\n';
        else
            css += `.nav-menu-group { padding-bottom:0.25rem; height: ${groupHeight - 0.25}rem !important; }\n`;
        
        css += `.nav-item { height: ${itemHeight - 0.1}rem !important; padding:0.05rem 0; }`;
        css += `.nav-menu-container.collapse .nav-item { height: ${itemHeight + 0.25}rem !important; }`;
        this.setCSS(css);
    }
}