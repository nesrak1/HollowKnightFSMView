﻿using AssetsTools.NET;
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
                string displayValue = "? " + type.ToString();
                if (version == 1) //read binary as normal
                {
                    switch (type)
                    {
                        case ParamDataType.Integer:
                        case ParamDataType.Enum:
                        case ParamDataType.FsmInt:
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
                        case ParamDataType.FsmGameObject:
                        case ParamDataType.FsmOwnerDefault:
                        {
                            AssetTypeValueField gameObject;
                            if (type == ParamDataType.FsmOwnerDefault)
                            {
                                AssetTypeValueField fsmOwnerDefaultParam = actionData.Get("fsmOwnerDefaultParams").Get((uint)paramDataPos);
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
                                {
                                    displayValue = typeName;
                                }
                                else
                                {
                                    displayValue += ": " + typeName;
                                }
                            }
                            if (m_PathID != 0)
                            {
                                displayValue += $" [{m_FileID},{m_PathID}]";
                            }
                            break;
                        }
                        case ParamDataType.Array:
                            displayValue = "";
                            break;
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
                        case ParamDataType.FsmGameObject:
                        case ParamDataType.FsmOwnerDefault:
                        {
                            AssetTypeValueField gameObject;
                            if (type == ParamDataType.FsmOwnerDefault)
                            {
                                AssetTypeValueField fsmOwnerDefaultParam = actionData.Get("fsmOwnerDefaultParams").Get((uint)paramDataPos);
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
                        case ParamDataType.Array:
                            displayValue = "";
                            break;
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
                actionValues[i] = displayValue;
            }
            reader.Close();
            stream.Close();
            return actionValues;
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
