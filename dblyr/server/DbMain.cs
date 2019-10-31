using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using CitizenFX.Core;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Profiling;

using static CitizenFX.Core.Native.API;


namespace CitizenFX.DataLayer
{
    public class DbMain : BaseScript
    {
        private readonly ServiceProvider m_serviceProvider;
        private readonly MiniProfiler m_profiler;

        public MiniProfiler Profiler => m_profiler;

        public static DbMain Self { get; private set; }

        public DbMain()
        {
            Exports.Add("getConnectionObject", new Func<string, object>(GetConnectionObject));

            m_serviceProvider = new ServiceCollection()
                .AddSingleton<DbFactoryList>()
                .AddDbTaskFactory<FetchAllFactory>()
                .AddDbTaskFactory<FetchScalarFactory>()
                .AddDbTaskFactory<ExecuteFactory>()
                .AddDbTaskFactory<InsertFactory>()
                .AddSingleton<DbContext>()
                .BuildServiceProvider();

            MiniProfiler.Configure(new MiniProfilerOptions()
            {
                
            });

            m_profiler = MiniProfiler.StartNew("dblyr");

            Self = this;
        }

        [Command("dblyr_connect")]
        public void Connect(string[] args)
        {
            if (args.Length == 1)
            {
                using (var scope = m_serviceProvider.CreateScope())
                {
                    scope.ServiceProvider.GetRequiredService<DbContext>().GetConnection(args[0]);
                }
            }
        }

        [Command("dblyr_profile")]
        public void Profile()
        {
            Debug.WriteLine(m_profiler.RenderPlainText());
        }

        [Tick]
        public Task OnTick()
        {
            using (var scope = m_serviceProvider.CreateScope())
            {
                return scope.ServiceProvider.GetRequiredService<DbContext>().Tick();
            }
        }

        private object GetConnectionObject(string connectionName)
        {
            using (var scope = m_serviceProvider.CreateScope())
            {
                return WrapClass(new WrapConnection(m_serviceProvider, scope.ServiceProvider.GetRequiredService<DbContext>().GetConnection(connectionName)));
            }
        }

        private class WrapConnection
        {
            private ServiceProvider m_serviceProvider;
            private FxDbConnection m_fxDbConnection;

            public WrapConnection(ServiceProvider serviceProvider, FxDbConnection fxDbConnection)
            {
                this.m_serviceProvider = serviceProvider;
                this.m_fxDbConnection = fxDbConnection;
            }

            public bool IsConnected()
            {
                return m_fxDbConnection.Connected;
            }

            private void DoRequest<T, TResult>(string query, object parameters, CallbackDelegate callback, TResult def = default(TResult))
                where T : DbTaskFactory<TResult>
            {
                if (!(parameters is IDictionary<string, object> paramList))
                {
                    paramList = new Dictionary<string, object>();
                }

                var invoker = GetInvokingResource();

                using (var scope = m_serviceProvider.CreateScope())
                {
                    var factory = scope.ServiceProvider.GetRequiredService<T>();
                    
                    m_fxDbConnection.WrapTask(
                        query,
                        Task.Run(
                            () => m_fxDbConnection
                                .ExecuteAsync(query, invoker, paramList, factory)
                            ),
                        callback
                    );
                }
            }

            private object[] DoSyncRequest<T, TResult>(string query, object parameters, TResult def = default(TResult))
                where T : DbTaskFactory<TResult>
            {
                if (!(parameters is IDictionary<string, object> paramList))
                {
                    paramList = new Dictionary<string, object>();
                }

                var invoker = GetInvokingResource();

                using (var scope = m_serviceProvider.CreateScope())
                {
                    var factory = scope.ServiceProvider.GetRequiredService<T>();
                     
                    var task = Task.Run(
                        () => m_fxDbConnection
                            .ExecuteAsync(query, invoker, paramList, factory)
                    );

                    try
                    {
                        task.Wait();

                        return new object[2] { false, task.Result };
                    }
                    catch (AggregateException)
                    {
                        return new object[2] { m_fxDbConnection.ExceptionToMessage(task, query), false };
                    }
                }
            }

            public void Execute(string query, object parameters, CallbackDelegate cb)
            {
                DoRequest<ExecuteFactory, object>(query, parameters, cb);
            }

            public void Insert(string query, object parameters, CallbackDelegate cb)
            {
                DoRequest<InsertFactory, object>(query, parameters, cb);
            }

            public void FetchScalar(string query, object parameters, CallbackDelegate cb)
            {
                DoRequest<FetchScalarFactory, object>(query, parameters, cb);
            }

            public void FetchAll(string query, object parameters, CallbackDelegate cb)
            {
                DoRequest<FetchAllFactory, IList<IDictionary<string, object>>>(query, parameters, cb);
            }

            public object[] ExecuteSync(string query, object parameters)
            {
                return DoSyncRequest<ExecuteFactory, object>(query, parameters);
            }

            public object[] InsertSync(string query, object parameters)
            {
                return DoSyncRequest<InsertFactory, object>(query, parameters);
            }

            public object[] FetchScalarSync(string query, object parameters)
            {
                return DoSyncRequest<FetchScalarFactory, object>(query, parameters);
            }

            public object[] FetchAllSync(string query, object parameters)
            {
                return DoSyncRequest<FetchAllFactory, IList<IDictionary<string, object>>>(query, parameters);
            }

            public void ExecuteTransaction(IEnumerable<object> queriesRaw, IEnumerable<object> parameters, CallbackDelegate cb)
            {
                var queries = queriesRaw.Select(a => a.ToString());

                using (var scope = m_serviceProvider.CreateScope())
                {
                    m_fxDbConnection.WrapTask(
                        string.Join(";\n", queries),
                        Task.Run(() => m_fxDbConnection.RunTransaction(queries, parameters)),
                        cb
                    );
                }
            }

            public object[] ExecuteTransactionSync(IEnumerable<object> queriesRaw, IEnumerable<object> parameters, CallbackDelegate cb)
            {
                var queries = queriesRaw.Select(a => a.ToString());

                using (var scope = m_serviceProvider.CreateScope())
                {
                    var task = Task.Run(() => m_fxDbConnection.RunTransaction(queries, parameters));

                    try
                    {
                        task.Wait();

                        return new object[2] { false, task.Result };
                    }
                    catch (AggregateException)
                    {
                        return new object[2] { m_fxDbConnection.ExceptionToMessage(task, string.Join(";\n", queries)), false };
                    }
                }
            }
        }

        private static object WrapClass(object instance)
        {
            var type = instance.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            var retval = new Dictionary<string, object>();

            foreach (var method in methods)
            {
                var delegType = Expression.GetDelegateType(
                    method.GetParameters()
                          .Select(param => param.ParameterType)
                          .Concat(new[] { method.ReturnType })
                          .ToArray()
                );

                retval[method.Name] = method.CreateDelegate(delegType, instance);
            }

            return retval;
        }
    }
}