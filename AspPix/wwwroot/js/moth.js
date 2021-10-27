(function () {
    document.addEventListener("DOMContentLoaded", function () {
        let upButton = document.getElementsByClassName("up");
        let downButton = document.getElementsByClassName("down");
        let up = document.getElementById("form_up");
        let down = document.getElementById("form_down");
        function addEvent(ie, func) {
            Array.from(ie, (e) => e.onclick = func);
        }
        addEvent(upButton, () => up.submit());
        addEvent(downButton, () => down.submit());
    });
})();
