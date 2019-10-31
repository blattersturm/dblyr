using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenFX.DataLayer
{
    internal class FetchAllFactory : DbTaskFactory<IList<IDictionary<string, object>>>
    {
        public override DbTask<IList<IDictionary<string, object>>> CreateTask(DbCommand cmd)
        {
            return new FetchAllTask(cmd);
        }
    }

    internal class FetchAllTask : DbTask<IList<IDictionary<string, object>>>
    {
        private DbCommand cmd;

        public FetchAllTask(DbCommand cmd)
        {
            this.cmd = cmd;
        }

        public override async Task<IList<IDictionary<string, object>>> ExecuteCommandAsync(DbDataAbstraction dataAbstraction)
        {
            var rows = new List<IDictionary<string, object>>();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    rows.Add(
                        Enumerable.Range(0, reader.FieldCount)
                            .ToDictionary(
                                i => reader.GetName(i),
                                i => !reader.IsDBNull(i) ? reader.GetValue(i) : null
                            )
                    );
                }
            }

            return rows;
        }
    }
}