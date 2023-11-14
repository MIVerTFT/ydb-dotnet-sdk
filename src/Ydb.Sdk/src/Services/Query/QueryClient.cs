using Microsoft.Extensions.Logging;
using Ydb.Query;
using Ydb.Query.V1;
using Ydb.Sdk.Client;
using Ydb.Sdk.Services.Sessions;
using Ydb.Sdk.Value;

namespace Ydb.Sdk.Services.Query;

public enum ExecMode
{
    Unspecified = 0,
    Parse = 10,
    Validate = 20,
    Explain = 30,

    // reserved 40; // EXEC_MODE_PREPARE
    Execute = 50
}

public enum Syntax
{
    Unspecified = 0,

    /// <summary>
    /// YQL
    /// </summary>
    YqlV1 = 1,

    /// <summary>
    /// PostgresQL
    /// </summary>
    Pg = 2
}

public enum StatsMode
{
    Unspecified = 0,

    /// <summary>
    /// Stats collection is disabled
    /// </summary>
    None = 10,

    /// <summary>
    /// Aggregated stats of reads, updates and deletes per table
    /// </summary>
    Basic = 20,

    /// <summary>
    /// Add execution stats and plan on top of STATS_MODE_BASIC
    /// </summary>
    Full = 30,

    /// <summary>
    /// Detailed execution stats including stats for individual tasks and channels
    /// </summary>
    Profile = 40
}

public class ExecuteQuerySettings : RequestSettings
{
    public ExecMode ExecMode { get; set; } = ExecMode.Execute;
    public Syntax Syntax { get; set; }

    public StatsMode StatsMode { get; set; }
}

public class CreateSessionSettings : RequestSettings
{
}

public class DeleteSessionSettings : RequestSettings
{
}

public class AttachSessionSettings : RequestSettings
{
}

public class BeginTransactionSettings : RequestSettings
{
}

public class CommitTransactionSettings : RequestSettings
{
}

public class RollbackTransactionSettings : RequestSettings
{
}

public class CreateSessionResponse : ResponseWithResultBase<CreateSessionResponse.ResultData>
{
    internal CreateSessionResponse(Status status, ResultData? result = null)
        : base(status, result)
    {
    }

    public class ResultData
    {
        private ResultData(Session session)
        {
            Session = session;
        }

        public Session Session { get; }

        internal static ResultData FromProto(Ydb.Query.CreateSessionResponse resultProto, Driver driver,
            string endpoint)
        {
            var session = new Session(
                driver: driver,
                sessionPool: null,
                id: resultProto.SessionId,
                nodeId: resultProto.NodeId,
                endpoint: endpoint);

            return new ResultData(
                session: session
            );
        }
    }
}

public class DeleteSessionResponse : ResponseBase
{
    internal DeleteSessionResponse(Status status) : base(status)
    {
    }

    private DeleteSessionResponse(Ydb.Query.DeleteSessionResponse proto)
        : base(Status.FromProto(proto.Status, proto.Issues))
    {
    }

    internal static DeleteSessionResponse FromProto(Ydb.Query.DeleteSessionResponse proto)
    {
        return new DeleteSessionResponse(proto);
    }
}

public class SessionState : ResponseBase
{
    internal SessionState(Status status) : base(status)
    {
    }

    private SessionState(Ydb.Query.SessionState proto)
        : base(Status.FromProto(proto.Status, proto.Issues))
    {
    }

    internal static SessionState FromProto(Ydb.Query.SessionState proto)
    {
        return new SessionState(proto);
    }
}

public class SessionStateStream : StreamResponse<Ydb.Query.SessionState, SessionState>
{
    internal SessionStateStream(Driver.StreamIterator<Ydb.Query.SessionState> iterator) : base(iterator)
    {
    }

    protected override SessionState MakeResponse(Ydb.Query.SessionState protoResponse)
    {
        return SessionState.FromProto(protoResponse);
    }

    protected override SessionState MakeResponse(Status status)
    {
        return new SessionState(status);
    }
}

