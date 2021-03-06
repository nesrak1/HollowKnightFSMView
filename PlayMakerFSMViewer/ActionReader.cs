﻿using AssetsTools.NET;
using PlayMakerFSMViewer.FieldClasses;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssetsTools.NET.Extra;

namespace PlayMakerFSMViewer
{
    public class ActionReader
    {
        public static string[] ActionValues(AssetTypeValueField actionData, AssetsFileInstance inst, int version)
        {
            AssetTypeValueField paramDataType = actionData.Get("paramDataType");
            int paramCount = paramDataType.GetValue().AsArray().size;
            byte[] byteData = GetByteData(actionData.Get("byteData"));
            MemoryStream stream = new MemoryStream(byteData);
            BinaryReader reader = new BinaryReader(stream);
            string[] actionValues = new string[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                ParamDataType type = (ParamDataType)paramDataType.Get(i).GetValue().AsInt();
                int paramDataPos = actionData.Get("paramDataPos").Get(i).GetValue().AsInt();
                int paramByteDataSize = actionData.Get("paramByteDataSize").Get(i).GetValue().AsInt();
                reader.BaseStream.Position = paramDataPos;
                string displayValue = GetDisplayValue(actionData, inst, version, type, paramDataPos, paramByteDataSize, reader);
                actionValues[i] = displayValue;
            }
            reader.Close();
            stream.Close();
            return actionValues;
        }

