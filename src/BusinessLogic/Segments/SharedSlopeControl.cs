// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Segments.SharedSlopeControl
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;

namespace PharmaProject.BusinessLogic.Segments
{
    public class SharedSlopeControl
    {
        public Action CanTransferChanged;
        public Func<bool> CanTransferPtr;

        public bool CanTransfer
        {
            get
            {
                var canTransferPtr = CanTransferPtr;
                return canTransferPtr != null && canTransferPtr();
            }
        }

        public void RaiseCanTransferChanged()
        {
            var canTransferChanged = CanTransferChanged;
            canTransferChanged?.Invoke();
        }
    }
}