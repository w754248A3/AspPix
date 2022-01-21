(function () {
    document.addEventListener("DOMContentLoaded", function () {


        let imgDiv = <HTMLDivElement>document.getElementById("imgDiv");

        let vs = JSON.parse(imgDiv.getAttribute("my"));


        function addImg(vs: string[]) {

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