public class ExecuteQueryResponsePart : ResponseBase
{
    public long? ResultSetIndex;
    public Value.ResultSet? ResultSet;

    internal ExecuteQueryResponsePart(Status status) : base(status)
    {
    }

    private ExecuteQueryResponsePart(Ydb.Query.ExecuteQueryResponsePart proto)
        : base(Status.FromProto(proto.Status, proto.Issues))
    {
        ResultSetIndex = proto.ResultSetIndex;
        if (proto.ResultSet is not null)
        {
            ResultSet = Value.ResultSet.FromProto(proto.ResultSet);
        }
    }

    internal static ExecuteQueryResponsePart FromProto(Ydb.Query.ExecuteQueryResponsePart proto)
    {
        return new ExecuteQueryResponsePart(proto);
    }
}

public class ExecuteQueryStream : StreamResponse<Ydb.Query.ExecuteQueryResponsePart, ExecuteQueryResponsePart>
    , IAsyncEnumerable<ExecuteQueryResponsePart>
{
    internal ExecuteQueryStream(Driver.StreamIterator<Ydb.Query.ExecuteQueryResponsePart> iterator) : base(iterator)
    {
    }

    public new async Task<bool> Next()
    {
        var isNext = await base.Next();
        if (isNext)
        {
            Response.EnsureSuccess();
        }

        return isNext;
    }

    protected override ExecuteQueryResponsePart MakeResponse(Ydb.Query.ExecuteQueryResponsePart protoResponse)
    {
        return ExecuteQueryResponsePart.FromProto(protoResponse);
    }

    protected override ExecuteQueryResponsePart MakeResponse(Status status)
    {
        return new ExecuteQueryResponsePart(status);
    }

    public async IAsyncEnumerator<ExecuteQueryResponsePart> GetAsyncEnumerator(
        CancellationToken cancellationToken = new CancellationToken())
    {
        while (await Next())
        {
            yield return Response;
        }
    }
}

public class BeginTransactionResponse : ResponseBase
{
    internal BeginTransactionResponse(Status status) : base(status)
    {
    }

    public Tx Tx { get; } = new();

    private BeginTransactionResponse(Ydb.Query.BeginTransactionResponse proto) : base(
        Status.FromProto(proto.Status, proto.Issues))
    {
        Tx.TxId = proto.TxMeta.Id;
    }

    internal static BeginTransactionResponse FromProto(Ydb.Query.BeginTransactionResponse proto)
    {
        return new BeginTransactionResponse(proto);
    }
}

public class CommitTransactionResponse : ResponseBase
{
    internal CommitTransactionResponse(Status status) : base(status)
    {
    }

    private CommitTransactionResponse(Ydb.Query.CommitTransactionResponse proto) : base(
        Status.FromProto(proto.Status, proto.Issues))
    {
    }

    internal static CommitTransactionResponse FromProto(Ydb.Query.CommitTransactionResponse proto)
    {
        return new CommitTransactionResponse(proto);
    }
}

public class RollbackTransactionResponse : ResponseBase
{
    internal RollbackTransactionResponse(Status status) : base(status)
    {
    }

    private RollbackTransactionResponse(Ydb.Query.RollbackTransactionResponse proto) : base(
        Status.FromProto(proto.Status, proto.Issues))
    {
    }

    internal static RollbackTransactionResponse FromProto(Ydb.Query.RollbackTransactionResponse proto)
    {
        return new RollbackTransactionResponse(proto);
    }
}

// TODO remove crutch calls
// ===========================================================================
// WARNING: temporary structs just for testing should be removed 
// ===========================================================================
public class BeginCrutchTxSettings : OperationRequestSettings
{
}

public class CommitCrutchTxSettings : OperationRequestSettings
{
}

public class RollbackCrutchTxSettings : OperationRequestSettings
{
}

internal class BeginCrutchTxResponse : ResponseWithResultBase<BeginCrutchTxResponse.ResultData>
{
    internal BeginCrutchTxResponse(Status status, ResultData? result = null) : base(status, result)
    {
    }

    public class ResultData
    {
        public Tx? Tx { get; }

