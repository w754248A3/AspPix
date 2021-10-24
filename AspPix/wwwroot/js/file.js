(function () {
    document.addEventListener("DOMContentLoaded", function () {
        let upButton = document.getElementsByClassName("up");
        let downButton = document.getElementsByClassName("down");
        let getForm = document.getElementById("form");
        let pageCount = document.getElementById("down");
        let tagInput = document.getElementById("tag");
        let selectInput = document.getElementById("select");
        let resetButton = document.getElementById("reset");
        let dateInput = document.getElementById("date");
        function tagChange() {
            pageCount.value = (0).toString();
            getForm.submit();
        }
        resetButton.onclick = function () {
            dateInput.value = "";
        };
        tagInput.onchange = tagChange;
        selectInput.onchange = function () {
            tagInput.value = selectInput.value;
            tagChange();
        };
        dateInput.onchange = tagChange;
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
