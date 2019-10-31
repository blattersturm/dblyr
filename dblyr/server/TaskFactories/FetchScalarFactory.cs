using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenFX.DataLayer
{
    internal class FetchScalarFactory : DbTaskFactory<object>
    {
        public override DbTask<object> CreateTask(DbCommand cmd)
        {
            return new FetchScalarTask(cmd);
        }
    }

    internal class FetchScalarTask : DbTask<object>
    {
        private DbCommand cmd;

        public FetchScalarTask(DbCommand cmd)
        {
            this.cmd = cmd;
        }

        public override Task<object> ExecuteCommandAsync(DbDataAbstraction dataAbstraction)
        {
            return cmd.ExecuteScalarAsync();
        }
    }
}