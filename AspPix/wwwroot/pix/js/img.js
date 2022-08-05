(function () {
    document.addEventListener("DOMContentLoaded", function () {
        let imgDiv = document.getElementById("imgDiv");
        let vs = JSON.parse(imgDiv.getAttribute("my"));
        function as() {
        }
        function addImg(vs) {
            if (vs.length !== 0) {
                let uri = vs.shift();
                let img = document.createElement("img");
                img.onload = () => {
                    if (img.width !== 0 && img.height !== 0) {
                        imgDiv.appendChild(img);
                        addImg(vs);
                    }
                };
                img.src = uri;
            }
        }
        addImg(vs);
    });
})();
