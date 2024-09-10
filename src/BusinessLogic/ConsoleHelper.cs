// Decompiled with JetBrains decompiler
// Type: PharmaProject.Entry
// Assembly: BusinessLogic, Version=1.0.0.5, Culture=neutral, PublicKeyToken=null
// MVID: 9C9BA900-8C53-48F6-9DE6-D42367924779
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\BusinessLogic.dll

using System;

namespace PharmaProject
{
    public static class ConsoleHelper
    {
        public static void WriteLineColor(this string message, ConsoleColor color)
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = c;
        }
    }
}