c:\\windows\\system32\\inetsrv\\appcmd.exe list config -section:system.webServer/rewrite/globalRules | findstr /C:"http_redirect">nul && (c:\\windows\\system32\\inetsrv\\appcmd.exe set config -section:system.webServer/rewrite/globalRules /-"[name='http_redirect']" || Echo.True ) || Echo.True
c:\\windows\\system32\\inetsrv\\appcmd.exe set config -section:system.webServer/rewrite/globalRules /+"[name='http_redirect',stopProcessing='True']"
c:\\windows\\system32\\inetsrv\\appcmd.exe set config -section:system.webServer/rewrite/globalRules /[name='http_redirect',stopProcessing='True'].match.url:"(.*)"
c:\\windows\\system32\\inetsrv\\appcmd.exe set config -section:system.webServer/rewrite/globalRules /+"[name='http_redirect',stopProcessing='True'].conditions.[input='{HTTP_X_FORWARDED_PROTO}',pattern='^http$']" 
c:\\windows\\system32\\inetsrv\\appcmd.exe set config -section:system.webServer/rewrite/globalRules /[name='http_redirect',stopProcessing='True'].action.type:"Redirect" /[name='http_redirect',stopProcessing='True'].action.url:"https://{HTTP_HOST}/{R:1}" /[name='http_redirect',stopProcessing='True'].action.redirectType:"Found"