
/*
(function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){
(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),
m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)
})(window,document,'script','//www.google-analytics.com/analytics.js','ga');

ga('create', 'UA-87769421-1', 'auto', {'sampleRate': 2});
ga('send', 'pageview');
*/

// --- Gestion du scaling et de l'affichage ---
(function (window) {
    var doc = window.document,
        body = doc.body,
        stageScale = body.clientWidth / 640,
        fontSize = stageScale * 30;

    // device check for fontSize
    var isAndroid = /Android .*;\s*(.*)\sBuild/.exec(navigator.userAgent);
    if ( isAndroid !== null && isAndroid[1] === 'Galaxy Nexus') {
        fontSize /= stageScale;
    }

    // for iPad
    if ( navigator.userAgent.indexOf('iPad') != -1 ) {
        stageScale = 1;
    }
    else if(navigator.userAgent.indexOf('Android') != -1){
        stageScale = screen.width / 640;
    }
    var pageHeight = Math.max(960, window.innerHeight / stageScale);

    var styleContent = '';
    if(navigator.userAgent.indexOf('Android') != -1){
        styleContent = 'html{zoom:1;font-size:30px;} body{font-size:'+ stageScale*100 +'%;} .page{height:'+ pageHeight +'px;}';
    } else {
        var zoomVal = (navigator.userAgent.indexOf('iPad') != -1) ? 1 : stageScale;
        styleContent = 'html{zoom:'+ zoomVal +';font-size:30px;} body{font-size:'+ stageScale*100 +'%;} .page{height:'+ pageHeight +'px;}';
        if (navigator.userAgent.indexOf('iPad') != -1) {
            styleContent += 'table{font-size:'+ stageScale*100 +'%;}';
        }
    }

    doc.write('<style>' + styleContent + '</style>');

    // set Global variables.
    window.stageScale = stageScale;
    window.fontSize = fontSize;
    window.pageHeight = pageHeight;
})(window);

// --- Gestion des cookies ---
var cookie;
function loadingBody(){
    try {
        cookie = document.cookie;
    } catch(e) {}
}
