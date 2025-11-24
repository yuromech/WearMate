using System.Text;

namespace WearMate.Shared.Helpers;

/// <summary>
/// Custom Query Builder for Supabase PostgREST API
/// </summary>
public class QueryBuilder
{
    private readonly string _baseUrl;
    private readonly string _table;
    private readonly StringBuilder _query;
    private readonly List<string> _selects = new();
    private readonly List<string> _filters = new();
    private string? _order;
    private int? _limit;
    private int? _offset;

    public QueryBuilder(string baseUrl, string table)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _table = table;
        _query = new StringBuilder();
    }

    /// <summary>
    /// Select columns (default: *)
    /// </summary>
    public QueryBuilder Select(params string[] columns)
    {
        if (columns.Length > 0)
            _selects.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Filter: WHERE column = value
    /// </summary>
    public QueryBuilder Eq(string column, object value)
    {
        _filters.Add($"{column}=eq.{value}");
        return this;
    }

    /// <summary>
    /// Filter: WHERE column != value
    /// </summary>
    public QueryBuilder Neq(string column, object value)
    {
        _filters.Add($"{column}=neq.{value}");
        return this;
    }

    /// <summary>
    /// Filter: WHERE column > value
    /// </summary>
    public QueryBuilder Gt(string column, object value)
    {
        _filters.Add($"{column}=gt.{value}");
        return this;
    }

    /// <summary>
    /// Filter: WHERE column >= value
    /// </summary>
    public QueryBuilder Gte(string column, object value)
    {
        _filters.Add($"{column}=gte.{value}");
        return this;
    }

    /// <summary>
    /// Filter: WHERE column < value
    /// </summary>
    public QueryBuilder Lt(string column, object value)
    {
        _filters.Add($"{column}=lt.{value}");
        return this;
    }

    /// <summary>
    /// Filter: WHERE column <= value
    /// </summary>
    public QueryBuilder Lte(string column, object value)
    {
        _filters.Add($"{column}=lte.{value}");
        return this;
    }

    /// <summary>
    /// Filter: WHERE column LIKE %value%
    /// </summary>
    public QueryBuilder Like(string column, string value)
    {
        _filters.Add($"{column}=like.*{value}*");
        return this;
    }

    /// <summary>
    /// Filter: WHERE column ILIKE %value% (case insensitive)
    /// </summary>
    public QueryBuilder ILike(string column, string value)
    {
        _filters.Add($"{column}=ilike.*{value}*");
        return this;
    }

    /// <summary>
    /// Filter: WHERE column IS NULL
    /// </summary>
    public QueryBuilder IsNull(string column)
    {
        _filters.Add($"{column}=is.null");
        return this;
    }

    /// <summary>
    /// Filter: WHERE column IS NOT NULL
    /// </summary>
    public QueryBuilder IsNotNull(string column)
    {
        _filters.Add($"{column}=not.is.null");
        return this;
    }

    /// <summary>
    /// Filter: WHERE column IN (values)
    /// </summary>
    public QueryBuilder In(string column, params object[] values)
    {
        var valueList = string.Join(",", values);
        _filters.Add($"{column}=in.({valueList})");
        return this;
    }

    /// <summary>
    /// Order by column
    /// </summary>
    public QueryBuilder OrderBy(string column, bool ascending = true)
    {
        _order = $"{column}.{(ascending ? "asc" : "desc")}";
        return this;
    }

    /// <summary>
    /// Limit results
    /// </summary>
    public QueryBuilder Limit(int limit)
    {
        _limit = limit;
        return this;
    }

    /// <summary>
    /// Offset results (for pagination)
    /// </summary>
    public QueryBuilder Offset(int offset)
    {
        _offset = offset;
        return this;
    }

    /// <summary>
    /// Build the final query URL
    /// </summary>
    public string Build()
    {
        var url = $"{_baseUrl}/rest/v1/{_table}";
        var queryParams = new List<string>();

        // Select
        if (_selects.Any())
            queryParams.Add($"select={string.Join(",", _selects)}");
        else
            queryParams.Add("select=*");

        // Filters
        queryParams.AddRange(_filters);

        // Order
        if (!string.IsNullOrEmpty(_order))
            queryParams.Add($"order={_order}");

        // Limit
        if (_limit.HasValue)
            queryParams.Add($"limit={_limit.Value}");

        // Offset
        if (_offset.HasValue)
            queryParams.Add($"offset={_offset.Value}");

        if (queryParams.Any())
            url += "?" + string.Join("&", queryParams);

        return url;
    }

    /// <summary>
    /// Build URL for single row (returns first match)
    /// </summary>
    public string BuildSingle()
    {
        return Build();
    }

    /// <summary>
    /// Build URL for count query
    /// </summary>
    public string BuildCount()
    {
        var url = $"{_baseUrl}/rest/v1/{_table}";
        var queryParams = new List<string>
        {
            "select=count"
        };

        queryParams.AddRange(_filters);

        return url + "?" + string.Join("&", queryParams);
    }
}