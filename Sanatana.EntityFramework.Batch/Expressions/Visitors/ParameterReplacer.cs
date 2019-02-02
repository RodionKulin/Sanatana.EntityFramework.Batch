using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Expressions
{
    internal class ParameterReplacer : ExpressionVisitor
    {
        //properties
        public ParameterExpression ParameterExpression { get; private set; }



        //init
        public ParameterReplacer(ParameterExpression paramExpr)
        {
            ParameterExpression = paramExpr;
        }

        //methods
        public Expression Replace(Expression expr)
        {
            return Visit(expr);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return ParameterExpression;
        }
    }
}
