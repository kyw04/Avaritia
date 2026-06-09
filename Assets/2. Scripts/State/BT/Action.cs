using System;

namespace BT
{
    public class Action : Node
    {
        private readonly Func<NodeStatus> action;

        public Action(Func<NodeStatus> action)
        {
            this.action = action;
        }

        public override NodeStatus Evaluate() => action();
    }
}
