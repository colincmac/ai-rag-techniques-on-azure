using System;
using System.Linq.Expressions;

public interface IEmbedded
{
    ReadOnlyMemory<float> Embedding { get; }

    double SimilarityScore { get; set; }
    double RelevanceScore { get; set; }
}

// public static class CosmosExtensions
// {
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public static double VectorDistance(this ReadOnlyMemory<float> embedding1, ReadOnlyMemory<float> embedding2)
//     {
//         // This method is a placeholder for the Cosmos DB VectorDistance function.
//         // It should never be called directly. Instead, it will be translated by the Expression Visitor.
//         throw new NotImplementedException("VectorDistance should be translated by the Cosmos DB LINQ provider.");
//     }
// }

public static double VectorDistance(this ReadOnlyMemory<float> embedding1, ReadOnlyMemory<float> embedding2)
{
    // This method is a placeholder for the Cosmos DB VectorDistance function.
    // It should never be called directly. Instead, it will be translated by the Expression Visitor.
    throw new NotImplementedException("VectorDistance should be translated by the Cosmos DB LINQ provider.");
}
public class CosmosExpressionValidator : ExpressionVisitor
{
    public void Validate(Expression expression)
    {
        Visit(expression);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Check if the binary operation is supported
        if (!(node.NodeType == ExpressionType.Equal ||
              node.NodeType == ExpressionType.AndAlso ||
              node.NodeType == ExpressionType.OrElse ||
              node.NodeType == ExpressionType.LessThan ||
              node.NodeType == ExpressionType.GreaterThan ||
              node.NodeType == ExpressionType.Add ||
              node.NodeType == ExpressionType.Subtract ||
              node.NodeType == ExpressionType.Multiply ||
              node.NodeType == ExpressionType.Divide))
        {
            throw new NotSupportedException($"The binary operator '{node.NodeType}' is not supported.");
        }

        return base.VisitBinary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Allow only supported methods like string methods and VectorDistance
        if (node.Method.DeclaringType != typeof(string) &&
            node.Method.DeclaringType != typeof(Enumerable) &&
            node.Method.Name != "VectorDistance")
        {
            throw new NotSupportedException($"The method '{node.Method.Name}' is not supported.");
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        // Ensure only supported constant types are used
        if (!(node.Value is string || node.Value is int || node.Value is long ||
              node.Value is double || node.Value is float || node.Value is bool || node.Value == null))
        {
            throw new NotSupportedException($"The constant type '{node.Type}' is not supported.");
        }

        return base.VisitConstant(node);
    }

    // Override other visit methods as needed to enforce restrictions
}


public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public Specification<T> And(Specification<T> specification)
    {
        var andSpec = new AndSpecification<T>(this, specification);
        andSpec.Validate();
        return andSpec;
    }

    public Specification<T> Or(Specification<T> specification)
    {
        var orSpec = new OrSpecification<T>(this, specification);
        orSpec.Validate();
        return orSpec;
    }

    public Specification<T> Not()
    {
        var notSpec = new NotSpecification<T>(this);
        notSpec.Validate();
        return notSpec;
    }
}

/**
  * Composite Specifications
*/
public class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter)
        );

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public void Validate()
    {
        var validator = new CosmosExpressionValidator();
        validator.Validate(ToExpression().Body);
    }
}

public class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter)
        );

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public void Validate()
    {
        var validator = new CosmosExpressionValidator();
        validator.Validate(ToExpression().Body);
    }
}

