// Decompiled with JetBrains decompiler
// Type: PharmaProject.Segments.SharedSlopeControl
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe

using System;

namespace PharmaProject.Segments
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
            if (canTransferChanged == null)
                return;
            canTransferChanged();
        }
    }
}