(function () {
    function set输入部分的HTML为固定布局(form, imgs) {
        let faterstyle = form.parentElement.style;
        faterstyle.position = "fixed";
        faterstyle.left = "0";
        faterstyle.top = "0";
        faterstyle.backgroundColor = "White";
        imgs.style.marginTop = form.offsetHeight + "px";
    }
    function getUriPathWithoutQuery() {
        return window.location.href.split('?')[0];
    }
    function on当元素进入窗口时运行(e, func) {
        let options = {
            rootMargin: '0px',
            threshold: 1.0
        };
        let observer = new IntersectionObserver((entries, observer) => {
            let entrie = entries.filter(v => v.target === e)[0];
            if (entrie && entrie.isIntersecting) {
                observer.disconnect();
                setTimeout(func, 0);
            }
        }, options);
        observer.observe(e);
    }
    function load新的图片(uri, func) {
        let xml = new XMLHttpRequest();
        xml.responseType = "document";
        xml.addEventListener("loadend", () => {
            let doc = xml.response;
            setTimeout(() => func(doc), 0);
        });
        xml.open("GET", uri);
        xml.send(null);
    }
    function createUri(form) {
        let fd = new FormData(form);
        let uq = new URLSearchParams(fd);
        return getUriPathWithoutQuery() + "?" + uq.toString();
    }
    function into将图片放入DOM(imgs, doc, func) {
        let e = doc.getElementById("imgs");
        let newimgs = e.getElementsByTagName("a");
        if (newimgs.length !== 0) {
            let last = newimgs[0];
            let coll = new DocumentFragment();
            coll.append(...newimgs);
            imgs.appendChild(coll);
            setTimeout(() => func(last), 0);
        }
    }
    function run增量加载图片(imgs, form, pageCount) {
        let n = 0;
        function on进入窗口CallBack() {
            pageCount.value = (++n).toString();
            let uri = createUri(form);
            load新的图片(uri, (doc) => {
                into将图片放入DOM(imgs, doc, (last) => {
                    on当元素进入窗口时运行(last, () => {
                        on进入窗口CallBack();
                    });
                });
            });
        }
        on进入窗口CallBack();
    }
    function set图片样式() {
        let sy = document.createElement("style");
        sy.innerHTML = `

            body {

                background-color: black;

            }

            img {

                object-fit: contain;

                height: 200px;

                width: 200px;


            }

        `;
        document.head.appendChild(sy);
    }
    set图片样式();
    document.addEventListener("DOMContentLoaded", function () {
        let getForm = document.getElementById("form");
        let pageCount = document.getElementById("down");
        let imgs = document.getElementById("imgs");
        let last = document.getElementById("last");
        set输入部分的HTML为固定布局(getForm, imgs);
        run增量加载图片(imgs, getForm, pageCount);
        getForm.addEventListener("change", (ev) => {
            if (ev.target !== pageCount) {
                pageCount.value = "0";
            }
            getForm.submit();
        });
    });
})();