        public static string GetDisplayValue(AssetTypeValueField actionData, AssetsFileInstance inst, int version,
            ParamDataType type, int paramDataPos, int paramByteDataSize, BinaryReader reader)
        {
            string displayValue = "[?] " + type;
            if (version == 1 && !(type == ParamDataType.FsmString && paramByteDataSize == 0)) //read binary as normal
            {
                switch (type)
                {
                    case ParamDataType.Integer:
                    case ParamDataType.FsmInt:
                    case ParamDataType.FsmEnum:
                    {
                        displayValue = reader.ReadInt32().ToString();
                        break;
                    }
                    case ParamDataType.Enum:
                    {
                        displayValue = "Enum " + reader.ReadInt32().ToString();
                        break;
                    }
                    case ParamDataType.Boolean:
                    case ParamDataType.FsmBool:
                    {
                        displayValue = reader.ReadBoolean().ToString().ToLower();
                        break;
                    }
                    case ParamDataType.Float:
                    case ParamDataType.FsmFloat:
                    {
                        displayValue = reader.ReadSingle().ToString();
                        break;
                    }
                    case ParamDataType.String:
                    case ParamDataType.FsmString:
                    case ParamDataType.FsmEvent:
                    {
                        displayValue = Encoding.UTF8.GetString(reader.ReadBytes(paramByteDataSize));
                        break;
                    }
                    case ParamDataType.Vector2:
                    case ParamDataType.FsmVector2:
                    {
                        string x = reader.ReadSingle().ToString();
                        string y = reader.ReadSingle().ToString();
                        displayValue = x + ", " + y;
                        break;
                    }
                    case ParamDataType.Vector3:
                    case ParamDataType.FsmVector3:
                    {
                        string x = reader.ReadSingle().ToString();
                        string y = reader.ReadSingle().ToString();
                        string z = reader.ReadSingle().ToString();
                        displayValue = x + ", " + y + ", " + z;
                        break;
                    }
                }
                if (PossiblyHasName(type))
                {
                    int length = (paramByteDataSize + paramDataPos) - (int)reader.BaseStream.Position;
                    if (length > 0)
                    {
                        byte hasName = reader.ReadByte();
                        if (hasName == 0x01)
                        {
                            string varName = Encoding.UTF8.GetString(reader.ReadBytes(length - 1));
                            if (varName != "")
                            {
                                displayValue = varName;
                            }
                        }
                    }
                }
            }
            else //read from fsmXXXParams
            {
                AssetTypeValueField field = null;
                switch (type)
                {
                    case ParamDataType.Integer:
                    {
                        displayValue = reader.ReadInt32().ToString();
                        break;
                    }
                    case ParamDataType.Enum:
                    {
                        displayValue = "Enum " + reader.ReadInt32().ToString();
                        break;
                    }
                    case ParamDataType.Boolean:
                    {
                        displayValue = reader.ReadBoolean().ToString().ToLower();
                        break;
                    }
                    case ParamDataType.Float:
                    {
                        displayValue = reader.ReadSingle().ToString();
                        break;
                    }
                    case ParamDataType.String:
                    {
                        displayValue = Encoding.UTF8.GetString(reader.ReadBytes(paramByteDataSize));
                        break;
                    }
                    case ParamDataType.Vector2:
                    {
                        string x = reader.ReadSingle().ToString();
                        string y = reader.ReadSingle().ToString();
                        displayValue = x + ", " + y;
                        break;
                    }
                    case ParamDataType.Vector3:
                    {
                        string x = reader.ReadSingle().ToString();
                        string y = reader.ReadSingle().ToString();
                        string z = reader.ReadSingle().ToString();
                        displayValue = x + ", " + y + ", " + z;
                        break;
                    }

                    case ParamDataType.FsmInt:
                    {
                        field = actionData.Get("fsmIntParams").Get(paramDataPos);
                        displayValue = field.Get("value").GetValue().AsInt().ToString();
                        break;
                    }
                    case ParamDataType.FsmEnum:
                    {
                        field = actionData.Get("fsmEnumParams").Get(paramDataPos);
                        string intValue = field.Get("intValue").GetValue().AsInt().ToString();
                        string enumName = field.Get("enumName").GetValue().AsString().ToString();
                        displayValue = $"{intValue} ({enumName})";
                        break;
                    }
                    case ParamDataType.FsmBool:
                    {
                        field = actionData.Get("fsmBoolParams").Get(paramDataPos);
                        displayValue = field.Get("value").GetValue().AsBool().ToString();
                        break;
                    }
                    case ParamDataType.FsmFloat:
                    {
                        field = actionData.Get("fsmFloatParams").Get(paramDataPos);
                        displayValue = field.Get("value").GetValue().AsFloat().ToString();
                        break;
                    }
                    case ParamDataType.FsmString:
                    {
                        field = actionData.Get("fsmStringParams").Get(paramDataPos);
                        displayValue = field.Get("value").GetValue().AsString();
                        break;
                    }
                    case ParamDataType.FsmEvent:
                    {
                        field = actionData.Get("stringParams").Get(paramDataPos);
                        displayValue = field.GetValue().AsString();
                        break;
                    }
                    case ParamDataType.FsmVector2:
                    {
                        field = actionData.Get("fsmVector2Params").Get(paramDataPos);
                        AssetTypeValueField value = field.Get("value");
                        string x = value.Get("x").GetValue().AsFloat().ToString();
                        string y = value.Get("y").GetValue().AsFloat().ToString();
                        displayValue = x + ", " + y;
                        break;
                    }
                    case ParamDataType.FsmVector3:
                    {
                        field = actionData.Get("fsmVector3Params").Get(paramDataPos);
                        AssetTypeValueField value = field.Get("value");
                        string x = value.Get("x").GetValue().AsFloat().ToString();
                        string y = value.Get("y").GetValue().AsFloat().ToString();
                        string z = value.Get("z").GetValue().AsFloat().ToString();
                        displayValue = x + ", " + y + ", " + z;
                        break;
                    }
                    case ParamDataType.FsmQuaternion:
                    {
                        field = actionData.Get("fsmVector3Params").Get(paramDataPos);
                        AssetTypeValueField value = field.Get("value");
                        string x = value.Get("x").GetValue().AsFloat().ToString();
                        string y = value.Get("y").GetValue().AsFloat().ToString();
                        string z = value.Get("z").GetValue().AsFloat().ToString();
                        string w = value.Get("w").GetValue().AsFloat().ToString();
                        displayValue = x + ", " + y + ", " + z + ", " + w;
                        break;
                    }
                    default:
                    {
                        displayValue = "unknown type " + type.ToString();
                        break;
                    }
                }
                if (PossiblyHasName(type) && UseVariable(field))
                {
                    string varName = field.Get("name").GetValue().AsString();
                    if (varName != "")
                    {
                        displayValue = varName;
                    }
                }
            }
            //either version
            switch (type)
            {
                case ParamDataType.FsmGameObject:
                case ParamDataType.FsmOwnerDefault:
                {
                    AssetTypeValueField gameObject;
                    if (type == ParamDataType.FsmOwnerDefault)
                    {
                        AssetTypeValueField fsmOwnerDefaultParam = actionData.Get("fsmOwnerDefaultParams").Get(paramDataPos);

                        if (fsmOwnerDefaultParam["ownerOption"].GetValue().AsInt() == 0)
                        {
                            displayValue = "FSM Owner";
                            break;
                        }
                        
                        gameObject = fsmOwnerDefaultParam.Get("gameObject");
                    }
                    else
                    {
                        gameObject = actionData.Get("fsmGameObjectParams").Get(paramDataPos);
                    }
                    string name = gameObject.Get("name").GetValue().AsString();
                    AssetTypeValueField value = gameObject.Get("value");
                    int m_FileID = value.Get("m_FileID").GetValue().AsInt();
                    long m_PathID = value.Get("m_PathID").GetValue().AsInt64();
                    if (name == "")
                        name += GetAssetNameFast(m_FileID, m_PathID, inst);
                    displayValue = name;
                    if (m_PathID != 0)
                    {
                        if (name != "")
                            displayValue += " ";
                        displayValue += $"[{m_FileID},{m_PathID}]";
                    }
                    break;
                }
                case ParamDataType.FsmObject:
                {
                    AssetTypeValueField fsmObjectParam = actionData.Get("fsmObjectParams").Get(paramDataPos);
                    string name = fsmObjectParam.Get("name").GetValue().AsString();
                    string typeName = fsmObjectParam.Get("typeName").GetValue().AsString();

                    if (typeName.Contains("."))
                        typeName = typeName.Substring(typeName.LastIndexOf(".") + 1);

                    AssetTypeValueField value = fsmObjectParam.Get("value");
                    int m_FileID = value.Get("m_FileID").GetValue().AsInt();
                    long m_PathID = value.Get("m_PathID").GetValue().AsInt64();

                    displayValue = "";

                    if (name == "")
                        name += GetAssetNameFast(m_FileID, m_PathID, inst);

                    if (typeName != "")
                        displayValue += "(" + typeName + ")";

                    if (name != "")
                        displayValue += " " + name;

                    if (m_PathID != 0)
                        displayValue += $" [{m_FileID},{m_PathID}]";
                    else
                        displayValue += " [null]";
                    break;
                }
                case ParamDataType.FsmVar:
                {
                    AssetTypeValueField fsmVarParam = actionData.Get("fsmVarParams").Get(paramDataPos);
                    bool useVariable = fsmVarParam.Get("useVariable").GetValue().AsBool();
                    string objectType = fsmVarParam.Get("objectType").GetValue().AsString();
                    VariableType variableType = (VariableType)fsmVarParam.Get("type").GetValue().AsInt();

                    displayValue = "";

                    if (objectType.Contains("."))
                        objectType = objectType.Substring(objectType.LastIndexOf('.') + 1);

                    if (objectType != "")
                        displayValue += "(" + objectType + ")";

                    string variableName = fsmVarParam.Get("variableName").GetValue().AsString();
                    if (variableName != "")
                    {
                        displayValue += " " + variableName;
                    }
                    if (!useVariable)
                    {
                        displayValue += " ";
                        switch (variableType)
                        {
                            case VariableType.Float:
                                displayValue += fsmVarParam.Get("floatValue").GetValue().AsFloat().ToString();
                                break;
                            case VariableType.Int:
                                displayValue += fsmVarParam.Get("intValue").GetValue().AsInt().ToString();
                                break;
                            case VariableType.Bool:
                                displayValue += fsmVarParam.Get("boolValue").GetValue().AsBool().ToString().ToLower();
                                break;
                            case VariableType.String:
                                displayValue += fsmVarParam.Get("stringValue").GetValue().AsString().ToString();
                                break;
                            case VariableType.Color:
                            case VariableType.Quaternion:
                            case VariableType.Rect:
                            case VariableType.Vector2:
                            case VariableType.Vector3:
                                AssetTypeValueField vector4Value = fsmVarParam.Get("vector4Value");
                                displayValue += "[";
                                displayValue += vector4Value.Get("x").GetValue().AsInt().ToString() + ", ";
                                displayValue += vector4Value.Get("y").GetValue().AsInt().ToString() + ", ";
                                displayValue += vector4Value.Get("z").GetValue().AsInt().ToString() + ", ";
                                displayValue += vector4Value.Get("w").GetValue().AsInt().ToString();
                                displayValue += "]";
                                break;
                            case VariableType.Object:
                            case VariableType.GameObject:
                            case VariableType.Material:
                            case VariableType.Texture:
                                AssetTypeValueField objectReference = fsmVarParam.Get("objectReference");
                                int m_FileID = objectReference.Get("m_FileID").GetValue().AsInt();
                                long m_PathID = objectReference.Get("m_PathID").GetValue().AsInt64();
                                string name = GetAssetNameFast(m_FileID, m_PathID, inst);
                                if (name != "")
                                    name += " ";
                                displayValue += $"{name}[{m_FileID},{m_PathID}]";
                                break;
                            case VariableType.Array:
                                displayValue += ((VariableType)fsmVarParam.Get("arrayValue").Get("type").GetValue().AsInt()).ToString();
                                break;
                        }
                    }
                    break;
                }
                case ParamDataType.ObjectReference:
                {
                    AssetTypeValueField unityObjectParam = actionData.Get("unityObjectParams").Get(paramDataPos);
                    int m_FileID = unityObjectParam.Get("m_FileID").GetValue().AsInt();
                    long m_PathID = unityObjectParam.Get("m_PathID").GetValue().AsInt64();
                    string name = GetAssetNameFast(m_FileID, m_PathID, inst);
                    if (name != "")
                        name += " ";
                    displayValue = $"{name}[{m_FileID},{m_PathID}]";
                    break;
                }
                case ParamDataType.FunctionCall:
                {
                    AssetTypeValueField functionCallParam = actionData.Get("functionCallParams").Get(paramDataPos);
                    string functionName = functionCallParam.Get("FunctionName").GetValue().AsString();
                    string parameterType = functionCallParam.Get("parameterType").GetValue().AsString();
                    AssetTypeValueField field = null;
                    switch (parameterType)
                    {
                        case "bool":
                        {
                            field = functionCallParam.Get("BoolParameter");
                            displayValue = field.Get("value").GetValue().AsBool().ToString().ToLower();
                            goto NonPPtr;
                        }
                        case "float":
                        {
                            field = functionCallParam.Get("FloatParameter");
                            displayValue = field.Get("value").GetValue().AsFloat().ToString();
                            goto NonPPtr;
                        }
                        case "int":
                        {
                            field = functionCallParam.Get("IntParameter");
                            displayValue = field.Get("value").GetValue().AsInt().ToString();
                            goto NonPPtr;
                        }
                        case "GameObject":
                        {
                            field = functionCallParam.Get("GameObjectParameter");
                            goto PPtr;
                        }
                        case "Object":
                        {
                            field = functionCallParam.Get("ObjectParameter");
                            goto PPtr;
                        }
                        case "string":
                        {
                            field = functionCallParam.Get("StringParameter");
                            displayValue = field.Get("value").GetValue().AsString();
                            goto NonPPtr;
                        }
                        case "Vector2":
                        {
                            field = functionCallParam.Get("Vector2Parameter");
                            AssetTypeValueField value = field.Get("value");
                            string x = value.Get("x").GetValue().AsFloat().ToString();
                            string y = value.Get("y").GetValue().AsFloat().ToString();
                            displayValue = x + ", " + y;
                            goto NonPPtr;
                        }
                        case "Vector3":
                        {
                            field = functionCallParam.Get("Vector3Parameter");
                            AssetTypeValueField value = field.Get("value");
                            string x = value.Get("x").GetValue().AsFloat().ToString();
                            string y = value.Get("y").GetValue().AsFloat().ToString();
                            string z = value.Get("z").GetValue().AsFloat().ToString();
                            displayValue = x + ", " + y + ", " + z;
                            goto NonPPtr;
                        }
                        case "Rect":
                        {
                            field = functionCallParam.Get("RectParameter");
                            AssetTypeValueField value = field.Get("value");
                            string x = value.Get("x").GetValue().AsFloat().ToString();
                            string y = value.Get("y").GetValue().AsFloat().ToString();
                            string width = value.Get("width").GetValue().AsFloat().ToString();
                            string height = value.Get("height").GetValue().AsFloat().ToString();
                            displayValue = "[" + x + ", " + y + "], [" + width + ", " + height + "]";
                            goto NonPPtr;
                        }
                        case "Quaternion":
                        {
                            field = functionCallParam.Get("QuaternionParameter");
                            AssetTypeValueField value = field.Get("value");
                            string x = value.Get("x").GetValue().AsFloat().ToString();
                            string y = value.Get("y").GetValue().AsFloat().ToString();
                            string z = value.Get("z").GetValue().AsFloat().ToString();
                            string w = value.Get("w").GetValue().AsFloat().ToString();
                            displayValue = x + ", " + y + ", " + z + ", " + w;
                            goto NonPPtr;
                        }
                        case "Material":
                        {
                            field = functionCallParam.Get("MaterialParameter");
                            goto PPtr;
                        }
                        case "Texture":
                        {
                            field = functionCallParam.Get("TextureParameter");
                            goto PPtr;
                        }
                        case "Color":
                        {
                            field = functionCallParam.Get("ColorParameter");
                            AssetTypeValueField value = field.Get("value");
                            string r = ((int)(value.Get("r").GetValue().AsFloat()) * 255).ToString("X2");
                            string g = ((int)(value.Get("g").GetValue().AsFloat()) * 255).ToString("X2");
                            string b = ((int)(value.Get("b").GetValue().AsFloat()) * 255).ToString("X2");
                            string a = ((int)(value.Get("a").GetValue().AsFloat()) * 255).ToString("X2");
                            displayValue = "#" + r + g + b + a;
                            goto NonPPtr;
                        }
                        case "Enum":
                        {
                            field = functionCallParam.Get("EnumParameter");
                            string enumName = field.Get("enumName").GetValue().AsString();
                            if (enumName.Contains("."))
                                enumName = enumName.Substring(enumName.LastIndexOf(".") + 1);
                            displayValue = field.Get("value").GetValue().AsInt() + " (" + enumName + ")";
                            goto NonPPtr;
                        }
                        case "Array":
                        {
                            field = functionCallParam.Get("ArrayParameter");
                            displayValue = "";
                            goto NonPPtr;
                        }
                        case "None":
                        {
                            displayValue = "";
                            goto NonPPtr;
                        }
                        PPtr:
                        {
                            string name = field.Get("name").GetValue().AsString();
                            AssetTypeValueField value = field.Get("value");
                            int m_FileID = value.Get("m_FileID").GetValue().AsInt();
                            long m_PathID = value.Get("m_PathID").GetValue().AsInt64();
                            displayValue = functionName + "(" + name;
                            if (name == "")
                                name += GetAssetNameFast(m_FileID, m_PathID, inst);
                            if (m_PathID != 0)
                            {
                                if (name != "")
                                    displayValue += " ";
                                displayValue += $"[{m_FileID},{m_PathID}])";
                            }
                            break;
                        }
                        NonPPtr:
                        {
                            string name = "";
                            if (field != null)
                                name = field.Get("name").GetValue().AsString();
                            displayValue = name != ""
                                ? $"{functionName}({name}: {displayValue})"
                                : $"{functionName}({displayValue})";
                            break;
                        }
                    }
                    break;
                }
                case ParamDataType.FsmEventTarget:
                {
                    AssetTypeValueField fsmObjectParam = actionData.Get("fsmEventTargetParams").Get(paramDataPos);
                    EventTarget target = (EventTarget)fsmObjectParam.Get("target").GetValue().AsInt();
                    bool exclude = fsmObjectParam.Get("excludeSelf").Get("value").GetValue().AsBool();
                    displayValue = target.ToString() + (exclude ? "!" : "");
                    break;
                }
                case ParamDataType.Array:
                    displayValue = "";
                    break;
            }
            return displayValue;
        }

