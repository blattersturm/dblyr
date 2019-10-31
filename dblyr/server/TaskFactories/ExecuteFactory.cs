using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenFX.DataLayer
{
    internal class ExecuteFactory : DbTaskFactory<object>
    {
        public override DbTask<object> CreateTask(DbCommand cmd)
        {
            return new ExecuteTask(cmd);
        }
    }

    internal class ExecuteTask : DbTask<object>
    {
        private DbCommand cmd;

        public ExecuteTask(DbCommand cmd)
        {
            this.cmd = cmd;
        }

        public override async Task<object> ExecuteCommandAsync(DbDataAbstraction dataAbstraction)
        {
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}