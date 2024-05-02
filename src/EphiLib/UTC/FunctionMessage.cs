// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.FunctionMessage
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System.Linq;

namespace Ephi.Core.UTC;

public class FunctionMessage
{
    private static byte newMessageId;
    public readonly UTC_FUNCTION Function;
    public readonly byte MsgId;
    public byte[] content;

    public FunctionMessage(UTC_FUNCTION function)
        : this(function, ++newMessageId > 0 ? newMessageId : ++newMessageId)
    {
    }

    public FunctionMessage(UTC_FUNCTION function, byte msgId)
    {
        content = new byte[0];
        Function = function;
        MsgId = msgId;
    }

    public byte[] GenerateMessage
    {
        get
        {
            return new byte[3]
            {
                2,
                MsgId,
                (byte)Function
            }.Concat(content).Concat(new byte[1]
            {
                3
            }).ToArray();
        }
    }

    public bool IsConditional => Function >= UTC_FUNCTION.C_SET_STATEMENT;
}