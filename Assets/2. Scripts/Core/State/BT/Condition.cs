using System;

namespace BT
{
    public class Condition : Node
    {
        private readonly Func<bool> condition;

        public Condition(Func<bool> condition)
        {
            this.condition = condition;
        }

        public override NodeStatus Evaluate() =>
            condition() ? NodeStatus.Success : NodeStatus.Failure;
    }
}
