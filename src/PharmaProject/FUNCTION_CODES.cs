// Decompiled with JetBrains decompiler
// Type: PharmaProject.FUNCTION_CODES
// Assembly: PharmaProject, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 8350C65F-EBA0-4076-AF7F-DF91D9FF4E2D
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaProject.exe


namespace PharmaProject
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
        STÖRVEKTOR = 100, // 0x00000064
        ANFORDERUNG_STÖRVEKTOR = 101 // 0x00000065
    }
}