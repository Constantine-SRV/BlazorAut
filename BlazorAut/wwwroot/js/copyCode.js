// Function to copy code to clipboard
window.copyCode = (codeId) => {
    const codeElement = document.getElementById(codeId);
    if (codeElement) {
        const text = codeElement.innerText;
        navigator.clipboard.writeText(text).then(() => {
            // Create a notification element
            const notification = document.createElement('div');
            notification.innerText = 'Code copied to clipboard!';
            notification.style.position = 'fixed';
            notification.style.bottom = '10px';
            notification.style.right = '10px';
            notification.style.backgroundColor = '#28a745';
            notification.style.color = '#fff';
            notification.style.padding = '10px 20px';
            notification.style.borderRadius = '5px';
            notification.style.boxShadow = '0 2px 6px rgba(0,0,0,0.2)';
            notification.style.zIndex = '1000';
            document.body.appendChild(notification);

            // Remove the notification after 3 seconds
            setTimeout(() => {
                document.body.removeChild(notification);
            }, 3000);
        }).catch(err => {
            console.error('Failed to copy text: ', err);
        });
    }
};
