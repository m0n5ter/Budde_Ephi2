// Decompiled with JetBrains decompiler
// Type: Ephi.Core.UTC.UTC_FUNCTION
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll


namespace Ephi.Core.UTC;

public enum UTC_FUNCTION : byte
{
    UNINITIALIZED = 0,
    HEARTBEAT = 1,
    CONFIRM = 2,
    VERSION = 3,
    REQUEST_STATE = 17, // 0x11
    CURRENT_STATE = 18, // 0x12
    INPUT_UPDATE = 32, // 0x20
    CONDITIONAL_FORCED_UPDATE = 33, // 0x21
    SET_CONFIGURATION = 48, // 0x30
    SET_HEARTBEAT_TIMEOUT = 49, // 0x31
    C_SET_STATEMENT = 64, // 0x40
    C_SET_MACRO = 65, // 0x41
    C_SET_BATCH = 66, // 0x42
    C_SET_MEASUREMENT = 67, // 0x43
    C_ACTIVATE = 80, // 0x50
    C_ACTIVATE_TIMEOUT = 81, // 0x51
    C_DEACTIVATE = 82, // 0x52
    C_DEACTIVATE_ALL = 83, // 0x53
    C_DELETE = 84, // 0x54
    C_DELETE_ALL = 85, // 0x55
    C_STATE_UPDATE = 96, // 0x60
    C_REQ_STATES = 97, // 0x61
    C_REQ_COUNT = 98, // 0x62
    C_CURRENT_COUNT = 99, // 0x63
    C_MEASURE_RESULT = 100, // 0x64
    LAST = 101 // 0x65
}