(function () {
    document.addEventListener("DOMContentLoaded", function () {


        let imgDiv = <HTMLDivElement>document.getElementById("imgDiv");

        let vs = JSON.parse(window.atob(imgDiv.getAttribute("my")));


        function addImg(vs: string[], n: number) {

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