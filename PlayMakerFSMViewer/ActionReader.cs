using AssetsTools.NET;
using PlayMakerFSMViewer.FieldClasses;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMakerFSMViewer
{
    public class ActionReader
    {
        public static string[] ActionValues(AssetTypeValueField actionData, int version)
        {
            AssetTypeValueField paramDataType = actionData.Get("paramDataType");
            uint paramCount = paramDataType.GetValue().AsArray().size;
            byte[] byteData = GetByteData(actionData.Get("byteData"));
            MemoryStream stream = new MemoryStream(byteData);
            BinaryReader reader = new BinaryReader(stream);
            string[] actionValues = new string[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                ParamDataType type = (ParamDataType)paramDataType.Get((uint)i).GetValue().AsInt();
                int paramDataPos = actionData.Get("paramDataPos").Get((uint)i).GetValue().AsInt();
                int paramByteDataSize = actionData.Get("paramByteDataSize").Get((uint)i).GetValue().AsInt();
                reader.BaseStream.Position = paramDataPos;
                string displayValue = "? " + type;
                displayValue = GetDisplayValue(actionData, version, type, paramDataPos, paramByteDataSize, reader);
                actionValues[i] = displayValue;
            }
            reader.Close();
            stream.Close();
            return actionValues;
        }

        public static string GetDisplayValue(AssetTypeValueField actionData, int version,
            ParamDataType type, int paramDataPos, int paramByteDataSize, BinaryReader reader)
        {
            string displayValue = "? " + type;
            if (version == 1 && !(type == ParamDataType.FsmString && paramByteDataSize == 0)) //read binary as normal
            {
                switch (type)
                {
                    case ParamDataType.Integer:
                    case ParamDataType.Enum:
                    case ParamDataType.FsmInt:
                    case ParamDataType.FsmEnum:
                    {
                        displayValue = reader.ReadInt32().ToString();
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
                        displayValue = Encoding.ASCII.GetString(reader.ReadBytes(paramByteDataSize));
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
                            string varName = Encoding.ASCII.GetString(reader.ReadBytes(length - 1));
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
                        displayValue = Encoding.ASCII.GetString(reader.ReadBytes(paramByteDataSize));
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
                        field = actionData.Get("fsmIntParams").Get((uint)paramDataPos);
                        displayValue = field.Get("value").GetValue().AsInt().ToString();
                        break;
                    }
                    case ParamDataType.FsmEnum:
                    {
                        field = actionData.Get("fsmEnumParams").Get((uint)paramDataPos);
                        string intValue = field.Get("intValue").GetValue().AsInt().ToString();
                        string enumName = field.Get("enumName").GetValue().AsString().ToString();
                        displayValue = $"{intValue} ({enumName})";
                        break;
                    }
                    case ParamDataType.FsmBool:
                    {
                        field = actionData.Get("fsmBoolParams").Get((uint)paramDataPos);
                        displayValue = field.Get("value").GetValue().AsBool().ToString();
                        break;
                    }
                    case ParamDataType.FsmFloat:
                    {
                        field = actionData.Get("fsmFloatParams").Get((uint)paramDataPos);
                        displayValue = field.Get("value").GetValue().AsFloat().ToString();
                        break;
                    }
                    case ParamDataType.FsmString:
                    {
                        field = actionData.Get("fsmStringParams").Get((uint)paramDataPos);
                        displayValue = field.Get("value").GetValue().AsString();
                        break;
                    }
                    case ParamDataType.FsmEvent:
                    {
                        field = actionData.Get("stringParams").Get((uint)paramDataPos);
                        displayValue = field.GetValue().AsString();
                        break;
                    }
                    case ParamDataType.FsmVector2:
                    {
                        field = actionData.Get("fsmVector2Params").Get((uint)paramDataPos);
                        AssetTypeValueField value = field.Get("value");
                        string x = value.Get("x").GetValue().AsFloat().ToString();
                        string y = value.Get("y").GetValue().AsFloat().ToString();
                        displayValue = x + ", " + y;
                        break;
                    }
                    case ParamDataType.FsmVector3:
                    {
                        field = actionData.Get("fsmVector3Params").Get((uint)paramDataPos);
                        AssetTypeValueField value = field.Get("value");
                        string x = value.Get("x").GetValue().AsFloat().ToString();
                        string y = value.Get("y").GetValue().AsFloat().ToString();
                        string z = value.Get("z").GetValue().AsFloat().ToString();
                        displayValue = x + ", " + y + ", " + z;
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
                        AssetTypeValueField fsmOwnerDefaultParam = actionData.Get("fsmOwnerDefaultParams").Get((uint)paramDataPos);

                        if (fsmOwnerDefaultParam["ownerOption"].GetValue().AsInt() == 0)
                        {
                            displayValue = "FSM Owner";
                            break;
                        }
                        
                        gameObject = fsmOwnerDefaultParam.Get("gameObject");
                    }
                    else
                    {
                        gameObject = actionData.Get("fsmGameObjectParams").Get((uint)paramDataPos);
                    }
                    string name = gameObject.Get("name").GetValue().AsString();
                    AssetTypeValueField value = gameObject.Get("value");
                    int m_FileID = value.Get("m_FileID").GetValue().AsInt();
                    long m_PathID = value.Get("m_PathID").GetValue().AsInt64();
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
                    AssetTypeValueField fsmObjectParam = actionData.Get("fsmObjectParams").Get((uint)paramDataPos);
                    string name = fsmObjectParam.Get("name").GetValue().AsString();
                    string typeName = fsmObjectParam.Get("typeName").GetValue().AsString();
                    if (typeName.Contains("."))
                        typeName = typeName.Substring(typeName.LastIndexOf(".") + 1);
                    AssetTypeValueField value = fsmObjectParam.Get("value");
                    int m_FileID = value.Get("m_FileID").GetValue().AsInt();
                    long m_PathID = value.Get("m_PathID").GetValue().AsInt64();
                    displayValue = name;
                    if (typeName != "")
                    {
                        if (name == "")
                            displayValue = typeName;
                        else
                            displayValue += ": " + typeName;
                    }
                    if (m_PathID != 0)
                    {
                        displayValue += $" [{m_FileID},{m_PathID}]";
                    }
                    break;
                }
                case ParamDataType.FunctionCall:
                {
                    AssetTypeValueField functionCallParam = actionData.Get("functionCallParams").Get((uint)paramDataPos);
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
                            field?.Get("name").GetValue().AsString();
                            displayValue = name != ""
                                ? $"{functionName}({name}: {displayValue})"
                                : $"{functionName}({displayValue})";
                            break;
                        }
                    }
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
                data[i] = (byte)field.Get((uint)i).GetValue().AsUInt();
            }
            return data;
        }
    }
}
