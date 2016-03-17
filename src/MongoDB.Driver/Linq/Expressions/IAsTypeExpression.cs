using System.Linq.Expressions;

namespace MongoDB.Driver.Linq.Expressions
{
    internal interface IAsTypeExpression : ISerializationExpression
    {
        Expression Document { get; }     
    }
}