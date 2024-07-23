// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.FehlerLabeldruck
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll


using System;
using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
{
    internal class LabelCheckResponse : BaseMessage
    {
        public LabelCheckResponse(uint counter, uint location, byte[] functionData)
            : base(counter, (uint)FUNCTION_CODES.LABEL_CHECK_RESPONSE, location)
        {
            FunctionData = functionData;
        }

        public bool ShouldProceed => BitConverter.ToUInt32(TakeBytesFromFunctionData(0, 4), 0) != 0;
    }
}