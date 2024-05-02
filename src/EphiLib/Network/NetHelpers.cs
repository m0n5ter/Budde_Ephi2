// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Network.NetHelpers
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.Net;

namespace Ephi.Core.Network;

public static class NetHelpers
{
    public static bool TryParseIpPort(string ipPort, out IPAddress ipAddress, out int port)
    {
        ipAddress = IPAddress.Any;
        port = 0;
        var strArray = ipPort.Split(new char[4]
        {
            ':',
            '|',
            ' ',
            ','
        }, StringSplitOptions.RemoveEmptyEntries);
        return strArray.Length == 2 && IPAddress.TryParse(strArray[0], out ipAddress) && int.TryParse(strArray[1], out port);
    }
}