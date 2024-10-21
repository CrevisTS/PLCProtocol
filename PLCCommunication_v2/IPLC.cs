using System;

namespace PLCCommunication_v2
{
    public interface IPLC : IDisposable
    {
        void Connect();

        void Disconnect();

        void Refresh();

        void Load();

        void Load(string filePath);

        void Save();

        void Save(string filePath);
    }
}