        public ResultData(Tx? tx)
        {
            Tx = tx;
        }

        internal static ResultData FromProto(Ydb.Table.BeginTransactionResult resultProto)
        {
            var txId = resultProto.TxMeta.Id;
            // var tx = new Tx { TxId = resultProto.TxMeta.Id };
            var tx = new Tx(new TransactionControl { TxId = txId });
            var result = new ResultData(tx);

            return result;
        }
    }
}

internal class CommitCrutchTxResponse : ResponseBase
{
    internal CommitCrutchTxResponse(Status status) : base(status)
    {
    }
}

internal class RollbackCrutchTxResponse : ResponseBase
{
    internal RollbackCrutchTxResponse(Status status) : base(status)
    {
    }
}
// ===========================================================================
// END WARNING: temporary structs just for testing should be removed 
// ===========================================================================

public class QueryClientConfig
{
    public SessionPoolConfig SessionPoolConfig { get; }

    public QueryClientConfig(
        SessionPoolConfig? sessionPoolConfig = null)
    {
        SessionPoolConfig = sessionPoolConfig ?? new SessionPoolConfig();
    }
}

public class QueryClient :
    ClientBase,
    IDisposable
{
    private readonly ISessionPool<Session> _sessionPool;
    private readonly ILogger _logger;
    private bool _disposed;

    public QueryClient(Driver driver, QueryClientConfig? config = null) : base(driver)
    {
        config ??= new QueryClientConfig();

        _logger = Driver.LoggerFactory.CreateLogger<QueryClient>();

        _sessionPool = new SessionPool(driver, config.SessionPoolConfig);
    }

    internal QueryClient(Driver driver, ISessionPool<Session> sessionPool) : base(driver)
    {
        _logger = driver.LoggerFactory.CreateLogger<QueryClient>();

        _sessionPool = sessionPool;
    }

    internal async Task<CreateSessionResponse> CreateSession(CreateSessionSettings? settings = null)
    {
        settings ??= new CreateSessionSettings();
        var request = new CreateSessionRequest();

        try
        {
            var response = await Driver.UnaryCall(
                method: QueryService.CreateSessionMethod,
                request: request,
                settings: settings);

            var status = Status.FromProto(response.Data.Status, response.Data.Issues);

            CreateSessionResponse.ResultData? result = null;

            if (status.IsSuccess)
            {
                result = CreateSessionResponse.ResultData.FromProto(response.Data, Driver, response.UsedEndpoint);
            }

            return new CreateSessionResponse(status, result);
        }
        catch (Driver.TransportException e)
        {
            return new CreateSessionResponse(e.Status);
        }
    }

    internal async Task<DeleteSessionResponse> DeleteSession(string sessionId, DeleteSessionSettings? settings = null)
    {
        settings ??= new DeleteSessionSettings();
        var request = new DeleteSessionRequest
        {
            SessionId = sessionId
        };

        try
        {
            var response = await Driver.UnaryCall(
                method: QueryService.DeleteSessionMethod,
                request: request,
                settings: settings);


            return DeleteSessionResponse.FromProto(response.Data);
        }
        catch (Driver.TransportException e)
        {
            return new DeleteSessionResponse(e.Status);
        }
    }

    internal SessionStateStream AttachSession(string sessionId, AttachSessionSettings? settings = null)
    {
        settings ??= new AttachSessionSettings { TransportTimeout = TimeSpan.FromDays(1) };

        var request = new AttachSessionRequest { SessionId = sessionId };

        var streamIterator = Driver.StreamCall(
            method: QueryService.AttachSessionMethod,
            request: request,
            settings: settings
        );
        return new SessionStateStream(streamIterator);
    }

    private async Task<BeginTransactionResponse> BeginTransaction(
        string sessionId,
        Tx tx,
        BeginTransactionSettings? settings = null)
    {
        settings ??= new BeginTransactionSettings();

        var request = new BeginTransactionRequest { SessionId = sessionId, TxSettings = tx.ToProto().BeginTx };
        try
        {
            var response = await Driver.UnaryCall(
                QueryService.BeginTransactionMethod,
                request: request,
                settings: settings
            );
            return BeginTransactionResponse.FromProto(response.Data);
        }
        catch (Driver.TransportException e)
        {
            return new BeginTransactionResponse(e.Status);
        }
    }

    private async Task<CommitTransactionResponse> CommitTransaction(
        string sessionId,
        Tx tx,
        CommitTransactionSettings? settings = null)
    {
        settings ??= new CommitTransactionSettings();

        var request = new CommitTransactionRequest { SessionId = sessionId, TxId = tx.TxId };

        try
        {
            var response = await Driver.UnaryCall(
                QueryService.CommitTransactionMethod,
                request: request,
                settings: settings
            );
            return CommitTransactionResponse.FromProto(response.Data);
        }
        catch (Driver.TransportException e)
        {
            return new CommitTransactionResponse(e.Status);
        }
    }

    private async Task<RollbackTransactionResponse> RollbackTransaction(
        string sessionId,
        Tx tx,
        RollbackTransactionSettings? settings = null)
    {
        settings ??= new RollbackTransactionSettings();

        var request = new RollbackTransactionRequest { SessionId = sessionId, TxId = tx.TxId };
        try
        {
            var response = await Driver.UnaryCall(
                QueryService.RollbackTransactionMethod,
                request: request,
                settings: settings
            );
            return RollbackTransactionResponse.FromProto(response.Data);
        }
        catch (Driver.TransportException e)
        {
            return new RollbackTransactionResponse(e.Status);
        }
    }


    internal ExecuteQueryStream ExecuteQuery(
        string sessionId,
        string queryString,
        Tx tx,
        IReadOnlyDictionary<string, YdbValue>? parameters,
        ExecuteQuerySettings? settings = null)
    {
        settings ??= new ExecuteQuerySettings();
        parameters ??= new Dictionary<string, YdbValue>();

        var request = new ExecuteQueryRequest
        {
            SessionId = sessionId,
            ExecMode = (Ydb.Query.ExecMode)settings.ExecMode,
            TxControl = tx.ToProto(),
            QueryContent = new QueryContent { Syntax = (Ydb.Query.Syntax)settings.Syntax, Text = queryString },
            StatsMode = (Ydb.Query.StatsMode)settings.StatsMode
        };

        request.Parameters.Add(parameters.ToDictionary(p => p.Key, p => p.Value.GetProto()));

        var streamIterator = Driver.StreamCall(
            method: QueryService.ExecuteQueryMethod,
            request: request,
            settings: settings);

        return new ExecuteQueryStream(streamIterator);
    }

    private async Task<IResponse> ExecOnSession(
        Func<Session, Task<IResponse>> func,
        RetrySettings? retrySettings = null
    )
    {
        if (_sessionPool is not SessionPool sessionPool)
        {
            throw new InvalidCastException(
                $"Unexpected cast error: {nameof(_sessionPool)} is not object of type {typeof(SessionPool).FullName}");
        }

        return await sessionPool.ExecOnSession(func, retrySettings);
    }

    internal static async Task EmptyStreamReadFunc(ExecuteQueryStream stream)
    {
        while (await stream.Next())
        {
            stream.Response.EnsureSuccess();
        }
    }

    public async Task<QueryResponseWithResult<T>> Query<T>(
        string queryString,
        Dictionary<string, YdbValue>? parameters,
        Func<ExecuteQueryStream, Task<T>> func,
        ITxModeSettings? txModeSettings = null,
        ExecuteQuerySettings? executeQuerySettings = null,
        RetrySettings? retrySettings = null)
    {
        parameters ??= new Dictionary<string, YdbValue>();
        txModeSettings ??= new TxModeSerializableSettings();
        executeQuerySettings ??= new ExecuteQuerySettings();

        var response = await ExecOnSession(
            async session =>
            {
                var tx = Tx.Begin(txModeSettings);
                tx.QueryClient = this;
                tx.SessionId = session.Id;
                return await tx.Query(queryString, parameters, func, executeQuerySettings);
            },
            retrySettings
        );
        return response switch
        {
            QueryResponseWithResult<T> queryResponseWithResult => queryResponseWithResult,
            _ => throw new InvalidCastException(
                $"Unexpected cast error: {nameof(response)} is not object of type {typeof(QueryResponseWithResult<T>).FullName}")
        };
    }

    public async Task<QueryResponseWithResult<T>> Query<T>(
        string queryString,
        Func<ExecuteQueryStream, Task<T>> func,
        ITxModeSettings? txModeSettings = null,
        ExecuteQuerySettings? executeQuerySettings = null,
        RetrySettings? retrySettings = null)
    {
        return await Query(queryString, new Dictionary<string, YdbValue>(), func, txModeSettings, executeQuerySettings,
            retrySettings);
    }

    public async Task<QueryResponse> NonQuery(string queryString,
        Dictionary<string, YdbValue>? parameters = null,
        Func<ExecuteQueryStream, Task>? func = null,
        ITxModeSettings? txModeSettings = null,
        ExecuteQuerySettings? executeQuerySettings = null,
        RetrySettings? retrySettings = null)
    {
        func ??= EmptyStreamReadFunc;

        var response = await Query<None>(
            queryString,
            parameters,
            async session =>
            {
                await func(session);
                return None.Instance;
            },
            txModeSettings,
            executeQuerySettings,
            retrySettings);
        return response;
    }

    private async Task<QueryResponseWithResult<T>> Rollback<T>(Session session, Tx tx, Status status)
    {
        // TODO remove crutch calls
        _logger.LogTrace($"Transaction {tx.TxId} not committed, try to rollback");
        try
        {
            // var rollbackResponse = await RollbackTransaction(session.Id, tx);
            var rollbackResponse = await RollbackCrutchTx(session.Id, tx);
            rollbackResponse.EnsureSuccess();
        }
        catch (StatusUnsuccessfulException e)
        {
            _logger.LogError($"Transaction {tx.TxId} rollback not successful {e.Status}");
            return new QueryResponseWithResult<T>(e.Status);
        }

        return new QueryResponseWithResult<T>(status);
    }

    public async Task<QueryResponseWithResult<T>> DoTx<T>(Func<Tx, Task<T>> func,
        ITxModeSettings? txModeSettings = null,
        RetrySettings? retrySettings = null)
    {
        // TODO remove crutch calls
        var response = await ExecOnSession(
            async session =>
            {
                // var beginTransactionResponse = await BeginTransaction(session.Id, Tx.Begin(txModeSettings));
                // beginTransactionResponse.EnsureSuccess();
                // var tx = beginTransactionResponse.Tx;
                // tx.QueryClient = this;
                // tx.SessionId = session.Id;

                var beginTransactionResponse = await BeginCrutchTx(session.Id, Tx.Begin(txModeSettings));
                beginTransactionResponse.EnsureSuccess();
                var tx = beginTransactionResponse.Result.Tx!;
                tx.QueryClient = this;
                tx.SessionId = session.Id;

                T response;
                try
                {
                    response = await func(tx);
                }
                catch (StatusUnsuccessfulException e)
                {
                    var rollbackResponse = await Rollback<T>(session, tx, e.Status);
                    return rollbackResponse;
                }
                catch (Exception e)
                {
                    var status = new Status(
                        StatusCode.InternalError,
                        $"Failed to execute lambda on tx {tx.TxId}: {e.Message}");
                    var rollbackResponse = await Rollback<T>(session, tx, status);
                    return rollbackResponse;
                }

                // var commitResponse = await CommitTransaction(session.Id, tx);
                var commitResponse = await CommitCrutchTx(session.Id, tx);
                if (!commitResponse.Status.IsSuccess)
                {
                    return await Rollback<T>(session, tx, commitResponse.Status);
                }

                return response is None
                    ? new QueryResponseWithResult<T>(Status.Success)
                    : new QueryResponseWithResult<T>(Status.Success, response);
            },
            retrySettings
        );
        return response switch
        {
            QueryResponseWithResult<T> queryResponseWithResult => queryResponseWithResult,
            _ => throw new InvalidCastException(
                $"Unexpected cast error: {nameof(response)} is not object of type {typeof(QueryResponseWithResult<T>).FullName}")
        };
    }

    public async Task<QueryResponse> DoTx(Func<Tx, Task> func,
        ITxModeSettings? txModeSettings = null,
        RetrySettings? retrySettings = null)
    {
        var response = await DoTx<None>(
            async tx =>
            {
                await func(tx);
                return None.Instance;
            },
            txModeSettings,
            retrySettings
        );
        return response;
    }

    internal record None
    {
        internal static readonly None Instance = new();
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _sessionPool.Dispose();
        }

        _disposed = true;
    }


