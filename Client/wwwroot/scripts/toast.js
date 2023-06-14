class Toast {
    static toastContainer = null;

    static info(title, message, timeout = 5000) {
        Toast.showToast('info', title, message, timeout);
    }

    static error(title, message, timeout = 5000) {
        console.log('tost arror!');
        Toast.showToast('error',  title, message, timeout);
    }

    static warn(title, message, timeout = 5000) {
        Toast.showToast('warn', title, message, timeout);
    }

    static success(title, message, timeout = 5000) {
        Toast.showToast('success', title, message, timeout);
    }
    static showToast(type, title, message, timeout) {
        if (!Toast.toastContainer) {
            Toast.createToastContainer();
        }

        const toast = document.createElement('div');
        toast.classList.add('ff-toast', type);
        toast.innerHTML = `
          <div class="toast-content">
            <span class="toast-icon"><i class="fas fa-${Toast.getIcon(type)}"></i></span>
            <span class="toast-message">
                ${title ? `<span class="title">${title}</span>` : ''}
                <span class="message">${message}</message>
            </span>
            <span class="toast-close"><i class="fas fa-times"></i></span>
          </div>
        `;

        const toastClose = toast.querySelector('.toast-close');
        toastClose.addEventListener('click', () => {
            Toast.removeToast(toast);
        });

        Toast.toastContainer.appendChild(toast);
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);

        setTimeout(() => {
            Toast.removeToast(toast);
        }, timeout);

        toast.addEventListener('mouseenter', () => {
            clearTimeout(toast.dismissTimeout);
        });

        toast.addEventListener('mouseleave', () => {
            toast.dismissTimeout = setTimeout(() => {
                Toast.removeToast(toast);
            }, timeout);
        });
    }


    static createToastContainer() {
        Toast.toastContainer = document.createElement('div');
        Toast.toastContainer.classList.add('toast-container');
        document.body.appendChild(Toast.toastContainer);
    }

    static removeToast(toast) {
        toast.classList.add('hide');
        setTimeout(() => {
            if (toast.parentNode === Toast.toastContainer) {
                Toast.toastContainer.removeChild(toast);
            }
            if (Toast.toastContainer.childElementCount === 0) {
                document.body.removeChild(Toast.toastContainer);
                Toast.toastContainer = null;
            }
        }, 500);
    }

    static getIcon(type) {
        switch (type) {
            case 'success':
                return 'check-circle';
            case 'warning':
                return 'exclamation-triangle';
            case 'info':
                return 'info-circle';
            case 'error':
                return 'times-circle';
            default:
                return '';
        }
    }
}