using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NITW_Dialogue_Tool
{
    // SB: A lot of this loading code was taken/adopted from AssetStudio: https://github.com/Perfare/AssetStudio

    public static class BinaryReaderExtensions
    {
        public static string ReadStringToNull(this BinaryReader reader, int maxLength = 32767)
        {
            var bytes = new List<byte>();
            int count = 0;
            while (reader.BaseStream.Position != reader.BaseStream.Length && count < maxLength)
            {
                var b = reader.ReadByte();
                if (b == 0)
                {
                    break;
                }
                bytes.Add(b);
                count++;
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public static uint ReadUInt32BE(this BinaryReader reader)
        {
            var objectDataOffsetBytes = reader.ReadBytes(4);
            Array.Reverse(objectDataOffsetBytes);
            return BitConverter.ToUInt32(objectDataOffsetBytes, 0);
        }

        public static void AlignStream(this BinaryReader reader)
        {
            reader.AlignStream(4);
        }

        public static void AlignStream(this BinaryReader reader, int alignment)
        {
            var pos = reader.BaseStream.Position;
            var mod = pos % alignment;
            if (mod != 0)
            {
                reader.BaseStream.Position += alignment - mod;
            }
        }

        public static string ReadAlignedString(this BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length > 0 && length <= reader.BaseStream.Length - reader.BaseStream.Position)
            {
                var stringData = reader.ReadBytes(length);
                var result = Encoding.UTF8.GetString(stringData);
                reader.AlignStream(4);
                return result;
            }
            return "";
        }
    }

    public static class CommonString
    {
        public static readonly Dictionary<uint, string> StringBuffer = new Dictionary<uint, string>
        {
            {0, "AABB"},
            {5, "AnimationClip"},
            {19, "AnimationCurve"},
            {34, "AnimationState"},
            {49, "Array"},
            {55, "Base"},
            {60, "BitField"},
            {69, "bitset"},
            {76, "bool"},
            {81, "char"},
            {86, "ColorRGBA"},
            {96, "Component"},
            {106, "data"},
            {111, "deque"},
            {117, "double"},
            {124, "dynamic_array"},
            {138, "FastPropertyName"},
            {155, "first"},
            {161, "float"},
            {167, "Font"},
            {172, "GameObject"},
            {183, "Generic Mono"},
            {196, "GradientNEW"},
            {208, "GUID"},
            {213, "GUIStyle"},
            {222, "int"},
            {226, "list"},
            {231, "long long"},
            {241, "map"},
            {245, "Matrix4x4f"},
            {256, "MdFour"},
            {263, "MonoBehaviour"},
            {277, "MonoScript"},
            {288, "m_ByteSize"},
            {299, "m_Curve"},
            {307, "m_EditorClassIdentifier"},
            {331, "m_EditorHideFlags"},
            {349, "m_Enabled"},
            {359, "m_ExtensionPtr"},
            {374, "m_GameObject"},
            {387, "m_Index"},
            {395, "m_IsArray"},
            {405, "m_IsStatic"},
            {416, "m_MetaFlag"},
            {427, "m_Name"},
            {434, "m_ObjectHideFlags"},
            {452, "m_PrefabInternal"},
            {469, "m_PrefabParentObject"},
            {490, "m_Script"},
            {499, "m_StaticEditorFlags"},
            {519, "m_Type"},
            {526, "m_Version"},
            {536, "Object"},
            {543, "pair"},
            {548, "PPtr<Component>"},
            {564, "PPtr<GameObject>"},
            {581, "PPtr<Material>"},
            {596, "PPtr<MonoBehaviour>"},
            {616, "PPtr<MonoScript>"},
            {633, "PPtr<Object>"},
            {646, "PPtr<Prefab>"},
            {659, "PPtr<Sprite>"},
            {672, "PPtr<TextAsset>"},
            {688, "PPtr<Texture>"},
            {702, "PPtr<Texture2D>"},
            {718, "PPtr<Transform>"},
            {734, "Prefab"},
            {741, "Quaternionf"},
            {753, "Rectf"},
            {759, "RectInt"},
            {767, "RectOffset"},
            {778, "second"},
            {785, "set"},
            {789, "short"},
            {795, "size"},
            {800, "SInt16"},
            {807, "SInt32"},
            {814, "SInt64"},
            {821, "SInt8"},
            {827, "staticvector"},
            {840, "string"},
            {847, "TextAsset"},
            {857, "TextMesh"},
            {866, "Texture"},
            {874, "Texture2D"},
            {884, "Transform"},
            {894, "TypelessData"},
            {907, "UInt16"},
            {914, "UInt32"},
            {921, "UInt64"},
            {928, "UInt8"},
            {934, "unsigned int"},
            {947, "unsigned long long"},
            {966, "unsigned short"},
            {981, "vector"},
            {988, "Vector2f"},
            {997, "Vector3f"},
            {1006, "Vector4f"},
            {1015, "m_ScriptingClassIdentifier"},
            {1042, "Gradient"},
            {1051, "Type*"},
            {1057, "int2_storage"},
            {1070, "int3_storage"},
            {1083, "BoundsInt"},
            {1093, "m_CorrespondingSourceObject"},
            {1121, "m_PrefabInstance"},
            {1138, "m_PrefabAsset"}
        };
    }


    class UnityAssetFile
    {
        public enum EndianType
        {
            LittleEndian,
            BigEndian
        }

        private static int ReadSerializedType(BinaryReader reader, uint m_Version)   // SB: Using this so we can be in sync, but not actually storing the data
        {
            var classID = reader.ReadInt32();

            if (m_Version >= 16)
            {
                reader.ReadBoolean();
            }

            if (m_Version >= 17)
            {
                reader.ReadInt16();
            }

            if (m_Version >= 13)
            {
                if ((m_Version < 16 && classID < 0) || (m_Version >= 16 && classID == 114))
                {
                    reader.ReadBytes(16); //Hash128
                }
                reader.ReadBytes(16); //Hash128
            }

            return classID;
        }


        public static Dictionary<string,yarnFile> read(string assetsFilePath)
        {
            string assetFileName = Path.GetFileName(assetsFilePath);
            Form1.Log("Reading " + assetFileName + "...");

            Dictionary<string, yarnFile> yarnFiles = new Dictionary<string, yarnFile>();
            
            if (File.Exists(assetsFilePath))
            {
                if (IsFileLocked(assetsFilePath))
                {
                    MessageBox.Show("The file " + assetFileName + " is locked." + Environment.NewLine + Environment.NewLine + 
                        "Try closing any programs that could use it and if that fails try restarting your computer.",
                        "File Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Form1.Log("Failed to read " + assetFileName + " (locked)", true);
                    return yarnFiles;
                }
                
                using (BinaryReader reader = new BinaryReader(File.Open(assetsFilePath, FileMode.Open)))
                {
                    //read object data offset
                    //reader.BaseStream.Seek(12, SeekOrigin.Begin);
                    //byte[] objectDataOffsetBytes = reader.ReadBytes(4);
                    //Array.Reverse(objectDataOffsetBytes);
                    //uint objectDataOffset = BitConverter.ToUInt32(objectDataOffsetBytes, 0);

                    ////skip header
                    //reader.BaseStream.Seek(33, SeekOrigin.Begin);

                    ////find beginning of object info
                    //long position = 0;
                    //while (position == 0)
                    //{
                    //    if(reader.BaseStream.Position == reader.BaseStream.Length)
                    //    {
                    //        //asset file does not contain any objects, skip it
                    //        Form1.Log("Reading " + assetFileName + " skipped", true);
                    //        return yarnFiles;
                    //    }

                    //    if (reader.ReadByte() == 0x1)
                    //    {
                    //        for (int i = 0; i < 11; i++)
                    //        {
                    //            if (reader.BaseStream.Position == reader.BaseStream.Length)
                    //            {
                    //                //asset file does not contain any objects, skip it
                    //                Form1.Log("Reading " + assetFileName + " skipped", true);
                    //                return yarnFiles;
                    //            }
                    //            if (reader.ReadByte() != 0x0)
                    //            {
                    //                reader.BaseStream.Position -= 1;
                    //                break;
                    //            }
                    //            else if (i == 10)
                    //            {
                    //                position = reader.BaseStream.Position - 12 - 7;
                    //            }
                    //        }
                    //    }
                    //}

                    var m_MetadataSize = reader.ReadUInt32BE();
                    var m_FileSize = reader.ReadUInt32BE();
                    var m_Version = reader.ReadUInt32BE();
                    var objectDataOffset = reader.ReadUInt32BE();       // SB: Original name for this value

                    byte m_Endianess = 0;
                    byte[] m_Reserved = new byte[3];
                    EndianType m_FileEndianess;

                    if (m_Version >= 9)
                    {
                        m_Endianess = reader.ReadByte();
                        m_Reserved = reader.ReadBytes(3);
                        m_FileEndianess = (EndianType)m_Endianess;
                    }
                    else
                    {
                        reader.BaseStream.Seek(m_FileSize - m_MetadataSize, SeekOrigin.Begin);
                        m_FileEndianess = (EndianType)reader.ReadByte();
                    }

                    //ReadMetadata
                    if (m_FileEndianess != EndianType.LittleEndian)
                    {
                        // SB: Endianness not supported ... this could be implemented fairly easily though if needed, but it's not for NITW
                        throw new NotImplementedException("Currently only supporting little endian asset files");
                    }

                    string unityVersion;
                    if (m_Version >= 7)
                    {
                        unityVersion = reader.ReadStringToNull();
                        //SetVersion(unityVersion);     // SB: not really needed
                    }

                    if (m_Version >= 8)
                    {
                        reader.ReadInt32(); // SB: not really needed
                        //m_TargetPlatform = (BuildTarget)reader.ReadInt32();
                        //if (!Enum.IsDefined(typeof(BuildTarget), m_TargetPlatform))
                        //{
                        //    m_TargetPlatform = BuildTarget.UnknownPlatform;
                        //}
                    }

                    if (m_Version >= 13)
                    {
                        reader.ReadBoolean();   // SB: not really needed
                        //m_EnableTypeTree = reader.ReadBoolean();
                    }

                    //ReadTypes
                    int typeCount = reader.ReadInt32();
                    var m_Types = new List<int>();
                    for (int i = 0; i < typeCount; i++)
                    {
                        m_Types.Add(ReadSerializedType(reader, m_Version));     // SB: This is normally more complex than just the classID, but it's all we need in this case
                    }

                    if (m_Version >= 7 && m_Version < 14)
                    {
                        var bigIDEnabled = reader.ReadInt32();
                    }

                    //read object info
                    int objectInfoCount = reader.ReadInt32();
                    long objectInfoPosition = reader.BaseStream.Position;
                    for (int i = 0; i < objectInfoCount; i++)
                    {
                        if (reader.BaseStream.Position > reader.BaseStream.Length - 16) //hack
                        {
                            //too close to end of file
                            //break;

                            // SB: Going to throw an error... inconsistency is not to be tolerated!
                            throw new InvalidOperationException($"Unexpected end of asset file");
                        }
                        //long index = reader.ReadInt64();
                        //uint offset = reader.ReadUInt32();
                        //uint length = reader.ReadUInt32();


                        // SB NEW: Let's track the size of the object entry, rather than doing too many dodgy hardcodes
                        var objectEntryStart = reader.BaseStream.Position;

                        long index;
                        if (m_Version < 14)
                        {
                            index = reader.ReadInt32();
                        }
                        else
                        {
                            reader.AlignStream();
                            index = reader.ReadInt64();
                        }
                        var offset = reader.ReadUInt32();
                        var length = reader.ReadUInt32();
                        var typeID = reader.ReadInt32();
                        ushort classID;
                        if (m_Version < 16)
                        {
                            //objectInfo.classID = reader.ReadUInt16();
                            //objectInfo.serializedType = m_Types.Find(x => x.classID == objectInfo.typeID);
                            //var isDestroyed = reader.ReadUInt16();

                            classID = reader.ReadUInt16();
                            reader.ReadUInt16();
                        }
                        else
                        {
                            classID = (ushort)m_Types[typeID];
                        }

                        if (m_Version == 15 || m_Version == 16)
                        {
                            var stripped = reader.ReadByte();
                        }

                        var objectEntrySize = (int)(reader.BaseStream.Position - objectEntryStart);

                        if (classID == 49)   // 49 = TextAsset
                        {
                            long position = reader.BaseStream.Position;  // Back up position because we're going to jump

                            //check if object is yarn
                            if ((objectDataOffset + offset) < reader.BaseStream.Length)
                            {
                                reader.BaseStream.Seek(objectDataOffset + offset, SeekOrigin.Begin);
                                {
                                    string yarnFileName = reader.ReadAlignedString();
                                    if (yarnFileName.Contains(".yarn"))
                                    {
                                        Form1.Log("  Found " + yarnFileName);

                                        int yarnLength = reader.ReadInt32();

                                        if (yarnLength > 9999999) // SB: This was an arbitrary limit picked by the author, but we'll make it an error situation
                                        {
                                            throw new InvalidOperationException($"Got an unexpectedly huge size of {yarnLength}");
                                        }

                                        string yarnContent = Encoding.UTF8.GetString(reader.ReadBytes(yarnLength));

                                        if (yarnLength % 4 > 0)
                                        {
                                            reader.BaseStream.Seek(4 - (yarnLength % 4), SeekOrigin.Current);
                                        }

                                        uint yarnPathLength = reader.ReadUInt32();
                                        string yarnPath2 = "";
                                        if (yarnPathLength > 0)
                                        {
                                            yarnPath2 = Encoding.UTF8.GetString(reader.ReadBytes(yarnLength));
                                        }

                                        //save index, offset, length and write yarn to file
                                        string yarnDir = AppDomain.CurrentDomain.BaseDirectory + @"\yarn files";
                                        if (!Directory.Exists(yarnDir))
                                        {
                                            Directory.CreateDirectory(yarnDir);
                                        }

                                        string yarnPath = Path.Combine(yarnDir, yarnFileName) + ".txt";
                                        File.Create(yarnPath).Dispose();
                                        File.WriteAllText(yarnPath, yarnContent);
                                        DateTime lastModified = File.GetLastWriteTime(yarnPath);

                                        // SB NOTE: objectDataOffset, objectInfoCount, objectInfoPosition are constant,
                                        // not per-Yarn. But I see it's convenient for simple JSON objects... 
                                        // It still could probably be done a little cleaner with less duplication, but eh.
                                        yarnFiles.Add(yarnFileName, new yarnFile(assetsFilePath, index, offset, length, yarnFileName, yarnContent, yarnPath2, lastModified,
                                            objectDataOffset, objectInfoCount, objectInfoPosition, objectEntrySize));
                                    }
                                    
                                }
                                
                                // Restore position
                                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                            }
                        }
                    }
                }
            }
            else
            {
                Form1.Log("Failed to read " + assetFileName + " (not found)", true);
                return yarnFiles;
            }

            Form1.Log("Reading " + assetFileName + " done", true);

            return yarnFiles;
        }


        public static void write(string fileName, ref yarnDictionary rootz)
        {
            yarnFile f;
            if (!rootz.yarnFiles.TryGetValue(fileName, out f))
            {
                //yarn file not found
                Form1.Log("ERROR yarn file not found: " + fileName, true);
                return;
            }

            string assetFileName = Path.GetFileName(f.assetsFilePath);
            Form1.Log("Writing " + f.yarnFileName + " to " + assetFileName + "...");

            if (File.Exists(f.assetsFilePath))
            {
                if (IsFileLocked(f.assetsFilePath))
                {
                    MessageBox.Show("The file " + assetFileName + " is locked." + Environment.NewLine + Environment.NewLine + 
                        "Try closing any programs that could use it and if that fails try restarting your computer.",
                        "File Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Form1.Log("Failed to write " + assetFileName + " (locked)", true);
                    return;
                }
                                
                //calculate new yarn object length
                string yarnFilePath = AppDomain.CurrentDomain.BaseDirectory + @"\yarn files\" + f.yarnFileName + ".txt";
                
                if (IsFileLocked(yarnFilePath))
                {
                    MessageBox.Show("The file " + f.yarnFileName + " is locked." + Environment.NewLine + Environment.NewLine +
                        "Sublime is known to cause this problem, try editing with another text editor. Also close any programs that could use " + f.yarnFileName +
                        " and if that fails try restarting your computer.",
                        "File Locked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Form1.Log("Failed to write " + assetFileName + " (" + f.yarnFileName + " locked)", true);
                    return;
                }
                string newContent = File.ReadAllText(yarnFilePath);

                byte[] newContentBytes = Encoding.UTF8.GetBytes(newContent);
                byte[] pathBytes = Encoding.UTF8.GetBytes(f.yarnPath);

                uint newLength = 0;
                uint newLength_name = 0;
                uint newLength_namePadding = 0;
                uint newLength_content = 0;
                uint newLength_contentPadding = 0;
                uint newLength_path = 0;
                uint newLength_pathPadding = 0;

                newLength_name = 4 + (uint)f.yarnFileName.Length;
                if (newLength_name % 4 > 0)
                {
                    newLength_namePadding = 4 - (newLength_name % 4);
                }

                newLength_content = 4 + (uint)newContentBytes.Length;
                if (newLength_content % 4 > 0)
                {
                    newLength_contentPadding = 4 - (newLength_content % 4);
                }

                newLength_path = 4 + (uint)f.yarnPath.Length;
                if (newLength_path % 4 > 0)
                {
                    newLength_pathPadding = 4 - (newLength_path % 4);
                }

                newLength = newLength_name + newLength_namePadding;
                newLength += newLength_content + newLength_contentPadding;
                newLength += newLength_path + newLength_pathPadding;

                //calculate offset delta
                int offsetDelta = (int)newLength - (int)f.length;

                //change file length
                long nextObjectOriginalPosition = f.objectDataOffset + f.offset + f.length;
                long nextObjectNewPosition = nextObjectOriginalPosition + offsetDelta;
                long assetfileSize = new FileInfo(f.assetsFilePath).Length;
                long followingObjectsSize = assetfileSize - nextObjectOriginalPosition;
                long newAssetFileSize = assetfileSize + offsetDelta;
                                
                if (offsetDelta < 0)
                {
                    //yarn object is smaller now -> transpose yarn object following bytes and truncate file
                    FileOps.FileHelper.Transpose(f.assetsFilePath, nextObjectOriginalPosition, nextObjectNewPosition, followingObjectsSize);
                    FileOps.FileHelper.SetFileLen(f.assetsFilePath, newAssetFileSize);
                }                
                else if (offsetDelta > 0)
                {
                    //yarn object is bigger now -> transpose yarn object following bytes
                    FileOps.FileHelper.Transpose(f.assetsFilePath, nextObjectOriginalPosition, nextObjectNewPosition, followingObjectsSize);
                }

                //update object table with new length and new offsets for following objects
                List<uint> offsets = new List<uint>();

                // SB: Dodgy to leave this hardcoded but at least it's a constant by one name rather than magic numbers, right?
                // Use my newly added "f.objectEntrySize" for the size of the entry object as a whole...
                const int offsetToIndex = 0;    // SB: 8 bytes long (in file versions >= 14)    Won't work for file version < 14 (where this is Int32), but that doesn't apply to NITW
                const int offsetToOffset = offsetToIndex + 8;   // SB: 4 bytes long
                const int offsetToLength = offsetToOffset + 4;  // SB: 4 bytes long

                //put all object offsets in a list
                using (BinaryReader reader = new BinaryReader(File.Open(f.assetsFilePath, FileMode.Open)))
                {
                    reader.BaseStream.Seek(f.objectInfoPosition, SeekOrigin.Begin);
                    for (int i = 0; i < f.objectInfoCount; i++)
                    {
                        //skip uint64 index
                        reader.BaseStream.Seek(offsetToOffset, SeekOrigin.Current);

                        //save offsets for later
                        uint offset = reader.ReadUInt32();
                        offsets.Add(offset);

                        //skip rest of entry
                        var remainingBytes = f.objectEntrySize - offsetToLength;   // SB: still a little dodgy
                        reader.BaseStream.Seek(remainingBytes, SeekOrigin.Current);
                    }
                }



                //write new length and new offsets and new file size
                using (BinaryWriter writer = new BinaryWriter(File.Open(f.assetsFilePath, FileMode.Open)))
                {
                    //update filesize
                    writer.BaseStream.Seek(4, SeekOrigin.Begin);
                    byte[] newAssetFileSizeBytes = BitConverter.GetBytes((uint)newAssetFileSize);
                    Array.Reverse(newAssetFileSizeBytes);
                    writer.Write(newAssetFileSizeBytes);

                    long indexPosition = (f.index - 1) * f.objectEntrySize;     // SB: removed hardcoded value
                    writer.BaseStream.Seek(f.objectInfoPosition + indexPosition, SeekOrigin.Begin);

                    //update yarn length
                    {
                        writer.BaseStream.Seek(offsetToLength, SeekOrigin.Current);
                        writer.Write(newLength);

                        var remainingBytes = f.objectEntrySize - offsetToLength - 4;   // SB: still a little dodgy, additional -4 since we just wrote the length so we're passed the length
                        writer.BaseStream.Seek(remainingBytes, SeekOrigin.Current);
                    }

                    //update all offsets behind f.index
                    for (int i = (int)(f.index); i < f.objectInfoCount; i++)
                    {
                        uint newOffset = (uint)(offsets[i] + offsetDelta);

                        writer.BaseStream.Seek(offsetToOffset, SeekOrigin.Current);
                        writer.Write(newOffset);

                        var remainingBytes = f.objectEntrySize - offsetToLength;   // SB: still a little dodgy
                        writer.BaseStream.Seek(remainingBytes, SeekOrigin.Current);
                    }

                    //update yarn content
                    writer.BaseStream.Seek(f.objectDataOffset + f.offset + 4 + f.yarnFileName.Length + newLength_namePadding, SeekOrigin.Begin);
                    writer.Write((uint)(newContentBytes.Length));


                    writer.Write(newContentBytes);
                    if (newContentBytes.Length > 0)
                    {
                        int paddingTemp = 0;
                        if (newContentBytes.Length % 4 > 0)
                        {
                            paddingTemp = 4 - (newContentBytes.Length % 4);
                        }

                        for (int i = 0; i < paddingTemp; i++)
                        {
                            writer.Write((byte)0);
                        }
                    }

                    writer.BaseStream.Position = writer.BaseStream.Position;

                    writer.Write((uint)f.yarnPath.Length);
                    writer.Write(pathBytes);
                    if (pathBytes.Length > 0)
                    {
                        int paddingTemp = 0;
                        if (pathBytes.Length % 4 > 0)
                        {
                            paddingTemp = 4 - (pathBytes.Length % 4);
                        }

                        for (int i = 0; i < paddingTemp; i++)
                        {
                            writer.Write((byte)0);
                        }
                    }
                }

                Form1.Log("Writing " + f.yarnFileName + " to " + assetFileName + " done", true);

                //update yarn dictionary
                f.lastModified = File.GetLastWriteTime(yarnFilePath);
                f.length = newLength;
                f.edited = true;
                rootz.yarnFiles[f.yarnFileName] = f;
                JsonUtil.saveYarnDictionary(rootz);
            }
            else
            {
                Form1.Log("Failed to write " + assetFileName + " (not found)", true);
                return;
            }
        }

        private static bool IsFileLocked(string filePath)
        {
            FileStream stream = null;

            FileInfo file = new FileInfo(filePath);

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}
