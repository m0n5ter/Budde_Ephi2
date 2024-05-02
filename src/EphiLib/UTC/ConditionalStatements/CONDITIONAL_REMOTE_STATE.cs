// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.ConditionalStatements.CONDITIONAL_REMOTE_STATE
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll


namespace Ephi.Core.UTC.ConditionalStatements;

public enum CONDITIONAL_REMOTE_STATE : byte
{
    ERROR = 16, // 0x10
    VALID = 17, // 0x11
    RUNNING = 18, // 0x12
    RUNNING_PRECONDITIONS = 19, // 0x13
    FINISHED = 20, // 0x14
    CANCELLED = 21, // 0x15
    TIMED_OUT = 22, // 0x16
    DELETED = 23 // 0x17
}