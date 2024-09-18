using SukiUI.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SukiUI.Toasts;

namespace PardofelisUI
{
    public class DynamicUIConfig
    {
        public static string AppName { get; set; } = "PardofelisUI";

        public static SukiDialogManager GlobalDialogManager = new();
        public static SukiToastManager GlobalToastManager = new();

    }
}
