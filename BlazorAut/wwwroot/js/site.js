function setCookie(name, value, days, dotNetHelper) {
    var expires = "";
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/;  secure";
    
    console.log("[site.js] document.cookie после setCookie:", document.cookie);
    dotNetHelper.invokeMethodAsync('RefreshAuthenticationState');
}
function deleteCookie(name, dotNetHelper) {
    document.cookie = name + '=; expires=Thu, 01 Jan 1970 00:00:01 GMT; path=/;  secure';

    console.log("[site.js] document.cookie после deleteCookie:", document.cookie);
    dotNetHelper.invokeMethodAsync('RefreshAuthenticationState');
}
