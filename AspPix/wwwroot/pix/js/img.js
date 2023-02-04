(function () {
    document.addEventListener("DOMContentLoaded", function () {
        (function () {
            let imgDiv = document.getElementById("imgDiv");
            let vs = JSON.parse(window.atob(imgDiv.getAttribute("my")));
            function addImg(vs, n) {
                if (vs.length !== 0) {
                    let uri = vs[0] + n + vs[1];
                    let img = document.createElement("img");
                    img.onload = () => {
                        if (img.width !== 0 && img.height !== 0) {
                            imgDiv.appendChild(img);
                            addImg(vs, n + 1);
                        }
                    };
                    img.src = uri;
                }
            }
            addImg(vs, 0);
        })();
        (function () {
            function request(s) {
                let xml = new XMLHttpRequest();
                xml.addEventListener("loadend", () => window.location.reload());
                xml.open("GET", s);
                xml.send(null);
            }
            let a = document.querySelector("body > div > div:nth-child(4) > a");
            let par = a.parentElement;
            a.remove();
            let input = document.createElement("input");
            input.type = "checkbox";
            par.appendChild(input);
            if (a.innerText !== "不喜欢") {
                input.checked = true;
            }
            input.addEventListener("change", (ev) => {
                console.log(input.checked);
                if (input.checked) {
                    if (a.innerText !== "不喜欢") {
                        request(a.href);
                    }
                }
                else {
                    if (a.innerText === "不喜欢") {
                        request(a.href);
                    }
                }
            });
        })();
    });
})();
