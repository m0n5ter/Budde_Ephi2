// Decompiled with JetBrains decompiler
// Type: PharmaProject.Program
// Assembly: PharmaConsole, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 2768F44A-76B9-4E9B-A0B8-A2E7E7BBEED5
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\PharmaConsole.exe

using System;
using PharmaProject.BusinessLogic.Misc;

namespace PharmaProject
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var entry = new Entry();
            entry.Start();
            while (Console.ReadKey().Key != ConsoleKey.X)
            {
                Console.WriteLine("SEND");
                entry.location9.Set1Barcode("A");
                entry.location9.WmsSetDirection("A", WMS_TOTE_DIRECTION.DIRECTION_2, 0U);
            }

            do
            {
                ;
            } while (Console.ReadKey().Key != ConsoleKey.X);

            entry.Stop();
        }
    }
}