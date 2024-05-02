// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Misc.FUNCTION_CODES
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll


namespace PharmaProject.BusinessLogic.Misc
{
    public enum FUNCTION_CODES : uint
    {
        ACKNOWLEDGE = 0,
        ANMELDUNG_PACKSTÜCK = 1,
        ANWEISUNG_PACKSTÜCK = 2,
        RÜCKMELDUNG_PACKSTÜCK = 3,
        ANMELDUNG_LABELDRUCK = 40, // 0x00000028
        FEHLER_LABELDRUCK = 41, // 0x00000029
        LABELDRUCK_ERFOLGREICH = 43, // 0x0000002B
        AUFBRINGEN_LABEL = 44, // 0x0000002C
        ANFORDERUNG_PACKMITTEL = 60, // 0x0000003C
        IPUNKT_AUSSCHALTEN = 61, // 0x0000003D
        STÖRVEKTOR = 100, // 0x00000064
        ANFORDERUNG_STÖRVEKTOR = 101 // 0x00000065
    }
}