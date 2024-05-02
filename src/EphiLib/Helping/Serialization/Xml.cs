// Decompiled with JetBrains decompiler
// Type: Ephi.Core.Helping.Serialization.Xml
// Assembly: EphiLib, Version=4.0.0.8, Culture=neutral, PublicKeyToken=null
// MVID: E5F18B6C-CFEC-4D37-972F-1B0CEBD7C3AE
// Assembly location: D:\_Work\Budde\_Clients\Ephi\ConveyorService\EphiLib.dll

using System;
using System.IO;
using System.Xml.Serialization;

namespace Ephi.Core.Helping.Serialization;

public static class Xml
{
    public static Exception LastException { get; private set; }

    public static bool Serialize<T>(string filename, T obj)
    {
        try
        {
            LastException = null;
            filename = Path.GetFullPath(filename);
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            using (var fileStream = new FileStream(filename, FileMode.Create))
            {
                new XmlSerializer(typeof(T)).Serialize(fileStream, obj);
                fileStream.Close();
            }
        }
        catch (Exception ex)
        {
            LastException = ex;
            return false;
        }

        return true;
    }

    public static T Deserialize<T>(string filename)
    {
        try
        {
            LastException = null;
            filename = Path.GetFullPath(filename);
            using (var fileStream = new FileStream(filename, FileMode.Open))
            {
                return (T)new XmlSerializer(typeof(T)).Deserialize(new StreamReader(fileStream));
            }
        }
        catch (Exception ex)
        {
            LastException = ex;
            return default;
        }
    }
}