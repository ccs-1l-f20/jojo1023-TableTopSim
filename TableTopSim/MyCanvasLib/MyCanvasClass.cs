using Blazor.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyCanvasLib
{
    public class MyCanvasClass : ComponentBase
    {
        [Parameter]
        public bool Visible { get; set; } = true;
        [Parameter]
        public long Height { get; set; }

        [Parameter]
        public long Width { get; set; }

        public readonly string Id = Guid.NewGuid().ToString();
        protected ElementReference _canvasRef;

        public ElementReference CanvasReference { get => this._canvasRef; }

        [Inject]
        internal IJSRuntime JSRuntime { get; set; }
    }
}
