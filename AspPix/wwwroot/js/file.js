(function () {
    document.addEventListener("DOMContentLoaded", function () {
        let up = document.getElementsByClassName("up");
        let down = document.getElementsByClassName("down");
        let f = document.getElementById("form");
        let n = document.getElementById("down");
        let tag = document.getElementById("tag");
        let select = document.getElementById("select");
        let reset = document.getElementById("reset");
        let d = document.getElementById("date");
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
        function add(ie, func) {
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
//# sourceMappingURL=file.js.map