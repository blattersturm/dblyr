using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenFX.DataLayer
{
    internal class InsertFactory : DbTaskFactory<object>
    {
        public override DbTask<object> CreateTask(DbCommand cmd)
        {
            return new InsertTask(cmd);
        }
    }

    internal class InsertTask : DbTask<object>
    {
        private DbCommand cmd;

        public InsertTask(DbCommand cmd)
        {
            this.cmd = cmd;
        }

        public override string ModifyQuery(string query, DbDataAbstraction dataAbstraction)
        {
            return dataAbstraction.AppendInsertIdQuery(query);
        }

        public override Task<object> ExecuteCommandAsync(DbDataAbstraction dataAbstraction)
        {
            return cmd.ExecuteScalarAsync();
        }
    }
}