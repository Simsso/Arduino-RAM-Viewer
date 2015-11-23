using System;

namespace RAM_Viewer
{
    [Serializable]
    public class MemoryStamp
    {
        public static int TotalBytes = 2048;
        public string Name;
        public int BytesReceived = 0, BytesShiftedOut = 0;
        public byte[] RAM = new byte[TotalBytes];
        public bool AddedToMainWindow = false;
        public long LastTimeDataWasAdded = 0;


        public MemoryStamp(string Name)
        {
            this.Name = Name;
        }

        public void PushByte(byte NewByte)
        {
            if (BytesReceived >= TotalBytes) // rotate array if to many bytes are incoming
            {
                for (int i = 1; i < TotalBytes; i++)
                    RAM[i - 1] = RAM[i];

                BytesShiftedOut++;
                BytesReceived--;
            }
            RAM[BytesReceived] = NewByte;
            BytesReceived++;

            LastTimeDataWasAdded = DateTime.Now.Ticks;
        }

        public bool Complete()
        {
            if (BytesReceived == TotalBytes)
                return true;

            return false;
        }

        public double GetPercentage()
        {
            return BytesReceived / (double)TotalBytes;
        }
    }
}
