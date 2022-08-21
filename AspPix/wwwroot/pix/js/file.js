(function () {
    document.addEventListener("DOMContentLoaded", function () {
        let getForm = document.getElementById("form");
        let pageCount = document.getElementById("down");
        let imgs = document.getElementById("imgs");
        let last = document.getElementById("last");
        function getUriPathWithoutQuery() {
            return window.location.href.split('?')[0];
        }
        let faterstyle = getForm.parentElement.style;
        faterstyle.position = "fixed";
        faterstyle.left = "0";
        faterstyle.top = "0";
        faterstyle.backgroundColor = "White";
        imgs.style.marginTop = getForm.offsetHeight + "px";
        getForm.addEventListener("change", (ev) => {
            if (ev.target !== pageCount) {
                pageCount.value = "0";
            }
            getForm.submit();
        });
        let options = {
            rootMargin: '0px',
            threshold: 1.0
        };
        let cn = 0;
        let observer = new IntersectionObserver((entries, observer) => {
            if (!entries.filter(v => v.target === last)[0]) {
                return;
            }
            pageCount.value = (++cn).toString();
            let xml = new XMLHttpRequest();
            xml.responseType = "document";
            xml.addEventListener("loadend", () => {
                let doc = xml.response;
                let e = doc.getElementById("imgs");
                let newimgs = e.getElementsByTagName("a");
                if (newimgs.length === 0) {
                    observer.disconnect();
                }
                else {
                    imgs.append(...newimgs);
                }
            });
            let fd = new FormData(getForm);
            let map = new Map(fd);
            let uq = new URLSearchParams(map);
            xml.open("GET", getUriPathWithoutQuery() + "?" + uq.toString());
            xml.send();
        }, options);
        observer.observe(last);
    });
})();
