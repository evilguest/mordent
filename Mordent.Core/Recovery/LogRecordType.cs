namespace Mordent.Core
{
    internal enum LogRecordType : byte
    {
        None,
        TranStart,
        TranCommit,
        TranRollback,
        CheckPoint,
        //CheckPointStart,
        //CheckpointEnd,
        ChangeRowT,
        ChangeRowString
    };
}