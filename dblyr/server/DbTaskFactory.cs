using System.Data.Common;

namespace CitizenFX.DataLayer
{
    public abstract class DbTaskFactory<TResult>
    {
        public static TResult Hint => default(TResult);

        public abstract DbTask<TResult> CreateTask(DbCommand cmd);
    }
}