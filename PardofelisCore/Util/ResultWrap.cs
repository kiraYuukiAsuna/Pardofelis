using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PardofelisCore.Util;

public class ResultWrap<T>
{
    public ResultWrap(bool status, T message)
    {
        Status=status;
        Message=message;
    }

    public bool Status { get; set; }
    public T Message { get; set; }
}