public class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;

    public NotSpecification(Specification<T> specification)
    {
        _specification = specification;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var expr = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.Not(Expression.Invoke(expr, parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public void Validate()
    {
        var validator = new CosmosExpressionValidator();
        validator.Validate(ToExpression().Body);
    }
}

/**
  * Concrete Specifications
*/
public class PropertyEqualSpecification<T> : Specification<T>
{
    private readonly Expression<Func<T, object>> _property;
    private readonly object _value;

    public PropertyEqualSpecification(Expression<Func<T, object>> property, object value)
    {
        _property = property;
        _value = value;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var parameter = _property.Parameters[0];
        var left = _property.Body;

        // Unbox if necessary
        if (left.NodeType == ExpressionType.Convert && left is UnaryExpression unary && unary.Operand is MemberExpression memberExpr)
        {
            left = memberExpr;
        }

        Expression right = Expression.Constant(_value, left.Type);

        if (left.Type == typeof(string))
        {
            var equalsMethod = typeof(string).GetMethod("Equals", new[] { typeof(string) });
            var equalsExpr = Expression.Call(left, equalsMethod, right);
            return Expression.Lambda<Func<T, bool>>(equalsExpr, parameter);
        }

        var equalExpression = Expression.Equal(left, right);
        return Expression.Lambda<Func<T, bool>>(equalExpression, parameter);
    }
}

public class StringContainsSpecification<T> : Specification<T>
{
    private readonly Expression<Func<T, string>> _property;
    private readonly string _substring;

    public StringContainsSpecification(Expression<Func<T, string>> property, string substring)
    {
        _property = property;
        _substring = substring;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var parameter = _property.Parameters[0];
        var member = _property.Body;

        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var substringExpr = Expression.Constant(_substring, typeof(string));

        var containsExpr = Expression.Call(member, containsMethod, substringExpr);
        return Expression.Lambda<Func<T, bool>>(containsExpr, parameter);
    }
}

public class ArithmeticSpecification<T> : Specification<T>
{
    private readonly Expression<Func<T, bool>> _expression;

    public ArithmeticSpecification(Expression<Func<T, bool>> expression)
    {
        _expression = expression;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        // Validate that the expression uses only supported arithmetic operations
        // (handled by the validator)
        return _expression;
    }
}
/**
  * Projection & Vector Search Specifications
*/
public abstract class ProjectionSpecification<T, TResult>
{
    public abstract Expression<Func<T, TResult>> ToProjection();

    public virtual void Validate()
    {
        var validator = new CosmosExpressionValidator();
        validator.Validate(ToProjection().Body);
    }
}

// public class VectorSearchProjection<TRecord> : ProjectionSpecification<TRecord, (TRecord Record, double SimilarityScore)> where TRecord : IEmbedded
// {
//     private readonly ReadOnlyMemory<float> _embedding;

//     public VectorSearchProjection(ReadOnlyMemory<float> embedding)
//     {
//         _embedding = embedding;
//     }

//     public override Expression<Func<TRecord, (TRecord Record, double SimilarityScore)>> ToProjection()
//     {
//         return record => (
//             Record: record,
//             SimilarityScore: record.Embedding.VectorDistance(_embedding)
//         );
//     }
// }

public class VectorSearchSpecification<T> : Specification<T> where T : IEmbedded
{
    private readonly ReadOnlyMemory<float> _inputEmbedding;
    private readonly double _minRelevanceScore;

    public VectorSearchSpecification(ReadOnlyMemory<float> inputEmbedding, double minRelevanceScore = 0.0)
    {
        _inputEmbedding = inputEmbedding;
        _minRelevanceScore = minRelevanceScore;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        // Assume that the entity T has a property named 'Embedding' of type ReadOnlyMemory<float>
        // Adjust the property name as per your entity's definition

        return entity => entity.Embedding.VectorDistance(_inputEmbedding) >= _minRelevanceScore;
    }
}

// public class VectorDistanceExpressionVisitor : ExpressionVisitor
// {
//     private static readonly MethodInfo VectorDistanceMethod = typeof(CosmosExtensions)
//         .GetMethod(nameof(CosmosExtensions.VectorDistance), new Type[] { typeof(ReadOnlyMemory<float>), typeof(ReadOnlyMemory<float>) });

//     protected override Expression VisitMethodCall(MethodCallExpression node)
//     {
//         if (node.Method.Equals(VectorDistanceMethod))
//         {
//             // Translate VectorDistance to Cosmos DB's VectorDistance SQL function
//             // Cosmos DB expects the function to be called as VectorDistance(field, @vector, false)

//             // Assuming the third parameter (false) is a constant
//             if (node.Arguments.Count == 2)
//             {
//                 var left = node.Arguments[0];
//                 var right = node.Arguments[1];
//                 var similarityCalculation = Expression.Call(
//                     typeof(VectorDistanceHelper).GetMethod(nameof(VectorDistanceHelper.VectorDistanceCosmos)),
//                     left,
//                     right
//                 );
//                 return similarityCalculation;
//             }
//         }

//         return base.VisitMethodCall(node);
//     }
// }
public static class VectorDistanceHelper
{
    public static double VectorDistanceCosmos(ReadOnlyMemory<float> embedding1, ReadOnlyMemory<float> embedding2)
    {
        // This method serves as a placeholder for the Cosmos DB VectorDistance function.
        // It should never be called directly. Instead, it will be translated by the Expression Visitor.
        throw new NotImplementedException("VectorDistanceCosmos should be translated by the VectorDistanceExpressionVisitor.");
    }
}
public class VectorDistanceExpressionVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == "VectorDistance")
        {
            // Translate VectorDistance to Cosmos DB's VectorDistance SQL function
            // Cosmos DB expects the function to be called as VectorDistance(field, @vector, false)

            // Assuming the third parameter (false) is a constant
            if (node.Arguments.Count == 2)
            {
                var left = node.Arguments[0];
                var right = node.Arguments[1];
                var similarityCalculation = Expression.Call(
                    typeof(VectorDistanceHelper).GetMethod(nameof(VectorDistanceHelper.VectorDistanceCosmos)),
                    left,
                    right
                );
                return similarityCalculation;
            }
        }

        return base.VisitMethodCall(node);
    }
}