        private static bool UseVariable(AssetTypeValueField field)
        {
            if (field == null)
                return false;
            return field.Get("useVariable").GetValue().AsBool();
        }

        private static bool PossiblyHasName(ParamDataType paramDataType)
        {
            switch (paramDataType)
            {
                case ParamDataType.FsmBool:
                case ParamDataType.FsmColor:
                case ParamDataType.FsmFloat:
                case ParamDataType.FsmInt:
                case ParamDataType.FsmQuaternion:
                case ParamDataType.FsmRect:
                case ParamDataType.FsmVector2:
                case ParamDataType.FsmVector3:
                    return true;
                default:
                    return false;
            }
        }

        //asbytearray doesn't work so we create one to convert it
        private static byte[] GetByteData(AssetTypeValueField field)
        {
            byte[] data = new byte[field.GetValue().AsArray().size];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)field.Get(i).GetValue().AsUInt();
            }
            return data;
        }

        private static HashSet<uint> allowed = new HashSet<uint>
        {
            0x15,0x1C,0x2B,0x30,0x31,0x3E,0x48,0x4A,0x53,0x54,
            0x56,0x59,0x5A,0x5B,0x6D,0x73,0x75,0x79,0x80,0x86,
            0x8E,0x96,0x98,0x9C,0x9E,0xAB,0xB8,0xB9,0xBA,0xBB,
            0xBC,0xC8,0xD5,0xDD,0xE2,0xE4,0xEE,0xF0,0x102,0x10F,
            0x110,0x111,0x122,0x13F,0x149,0x16B,0x583D8C3F
        };

        private static string GetAssetNameFast(int fileId, long pathId, AssetsFileInstance inst)
        {
            if (pathId == 0)
                return "";

            AssetsFile file = null;
            AssetsFileTable table = null;
            if (fileId == 0)
            {
                file = inst.file;
                table = inst.table;
            }
            else
            {
                AssetsFileInstance dep = inst.dependencies[fileId - 1];
                file = dep.file;
                table = dep.table;
            }

            AssetFileInfoEx inf = table.GetAssetInfo(pathId);
            AssetsFileReader reader = file.reader;

            if (allowed.Contains(inf.curFileType))
            {
                reader.Position = inf.absoluteFilePos;
                return reader.ReadCountStringInt32();
            }
            if (inf.curFileType == 0x01)
            {
                reader.Position = inf.absoluteFilePos;
                int size = reader.ReadInt32();
                reader.Position += size * 12;
                reader.Position += 4;
                return reader.ReadCountStringInt32();
            }
            else if (inf.curFileType == 0x72)
            {
                reader.Position = inf.absoluteFilePos;
                reader.Position += 28;
                string name = reader.ReadCountStringInt32();
                if (name != "")
                {
                    return name;
                }
            }
            return "";
        }
    }
}
