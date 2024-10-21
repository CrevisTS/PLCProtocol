namespace PLCCommunication_v2
{
    public interface IPLCReceivingPacket
    {
        bool[] GetBooleanArray();

        byte[] GetByteArray();

        short[] GetInt16Array();

        ushort[] GetUInt16Array();

        int[] GetInt32Array();

        uint[] GetUInt32Array();

        long[] GetInt64Array();

        ulong[] GetUInt64Array();

        float[] GetSingleArray();

        double[] GetDoubleArray();

        string GetASCIIString();
    }
}
