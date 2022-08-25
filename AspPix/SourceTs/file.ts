(function () {


    function set输入部分的HTML为固定布局(form: HTMLFormElement, imgs: HTMLDivElement) {

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


    function on当元素进入窗口时运行(e: Element, func: () => void) {
        let options = {
            rootMargin: '0px',
            threshold: 1.0
        }
       
        let observer = new IntersectionObserver((entries, observer) => {
            

            let entrie = entries.filter(v => v.target === e)[0];

            if (entrie && entrie.isIntersecting) {

                observer.disconnect();

                setTimeout(func, 0);

            }

        }, options);

        observer.observe(e);
    }

    function load新的图片(uri: string, func: (doc: Document) => void) {

        let xml = new XMLHttpRequest();

        xml.responseType = "document";

        xml.addEventListener("loadend", () => {


            let doc = <Document>xml.response;


            setTimeout(() => func(doc), 0);

        });

        xml.open("GET", uri);

        xml.send(null);

    }


    function createUri(form: HTMLFormElement) {


        let fd = new FormData(form);


        let uq = new URLSearchParams((<Record<string, string>><unknown>fd));


        return getUriPathWithoutQuery() + "?" + uq.toString();

    }

    function into将图片放入DOM(imgs: HTMLDivElement, doc: Document, func: (last: Element) => void) {

        let e = <HTMLDivElement>doc.getElementById("imgs");

        let newimgs = e.getElementsByTagName("a");


        if (newimgs.length !== 0) {
            let last = newimgs[0];

            let coll = new DocumentFragment();

            coll.append(...newimgs);

            imgs.appendChild(coll);

            setTimeout(() => func(last), 0);

        }

    }

    function run增量加载图片(imgs: HTMLDivElement, form: HTMLFormElement, pageCount: HTMLInputElement) {

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


    document.addEventListener("DOMContentLoaded", function () {

        let getForm = <HTMLFormElement>document.getElementById("form");

        let pageCount = <HTMLInputElement>document.getElementById("down");

        let imgs = <HTMLDivElement>document.getElementById("imgs");

        let last = <HTMLDivElement>document.getElementById("last");

        set输入部分的HTML为固定布局(getForm, imgs);

        run增量加载图片(imgs, getForm, pageCount);


        getForm.addEventListener("change", (ev) => {

            if (ev.target !== pageCount) {
                pageCount.value = "0";
            }

            getForm.submit();


        });

        let sy = document.createElement("style");

        sy.innerHTML = `
            img {
                max-height: 300px;

                max-width: 300px;

            }

        `;


        document.head.appendChild(sy);

    });
})();