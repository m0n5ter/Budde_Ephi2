// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.OUTPUT_ENFORCEMENT
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll


namespace Ephi.Core.UTC.ConditionalStatements;

public enum OUTPUT_ENFORCEMENT : byte
{
    ENF_ILLEGAL,
    ENF_UNTIL_CONDITION_TRUE,
    ENF_AT_CONDITION_TRUE,
    ENF_NEGATE_WHEN_TRUE
}