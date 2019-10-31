using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using static CitizenFX.Core.Native.API;

namespace CitizenFX.DataLayer
{
    internal class FxDbConnection
    {
        private string m_name;
        private DbFactoryList m_factoryList;
        private DbProviderFactory m_factory;
        private ConcurrentQueue<Action> m_results = new ConcurrentQueue<Action>();
        private DbDataAbstraction m_dataAbstraction;
        private string m_connectionString;

        public bool Connected => m_factory != null;

        public FxDbConnection(string name, DbFactoryList factories)
        {
            this.m_name = name;
            this.m_factoryList = factories;

            Task.Run(this.Run).ContinueWith(t =>
            {
                Debug.WriteLine("^1Database error: ^7\n" + t.Exception.InnerExceptions.FirstOrDefault()?.Message ?? "NULL");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task Run()
        {
            // setup connection
            var connectionString = GetConvar($"{m_name}_connection_string", "");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"No connection string configured for {m_name}. " + 
                    $"Please set the `{m_name}_connection_string` convar in your server startup script.");
            }

            var csBuilder = new DbConnectionStringBuilder();
            csBuilder.ConnectionString = connectionString;
            
            if (!csBuilder.TryGetValue("dbProvider", out object provider))
            {
                provider = "mysql";
            }

            csBuilder.Remove("dbProvider");

            var factory = m_factoryList.GetFactory(provider.ToString());

            if (factory == null)
            {
                throw new InvalidOperationException($"Invalid database provider name {provider} in connection string for {m_name}. " + 
                    $"Supported providers: [{string.Join(", ", m_factoryList.GetNames())}].");
            }

            m_dataAbstraction = m_factoryList.GetDataAbstraction(provider.ToString());

            m_connectionString = csBuilder.ConnectionString;

            using (var connection = await OpenConnection(factory))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT 1";

                    var r = await command.ExecuteScalarAsync();

                    if (Convert.ToInt32(r) == 1)
                    {
                        m_factory = factory;
                    }
                    else
                    {
                        throw new InvalidOperationException($"SELECT 1 failed for {m_name} - it returned {r} instead.");
                    }
                }
            }
        }

        private async Task<DbConnection> OpenConnection(DbProviderFactory factory = null)
        {
            factory = factory ?? m_factory;

            var connection = new ProfiledDbConnection(factory.CreateConnection(), MiniProfiler.Current);
            connection.ConnectionString = m_connectionString;

            await connection.OpenAsync();

            return connection;
        }

        public async Task<TResult> ExecuteAsync<TResult>(string query, string invoker, IDictionary<string, object> parameters, DbTaskFactory<TResult> taskFactory)
        {
            if (m_factory != null)
            {
                using (var step = MiniProfiler.Current.Step(invoker ?? "dblyr-unknown"))
                {
                    using (var conn = await OpenConnection())
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            var task = taskFactory.CreateTask(cmd);

                            cmd.CommandText = task.ModifyQuery(query, m_dataAbstraction);
                            AddParameters(cmd, parameters);

                            return await task.ExecuteCommandAsync(m_dataAbstraction);
                        }
                    }
                }
            }

            return default(TResult);
        }

        internal void Tick()
        {
            while (m_results.TryDequeue(out var action))
            {
                action();
            }
        }

        internal void WrapTask<TResult>(string query, Task<TResult> task, CallbackDelegate cb)
        {
            task.ContinueWith(t =>
            {
                m_results.Enqueue(() =>
                {
                    cb.Invoke(false, t.Result);
                });

                ScheduleTick();
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            task.ContinueWith(t =>
            {
                m_results.Enqueue(() =>
                {
                    cb.Invoke(ExceptionToMessage(t, query), false);
                });

                ScheduleTick();
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void ScheduleTick()
        {
            ScheduleResourceTick("dblyr");
        }

        internal string ExceptionToMessage<TResult>(Task<TResult> task, string query)
        {
            Exception exception = task.Exception.InnerExceptions.FirstOrDefault() ?? task.Exception;

            if (exception is DbException ex)
            {
                return $"Database error in query `{query}`: {ex.Message}";
            }
            else
            {
                return "Critical error: " + exception.Message;;
            }
        }

        private void AddParameters(DbCommand command, IDictionary<string, object> parameters)
        {
            DbParameter CreateParameter(KeyValuePair<string, object> param)
            {
                var p = command.CreateParameter();
                p.ParameterName = param.Key.StartsWith("@") ? param.Key : $"@{param.Key}";
                p.Value = param.Value;
                return p;
            }

            command.Parameters.AddRange(parameters.Select(CreateParameter).ToArray());
        }

        public async Task<bool> RunTransaction(IEnumerable<string> queries, IEnumerable<object> parameters)
        {
            using (var connection = await OpenConnection())
            {
                var transaction = connection.BeginTransaction();

                try
                {
                    var cmd = connection.CreateCommand();

                    foreach (var q in Enumerable.Zip(queries, parameters, (query, paras) => new
                    {
                        query,
                        paras
                    }))
                    {
                        if (!(q.paras is IDictionary<string, object> paramList))
                        {
                            paramList = new Dictionary<string, object>();
                        }

                        cmd.CommandText = q.query;
                        cmd.Parameters.Clear();
                        AddParameters(cmd, paramList);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return true;
        }
    }
}