// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Lists.Fifo`1
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System.Linq;

namespace Ephi.Core.Helping.Lists;

public class Fifo<T> : BaseList<T>
{
    public override T Peek
    {
        get
        {
            lock (list)
            {
                return list.FirstOrDefault();
            }
        }
    }

    public override T Pop
    {
        get
        {
            lock (list)
            {
                var peek = Peek;
                if (!Empty)
                    list.RemoveAt(0);
                return peek;
            }
        }
    }
}