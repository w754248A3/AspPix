(function () {
    document.addEventListener("DOMContentLoaded", function () {
        let imgDiv = document.getElementById("imgDiv");
        let vs = JSON.parse(imgDiv.getAttribute("my"));
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
    });
})();
