using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PardofelisCore.Util;

public class ResultWrap
{
    public ResultWrap(bool status, string message)
    {
        Status=status;
        Message=message;
    }

    public bool Status { get; set; }
    public string Message { get; set; }
}

