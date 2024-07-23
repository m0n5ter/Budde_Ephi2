// Decompiled with JetBrains decompiler
// Type: PharmaProject.BusinessLogic.Wms_Communication.Messages.BaseMessage
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;
using System.Collections.Generic;
using System.Linq;
using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject.BusinessLogic.Wms_Communication.Messages
{
    public class BaseMessage
    {
        private static uint messageCounter;
        public uint counter;
        public uint functionCode;
        private byte[] functionData;
        public uint location;

        protected BaseMessage(uint counter, uint functionCode, uint location)
        {
            this.counter = counter;
            this.functionCode = functionCode;
            this.location = location;
            FunctionData = new byte[116];
        }

        public byte[] FunctionData
        {
            get => functionData;
            set
            {
                var byteList = value.Length <= 116 ? new List<byte>(value) : throw new ArgumentOutOfRangeException("Function data is too large");
                byteList.AddRange(new byte[116 - value.Length]);
                functionData = byteList.ToArray();
            }
        }

        public static uint GetMessageCounter()
        {
            return messageCounter;
        }

        public static uint GetAndIncrementMessageCounter()
        {
            return messageCounter++;
        }

        public static BaseMessage MakeEmpty()
        {
            var baseMessage = new BaseMessage(0U, 0U, 0U);
            baseMessage.FunctionData = new byte[116];
            Array.Clear(baseMessage.FunctionData, 0, baseMessage.FunctionData.Length);
            return baseMessage;
        }

        public static BaseMessage ByteArrayToMessage(byte[] buffer)
        {
            var numArray = buffer.Length == 128 ? buffer.Take(4).ToArray() : throw new ArgumentOutOfRangeException(nameof(buffer), "Buffer size not correct");
            var array1 = buffer.Skip(4).Take(4).ToArray();
            var array2 = buffer.Skip(8).Take(4).ToArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(numArray);
                Array.Reverse(array1);
                Array.Reverse(array2);
            }

            BaseMessage message;
            switch ((FUNCTION_CODES)BitConverter.ToUInt32(array1, 0))
            {
                case FUNCTION_CODES.ANMELDUNG_PACKSTÜCK:
                    message = new AnmeldungPackstück(0U, 0U, new byte[32], 0U, 0U, 0U);
                    break;
                case FUNCTION_CODES.ANWEISUNG_PACKSTÜCK:
                    message = new AnweisungPackstück(0U, 0U, new byte[116]);
                    break;
                case FUNCTION_CODES.RÜCKMELDUNG_PACKSTÜCK:
                    message = new RückmeldungPackstück(WMS_TOTE_DIRECTION.DIRECTION_1, new byte[32], 0U);
                    break;
                case FUNCTION_CODES.ANMELDUNG_LABELDRUCK:
                    message = new AnmeldungLabeldruck(false, false, false, false, new byte[32], 0U);
                    break;
                case FUNCTION_CODES.FEHLER_LABELDRUCK:
                    message = new FehlerLabeldruck(0U, 0U, new byte[116]);
                    break;
                case FUNCTION_CODES.LABELDRUCK_ERFOLGREICH:
                    message = new LabeldruckErfolgreich(0U, new byte[116], 0U);
                    break;
                case FUNCTION_CODES.AUFBRINGEN_LABEL:
                    message = new AufbringenLabel(false, new byte[32], 0U);
                    break;
                case FUNCTION_CODES.LABEL_CHECK_RESPONSE:
                    message = new LabelCheckResponse(0U, 0U, new byte[116]);
                    break;
                case FUNCTION_CODES.ANFORDERUNG_PACKMITTEL:
                    message = new AnforderungPackmittel(0U, 0U, new byte[108]);
                    break;
                case FUNCTION_CODES.STÖRVEKTOR:
                    message = new Störvektor(new byte[100]);
                    break;
                default:
                    message = MakeEmpty();
                    break;
            }

            message.counter = BitConverter.ToUInt32(numArray, 0);
            message.functionCode = BitConverter.ToUInt32(array1, 0);
            message.location = BitConverter.ToUInt32(array2, 0);
            Buffer.BlockCopy(buffer, 12, message.FunctionData, 0, message.FunctionData.Length);
            return message;
        }

        public static byte[] MessageToByteArray(BaseMessage message)
        {
            var bytes1 = BitConverter.GetBytes(message.counter);
            var bytes2 = BitConverter.GetBytes(message.functionCode);
            var bytes3 = BitConverter.GetBytes(message.location);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes1);
                Array.Reverse(bytes2);
                Array.Reverse(bytes3);
            }

            var dst = new byte[bytes1.Length + bytes2.Length + bytes3.Length + message.FunctionData.Length];
            Buffer.BlockCopy(bytes1, 0, dst, 0, bytes1.Length);
            Buffer.BlockCopy(bytes2, 0, dst, 4, bytes2.Length);
            Buffer.BlockCopy(bytes3, 0, dst, 8, bytes3.Length);
            Buffer.BlockCopy(message.FunctionData, 0, dst, 12, message.FunctionData.Length);
            return dst;
        }

        protected byte[] TakeBytesFromFunctionData(int from, int count)
        {
            var array = functionData.Skip(from).Take(count).ToArray();
            if (BitConverter.IsLittleEndian && from != 8 && count != 32)
                Array.Reverse(array);
            return array;
        }
    }
}