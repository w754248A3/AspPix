(function () {
    document.addEventListener("DOMContentLoaded", function () {

        let upButton = <HTMLCollectionOf<HTMLButtonElement>>document.getElementsByClassName("up");

        let downButton = <HTMLCollectionOf<HTMLButtonElement>>document.getElementsByClassName("down");

        let getForm = <HTMLFormElement>document.getElementById("form");

        let pageCount = <HTMLInputElement>document.getElementById("down");

        let tagInput = <HTMLInputElement>document.getElementById("tag");

        let resetButton = <HTMLInputElement>document.getElementById("reset");

        let dateInput = <HTMLInputElement>document.getElementById("date");
        let date2Input = <HTMLInputElement>document.getElementById("date2");


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