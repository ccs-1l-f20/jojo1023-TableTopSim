

window.openFileDialog = function (id) {
    $(id).trigger("click");
};

window.SetFocusTo = (element) => {
    element.focus();
};

window.FillCanvas = (canvas) => {
    canvas.style.width = '100%';
    canvas.style.height = '100%';

    return [canvas.offsetWidth, canvas.offsetHeight];
};
window.SetCanvasSize = (canvas, width, height) => {
    canvas.style.width = width + "px";
    canvas.style.height = height + "px";
    //canvas.width = canvas.offsetWidth;
    //canvas.height = canvas.offsetHeight;
    //return [canvas.offsetWidth, canvas.offsetHeight];
};
MyDOMGetBoundingClientRect = (element) => { return element.getBoundingClientRect(); };
