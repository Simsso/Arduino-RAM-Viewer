using System;

namespace RAM_Viewer
{
    [Serializable]
    public class WatchListVariable
    {
        // list view attributes
        public string Name { get; set; }
        public string ValueString { get; set; }
        public string AddressString { get; set; }
        public int Elements { get; set; }

        public Control.DataType DataType;
        public int Address;
        public byte[] Value;

        public WatchListVariable(string Name, int Address, int Elements, Control.DataType DataType)
        { 
            this.Name = Name;
            this.Address = Address;
            this.AddressString = Control.GetHexAddress(Address);
            this.Elements = Elements;
            this.DataType = DataType;

            // initialize value array
            switch (DataType)
            {
                case Control.DataType.Boolean:
                    Value = new byte[Elements * 1];
                    break;
                case Control.DataType.Byte:
                    Value = new byte[Elements * 1];
                    break;
                case Control.DataType.Char:
                    Value = new byte[Elements * 1];
                    break;
                case Control.DataType.UnsignedInt:
                    Value = new byte[Elements * 2];
                    break;
                case Control.DataType.Int:
                    Value = new byte[Elements * 2];
                    break;
                case Control.DataType.UnsignedLong:
                    Value = new byte[Elements * 4];
                    break;
                case Control.DataType.Long:
                    Value = new byte[Elements * 4];
                    break;
                case Control.DataType.Float:
                    Value = new byte[Elements * 4];
                    break;
                default:
                    break;
            }
        }

        public void SetValue(byte[] NewValue)
        {
            NewValue.CopyTo(Value, 0);
            RefreshValueString();
        }

        private void RefreshValueString()
        {
            string Output = "";
            for (int i = 0; i < Value.Length; i++)
            {
                switch (DataType)
                {
                    case Control.DataType.Boolean:
                        if (Value[i] == 1)
                            Output += "true";
                        else
                            Output += "false";
                        break;
                    case Control.DataType.Byte:
                        Output += Value[i].ToString();
                        break;
                    case Control.DataType.Char:
                        Output += ((char)Value[i]).ToString();
                        break;
                    case Control.DataType.UnsignedInt:
                        Output += ((uint)(Value[i] | Value[++i] << 8)).ToString();
                        break;
                    case Control.DataType.Int:
                        Output += ((int)(Value[i] | Value[++i] << 8)).ToString();
                        break;
                    case Control.DataType.UnsignedLong:
                        Output += ((ulong)(Value[i] | (Value[++i] << 8) | Value[++i] << 16 | Value[++i] << 24)).ToString();
                        break;
                    case Control.DataType.Long:
                        Output += ((long)(Value[i] | (Value[++i] << 8) | Value[++i] << 16 | Value[++i] << 24)).ToString();
                        break;
                    case Control.DataType.Float:
                        Output += System.BitConverter.ToSingle(Value, 0).ToString();
                        i += 3;
                        break;
                    default:
                        break;
                }

                Output += "; ";
            }

            ValueString = Output.Substring(0, Output.Length - 2);
        }
    }
}
