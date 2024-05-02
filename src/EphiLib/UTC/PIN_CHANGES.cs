// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.PIN_CHANGES
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll


namespace Ephi.Core.UTC;

public enum PIN_CHANGES : byte
{
    NONE = 1,
    OUTPUT = 2,
    INPUT = 4,
    INPUT_OVERRIDES = 8,
    HARD_EMRG = 16, // 0x10
    SHORT_CIRCUITED = 32, // 0x20
    SOFT_EMERGENCIES = 64, // 0x40
    SOFT_EMERGENCY_EXT = 128 // 0x80
}