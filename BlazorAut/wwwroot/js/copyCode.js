window.copyCode = (codeId) => {
    const codeElement = document.getElementById(codeId);
    if (codeElement) {
        const text = codeElement.innerText;
        navigator.clipboard.writeText(text).then(() => {
            alert('Code copied to clipboard!');
        }).catch(err => {
            console.error('Failed to copy text: ', err);
        });
    }
};
