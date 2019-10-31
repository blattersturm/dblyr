using System;
using System.Threading.Tasks;

namespace CitizenFX.DataLayer
{
    public abstract class DbTask<TResult>
    {
        public virtual string ModifyQuery(string query, DbDataAbstraction dataAbstraction)
        {
            return query;
        }

        public abstract Task<TResult> ExecuteCommandAsync(DbDataAbstraction dataAbstraction);
    }
}