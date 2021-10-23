(function () {
    document.addEventListener("DOMContentLoaded", function () {

        let up = <HTMLCollectionOf<HTMLButtonElement>>document.getElementsByClassName("up");

        let down = <HTMLCollectionOf<HTMLButtonElement>>document.getElementsByClassName("down");

        let f = <HTMLFormElement>document.getElementById("form");

        let n = <HTMLInputElement>document.getElementById("down");

        let tag = <HTMLInputElement>document.getElementById("tag");

        let select = <HTMLInputElement>document.getElementById("select");

        let reset = <HTMLInputElement>document.getElementById("reset");

        let d = <HTMLInputElement>document.getElementById("date");



        reset.onclick = function () {


            d.value = "";
        };

        tag.onchange = function () {

            n.value = (0).toString();
        };


        select.onchange = function () {
            n.value = (0).toString();
            tag.value = select.value;
        };


        function add(ie: HTMLCollectionOf<HTMLButtonElement>, func : ()=> any) {

            Array.from(ie, function (v) {

                v.onclick = func;
            });
        }




        add(up, function () {

            if (n.value) {

                let count = Number(n.value);

                if (count > 0) {

                    n.value = (count - 1).toString();

                    f.submit();
                }


            }
        });


        add(down, function () {
            if (n.value) {

                let count = Number(n.value);

                n.value = (count + 1).toString();


                f.submit();


            }
        });
    });


})();