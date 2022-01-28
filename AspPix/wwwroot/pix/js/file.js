(function () {
    document.addEventListener("DOMContentLoaded", function () {
        let upButton = document.getElementsByClassName("up");
        let downButton = document.getElementsByClassName("down");
        let getForm = document.getElementById("form");
        let pageCount = document.getElementById("down");
        let tagInput = document.getElementById("tag");
        let resetButton = document.getElementById("reset");
        let dateInput = document.getElementById("date");
        let date2Input = document.getElementById("date2");
        function tagChange() {
            pageCount.value = (0).toString();
            getForm.submit();
        }
        resetButton.onclick = function () {
            dateInput.value = "";
            date2Input.value = "";
        };
        tagInput.onchange = tagChange;
        dateInput.onchange = tagChange;
        date2Input.onchange = tagChange;
        function addButtonEvent(ie, func) {
            Array.from(ie, function (v) {
                v.onclick = func;
            });
        }
        addButtonEvent(upButton, function () {
            if (pageCount.value) {
                let count = Number(pageCount.value);
                if (count > 0) {
                    pageCount.value = (count - 1).toString();
                }
                getForm.submit();
            }
        });
        addButtonEvent(downButton, function () {
            if (pageCount.value) {
                let count = Number(pageCount.value);
                pageCount.value = (count + 1).toString();
                getForm.submit();
            }
        });
    });
})();
