using Newtonsoft.Json;
using NITW_Dialogue_Tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class JsonUtil
{
    public static void saveYarnDictionary(yarnDictionary rootz)
    {
        string json = JsonConvert.SerializeObject(rootz);
        json = JsonUtil.JsonPrettify(json);
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yarnFiles.json"), json);
    }

    public static yarnDictionary loadYarnDictionary()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yarnFiles.json");
        yarnDictionary rootz = null;

        if (File.Exists(path))
        {
            // SB: I've added a new, critical data field that determines the size of object entries
            // in the asset file (previously this was hardcoded), which absolutely needs to be known
            // before we can safely process... due to this change, we can't keep data that doesn't
            // have this value available (the user will need to re-run initial setup.)
            rootz = JsonConvert.DeserializeObject<yarnDictionary>(File.ReadAllText(path));

            if(rootz.yarnFiles.Any(y => y.Value.objectEntrySize == 0))
            {
                // This is an old JSON; we can't use it, so just trash it
                File.Delete(path);
                rootz = null;
            }
        }
        
        if(rootz == null)
        {
            rootz = new yarnDictionary();
            rootz.yarnFiles = new Dictionary<string, yarnFile>();
        }

        return rootz;
    }

    public static string JsonPrettify(string json)
    {
        using (var stringReader = new StringReader(json))
        using (var stringWriter = new StringWriter())
        {
            var jsonReader = new JsonTextReader(stringReader);
            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
            jsonWriter.WriteToken(jsonReader);
            return stringWriter.ToString();
        }
    }
}