// ===========================================================================
// WARNING: temporary methods just for testing should be removed 
// ===========================================================================
    internal async Task<BeginCrutchTxResponse> BeginCrutchTx(string sessionId,
        Tx tx, BeginCrutchTxSettings? settings = null)
    {
        settings ??= new BeginCrutchTxSettings();
        var request = new Ydb.Table.BeginTransactionRequest
        {
            SessionId = sessionId,
            OperationParams = MakeOperationParams(settings),
            TxSettings = new Ydb.Table.TransactionSettings
                { SerializableReadWrite = new Ydb.Table.SerializableModeSettings() }
        };
        try
        {
            var response = await Driver.UnaryCall(
                method: Ydb.Table.V1.TableService.BeginTransactionMethod,
                request: request,
                settings: settings
            );
            var status = UnpackOperation(response.Data.Operation, out Ydb.Table.BeginTransactionResult? resultProto);
            BeginCrutchTxResponse.ResultData? result = null;

            if (status.IsSuccess && resultProto is not null)
            {
                result = BeginCrutchTxResponse.ResultData.FromProto(resultProto);
            }

            return new BeginCrutchTxResponse(status, result);
        }
        catch (Driver.TransportException e)
        {
            return new BeginCrutchTxResponse(e.Status);
        }
    }

    internal async Task<CommitCrutchTxResponse> CommitCrutchTx(string sessionId, Tx tx,
        CommitCrutchTxSettings? settings = null)
    {
        settings ??= new CommitCrutchTxSettings();
        var request = new Ydb.Table.CommitTransactionRequest
        {
            SessionId = sessionId,
            OperationParams = MakeOperationParams(settings),
            TxId = tx.TxId
        };
        try
        {
            var response = await Driver.UnaryCall(
                method: Ydb.Table.V1.TableService.CommitTransactionMethod,
                request: request,
                settings: settings
            );
            var status = UnpackOperation(response.Data.Operation);

            return new CommitCrutchTxResponse(status);
        }
        catch (Driver.TransportException e)
        {
            return new CommitCrutchTxResponse(e.Status);
        }
    }

    internal async Task<RollbackCrutchTxResponse> RollbackCrutchTx(string sessionId, Tx tx,
        RollbackCrutchTxSettings? settings = null)
    {
        settings ??= new RollbackCrutchTxSettings();
        var request = new Ydb.Table.RollbackTransactionRequest
        {
            SessionId = sessionId,
            OperationParams = MakeOperationParams(settings),
            TxId = tx.TxId
        };

        try
        {
            var response = await Driver.UnaryCall(
                method: Ydb.Table.V1.TableService.RollbackTransactionMethod,
                request: request,
                settings: settings
            );

            var status = UnpackOperation(response.Data.Operation);

            return new RollbackCrutchTxResponse(status);
        }
        catch (Driver.TransportException e)
        {
            return new RollbackCrutchTxResponse(e.Status);
        }
    }

// ===========================================================================
// END WARNING: temporary methods just for testing should be removed 
// ===========================================================================
}

public class QueryResponse : ResponseBase
{
    public QueryResponse(Status status) : base(status)
    {
    }
}

public sealed class QueryResponseWithResult<TResult> : QueryResponse
{
    public readonly TResult? Result;

    public QueryResponseWithResult(Status status, TResult? result = default) : base(status)
    {
        Result = result;
    }
}
