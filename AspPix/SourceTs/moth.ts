
(function () {

    document.addEventListener("DOMContentLoaded", function () {

        let upButton = <HTMLCollectionOf<HTMLButtonElement>>document.getElementsByClassName("up");

        let downButton = <HTMLCollectionOf<HTMLButtonElement>>document.getElementsByClassName("down");


        let up = <HTMLFormElement>document.getElementById("form_up");
        let down = <HTMLFormElement>document.getElementById("form_down");



        function addEvent(ie: typeof upButton, func: typeof upButton[0]["onclick"]) {

            Array.from(ie, (e) => e.onclick = func);


        }



        addEvent(upButton, () => up.submit());
        addEvent(downButton, () => down.submit());
    });
})();