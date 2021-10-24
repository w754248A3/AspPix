(function () {
    document.addEventListener("DOMContentLoaded", function () {

        let upButton = <HTMLCollectionOf<HTMLButtonElement>>document.getElementsByClassName("up");

        let downButton = <HTMLCollectionOf<HTMLButtonElement>>document.getElementsByClassName("down");

        let getForm = <HTMLFormElement>document.getElementById("form");

        let pageCount = <HTMLInputElement>document.getElementById("down");

        let tagInput = <HTMLInputElement>document.getElementById("tag");

        let selectInput = <HTMLInputElement>document.getElementById("select");

        let resetButton = <HTMLInputElement>document.getElementById("reset");

        let dateInput = <HTMLInputElement>document.getElementById("date");


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
       

        function addButtonEvent<T>(ie: Iterable<T & HTMLButtonElement>, func: () => any) {

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