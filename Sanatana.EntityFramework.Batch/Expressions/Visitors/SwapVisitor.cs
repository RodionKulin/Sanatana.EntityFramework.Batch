using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sanatana.EntityFramework.Batch.Expressions
{
    public class SwapVisitor : ExpressionVisitor
    {
        //field
        private readonly Expression from, to;


        //init
        public SwapVisitor(Expression from, Expression to)
        {
            this.from = from;
            this.to = to;
        }


        //method
        public override Expression Visit(Expression node)
        {
            return node == from ? to : base.Visit(node);
        }
    }
}
