using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Business._Tool
{
    public static class StreamDexTools
    {
        public static List<T> DeepClone<T>(List<T> source, JsonSerializerOptions options)
        {
            var json = JsonSerializer.Serialize(source, options);
            return JsonSerializer.Deserialize<List<T>>(json, options)!;
        }
    }
}