public class PagingSpecification<T>
{
    public int? Skip { get; private set; }
    public int? Take { get; private set; }

    public PagingSpecification() { }

    public PagingSpecification<T> WithSkip(int skip)
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be non-negative.");

        Skip = skip;
        return this;
    }

    public PagingSpecification<T> WithTake(int take)
    {
        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero.");

        Take = take;
        return this;
    }
}
public class QueryOptions<T>: QueryOptions<T, T>
{
}
public class QueryOptions<T, TResult>
{
    public Specification<T> Specification { get; set; }
    public PagingSpecification<T> Paging { get; set; }
    public ProjectionSpecification<T, TResult> Projection { get; set; }

    public QueryOptions()
    {
        Paging = new PagingSpecification<T>();
    }
}

public static class SpecificationEvaluator
{
    public static IQueryable<TResult> GetQuery<T, TResult>(
        IQueryable<T> inputQuery,
        QueryOptions<T, TResult> queryOptions)
    {
        if (queryOptions == null)
            return inputQuery.Cast<TResult>();

        var query = inputQuery;

        // Apply filtering specification
        if (queryOptions.Specification != null)
        {
            var expression = queryOptions.Specification.ToExpression();

            // Visit and modify the expression to handle VectorDistance
            var visitor = new VectorDistanceExpressionVisitor();
            var modifiedExpression = (Expression<Func<T, bool>>)visitor.Visit(expression);

            query = query.Where(modifiedExpression);
        }

        IQueryable<TResult> resultQuery;
        // Apply projection specification
        if (queryOptions.Projection != null)
        {
            var projection = queryOptions.Projection.ToProjection();
            resultQuery = query.Select(projection);
        }
        else
        {
            resultQuery = query.Cast<TResult>();
        }

        // Apply paging specification
        if (queryOptions.Paging != null)
        {
            if (queryOptions.Paging.Skip.HasValue)
            {
                resultQuery = resultQuery.Skip(queryOptions.Paging.Skip.Value);
            }

            if (queryOptions.Paging.Take.HasValue)
            {
                resultQuery = resultQuery.Take(queryOptions.Paging.Take.Value);
            }
        }

        return resultQuery;
    }
}

public interface IRepository<T, TResult>
{
    Task<IEnumerable<TResult>> FindAsync(QueryOptions<T, TResult> queryOptions);
    Task<TResult> FindSingleAsync(QueryOptions<T, TResult> queryOptions);
    // Additional CRUD operations as needed
}

