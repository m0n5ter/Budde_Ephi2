// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.INPUT_CONDITION_TYPE
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll


namespace Ephi.Core.UTC.ConditionalStatements;

public enum INPUT_CONDITION_TYPE : byte
{
    GLOBAL_TIMEOUT = 1,
    START_BLOCK = 2,
    END_BLOCK = 3,
    SET_PIN = 4,
    PIN_CONDITION = 6,
    TIMEOUT_CONDITION = 7
}