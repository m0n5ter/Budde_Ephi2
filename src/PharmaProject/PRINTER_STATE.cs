// Decompiled with JetBrains decompiler
// Type: PharmaProject.PRINTER_STATE
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe


namespace PharmaProject
{
    public enum PRINTER_STATE
    {
        PASSTHROUGH,
        WAITING_FOR_PACKAGE_PRN,
        WAITING_FOR_PACKAGE_NEXT_PRN,
        PRINTING,
        PRINTING_READY,
        APPLYING,
        LABEL_APPLIED
    }
}